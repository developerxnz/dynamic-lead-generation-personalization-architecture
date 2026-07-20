using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Orchestrates a scenario run end-to-end, from input loading through artifact generation and optional persistence.
/// </summary>
internal sealed class ScenarioRunner
{
    private readonly RepositoryPaths _paths;
    private readonly FixtureStore _fixtures;
    private readonly RagPromptBuilder _ragPromptBuilder = new();
    private readonly AiJourneyInterpreter _journeyInterpreter;
    private readonly HttpClient _httpClient = new();

    public ScenarioRunner(RepositoryPaths paths)
    {
        _paths = paths;
        _fixtures = new FixtureStore(paths);
        _journeyInterpreter = new AiJourneyInterpreter(_fixtures);
    }

    public async Task RunAsync(CliOptions options)
    {
        await RunScenarioAsync(options);
    }

    public async Task<ScenarioRunResult> RunScenarioAsync(
        CliOptions options,
        bool printSummary = true,
        bool writeOutputs = true)
    {
        if (!_fixtures.ListScenarios().Contains(options.Scenario, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unknown scenario: {options.Scenario}");
        }

        var inputs = await LoadInputsAsync(options);
        var catalog = ActivityCatalog.Load(_paths);
        var journeySummaries = JourneySummaryBuilder.Build(inputs.JourneyStates);
        var interpretation = await _journeyInterpreter.InterpretAsync(options, inputs.SessionContext, journeySummaries);
        var selectionResult = DeterministicJourneySelector.Select(journeySummaries, inputs.SessionContext, interpretation);
        var selection = selectionResult.ToJson();
        var interpretationJson = interpretation.ToJson();
        var retrieval = BuildRetrieval(
            options.Scenario,
            inputs.JourneyStates,
            inputs.SessionContext,
            selectionResult,
            catalog);
        var rankingRequest = BuildRankingRequest(
            options.Scenario,
            inputs,
            inputs.JourneyStates,
            inputs.SessionContext,
            selectionResult,
            retrieval,
            catalog);
        var rankingResponse = RankingEngine.Rank(rankingRequest, selectionResult, catalog);
        var promptFixture = _fixtures.LoadScenarioArtifact(options.Scenario, "08-ai-prompt-input.json");
        var promptInput = options.PromptSource == "rag"
            ? _ragPromptBuilder.Build(
                options.Scenario,
                inputs,
                inputs.JourneyStates,
                inputs.SessionContext,
                selectionResult,
                rankingResponse,
                catalog,
                promptFixture)
            : promptFixture.DeepCloneObject();

        var (aiResponse, aiRecord) = await RunAiExplanationAsync(options, promptInput);
        var finalResponse = BuildFinalResponse(
            options.Scenario,
            inputs.JourneyStates,
            inputs.SessionContext,
            selectionResult,
            retrieval,
            rankingResponse,
            aiRecord,
            aiResponse,
            catalog);
        var analytics = BuildAnalytics(
            options.Scenario,
            selectionResult,
            finalResponse,
            aiRecord);
        var outputDirectory = options.OutputDir
            ?? Path.Combine(Path.GetTempPath(), "leadgen-scenario-runs", options.Scenario);

        var outputs = new Dictionary<string, JsonObject>
        {
            ["02-journey-summaries.json"] = new JsonObject
            {
                ["journeys"] = new JsonArray(journeySummaries.Select(static summary => (JsonNode)summary.ToJson()).ToArray()),
            },
            ["03-ai-journey-interpretation.json"] = interpretationJson,
            ["04-active-journey-selection.json"] = selection,
            ["05-candidate-retrieval.json"] = retrieval.ToJson(),
            ["06-ranking-request.json"] = rankingRequest.ToJson(),
            ["07-ranking-response.json"] = rankingResponse.ToJson(),
            ["08-ai-prompt-input.json"] = promptInput,
            ["09-ai-output.json"] = aiRecord,
            ["10-final-response.json"] = finalResponse.ToJson(),
            ["11-analytics-events.json"] = analytics.ToJson(),
        };

        if (writeOutputs)
        {
            WriteOutputs(outputDirectory, outputs);
        }

        if (options.Source == "cosmos")
        {
            var config = CosmosConfig.FromEnvironment();
            await using var store = new CosmosRuntimeStore(config);
            await store.PersistRuntimeOutputsAsync(
                options.Scenario,
                inputs.CustomerId,
                finalResponse,
                analytics,
                interpretation);
        }

        if (printSummary)
        {
            Console.WriteLine($"Ran scenario: {options.Scenario}");
            Console.WriteLine($"  source: {options.Source}");
            Console.WriteLine($"  ai_mode: {options.AiMode}");
            Console.WriteLine($"  prompt_source: {options.PromptSource}");
            Console.WriteLine($"  output_dir: {outputDirectory}");
        }

        return new ScenarioRunResult(options.Scenario, outputDirectory, outputs);
    }

    private async Task<ScenarioInputs> LoadInputsAsync(CliOptions options)
    {
        if (options.Source == "fixtures")
        {
            return _fixtures.LoadScenarioInputs(options.Scenario);
        }

        var config = CosmosConfig.FromEnvironment();
        await using var store = new CosmosRuntimeStore(config);
        if (options.SeedCosmos)
        {
            await store.SeedScenarioAsync(_fixtures.LoadScenarioInputs(options.Scenario));
        }

        return await store.LoadScenarioInputsAsync(options.Scenario, _fixtures);
    }

    private CandidateRetrieval BuildRetrieval(
        string scenario,
        IReadOnlyList<JourneyState> journeys,
        SessionContext session,
        ActiveJourneySelection selection,
        ActivityCatalog catalog)
    {
        var retrieval = RetrievalContractAdapter.FromJson(
            _fixtures.LoadScenarioArtifact(scenario, "05-candidate-retrieval.json"));
        var activeJourney = selection.SelectedJourney;
        var secondaryJourney = retrieval.Query.SecondaryJourney is null
            ? null
            : journeys.FirstOrDefault(journey =>
                journey.JourneyId != activeJourney.JourneyId
                && journey.ServiceCategory == retrieval.Query.SecondaryJourney.ServiceCategory);

        return retrieval with
        {
            Query = retrieval.Query with
            {
                ActiveJourney = new RetrievalJourney(
                    activeJourney.ServiceCategory,
                    activeJourney.Stage,
                    activeJourney.Intent,
                    activeJourney.ResumeCandidate,
                    null),
                SecondaryJourney = secondaryJourney is null || retrieval.Query.SecondaryJourney is null
                    ? retrieval.Query.SecondaryJourney
                    : retrieval.Query.SecondaryJourney with
                    {
                        ServiceCategory = secondaryJourney.ServiceCategory,
                        Stage = secondaryJourney.Stage,
                        Intent = secondaryJourney.Intent,
                    },
                Context = retrieval.Query.Context with { Region = session.Region, Channel = session.Channel },
            },
            Candidates = retrieval.Candidates.Select(candidate =>
                catalog.Assets.TryGetValue(candidate.AssetId, out var asset)
                    ? candidate with { AssetType = asset.AssetType, ServiceCategory = asset.ServiceCategory }
                    : candidate).ToArray(),
        };
    }

    private RankingRequest BuildRankingRequest(
        string scenario,
        ScenarioInputs inputs,
        IReadOnlyList<JourneyState> journeys,
        SessionContext session,
        ActiveJourneySelection selection,
        CandidateRetrieval retrieval,
        ActivityCatalog catalog)
    {
        var template = RuntimeOutputContractAdapter.RankingRequest(
            _fixtures.LoadScenarioArtifact(scenario, "06-ranking-request.json"));
        var activeJourney = journeys.First(journey => journey.JourneyId == selection.SelectedJourney.JourneyId);
        var suggestedJourneyId = selection.Interpretation.SuggestedJourneyId;
        var aiContext = template.AiContext is null
            ? null
            : new RankingAiContext(
                suggestedJourneyId,
                journeys.FirstOrDefault(journey => journey.JourneyId == suggestedJourneyId)?.ServiceCategory,
                selection.DeterministicOverride);
        var retrievalCandidatesById = retrieval.Candidates.ToDictionary(
            static candidate => candidate.AssetId,
            StringComparer.Ordinal);
        return template with
        {
            Scenario = scenario,
            CustomerProfile = new RankingCustomerProfile(
                session.CustomerId,
                inputs.Attributes.Location,
                inputs.Attributes.HouseholdType),
            ActiveJourney = new RankingJourney(
                activeJourney.JourneyId,
                activeJourney.ServiceCategory,
                activeJourney.Intent,
                activeJourney.Stage,
                activeJourney.Urgency,
                activeJourney.ResumeCandidate,
                activeJourney.QualificationState),
            AiContext = aiContext,
            Context = template.Context with
            {
                Channel = session.Channel,
                CampaignSource = session.EntryPoint,
                CampaignTheme = session.CampaignTheme,
                SessionId = session.SessionId,
            },
            Candidates = template.Candidates
                .Select(candidate => BuildRankingCandidate(
                    candidate,
                    retrievalCandidatesById[candidate.ContentId],
                    catalog))
                .ToArray(),
        };
    }

    private async Task<(JsonObject Response, JsonObject Record)> RunAiExplanationAsync(CliOptions options, JsonObject promptInput)
    {
        var expectedOutput = _fixtures.LoadScenarioArtifact(options.Scenario, "09-ai-expected-output.json");
        if (options.AiMode == "expected")
        {
            return (expectedOutput.RequireObjectProperty("response").DeepCloneObject(), expectedOutput.DeepCloneObject());
        }
        if (options.AiMode is "unavailable" or "invalid")
        {
            return RejectedAiExplanation(
                expectedOutput,
                options.AiMode,
                options.AiMode == "unavailable" ? "forced_unavailable" : "invalid_response",
                $"AI explanation is disabled by --ai-mode {options.AiMode}.");
        }

        var model = Environment.GetEnvironmentVariable("MODEL")
            ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL")
            ?? "llama3.1:8b";
        var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://ollama:11434";
        var requestBody = new JsonObject
        {
            ["model"] = model,
            ["messages"] = new JsonArray(
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = promptInput.RequireStringProperty("system_prompt"),
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = PromptUtilities.AssembleUserMessage(promptInput),
                }),
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["temperature"] = 0.1,
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/v1/chat/completions")
            {
                Content = JsonContent.Create(requestBody),
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "ollama");

            var stopwatch = Stopwatch.StartNew();
            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            stopwatch.Stop();

            var completion = JsonNode.Parse(responseContent).RequireObject("completion");
            var raw = completion.RequireArrayProperty("choices")[0]
                ?.RequireObject("choice")
                .RequireObjectProperty("message")
                .RequireStringProperty("content")
                ?? throw new InvalidDataException("Missing choices[0].message.content in Ollama response.");
            var responseObject = JsonNode.Parse(raw).RequireObject("ai_response");
            responseObject = PromptUtilities.NormalizeGroundingAssetIds(responseObject, promptInput);
            var typedResponse = AiExplanationResponse.FromJson(responseObject);
            responseObject = typedResponse.ToJson();
            var validation = PromptUtilities.ValidateResponse(responseObject, promptInput);
            var aiRecord = new JsonObject
            {
                ["scenario"] = options.Scenario,
                ["description"] = expectedOutput.RequireStringProperty("description"),
                ["prompt_template_version"] = "poc-cta-explainer-v1",
                ["response_status"] = validation.AllPassed ? "accepted" : "rejected",
                ["response"] = responseObject,
                ["validation"] = new JsonObject
                {
                    ["required_fields_present"] = promptInput.RequireObjectProperty("response_contract")
                        .RequireArrayProperty("required_fields")
                        .Select(static field => field?.GetValue<string>() ?? string.Empty)
                        .Where(static field => field.Length > 0)
                        .All(field => validation.Checks.GetValueOrDefault($"{field}_present")),
                    ["grounding_assets_referenced"] =
                        validation.Checks.GetValueOrDefault("grounding_assets_cited")
                        && validation.Checks.GetValueOrDefault("grounding_assets_valid"),
                    ["unsupported_claims_detected"] = false,
                    ["summary_word_count"] = PromptUtilities.CountWords(responseObject.OptionalStringProperty("summary") ?? string.Empty),
                    ["key_points_count"] = responseObject["key_points"] is JsonArray keyPoints ? keyPoints.Count : 0,
                    ["cta_support_text_word_count"] = PromptUtilities.CountWords(responseObject.OptionalStringProperty("cta_support_text") ?? string.Empty),
                    ["within_length_bounds"] =
                        validation.Checks.GetValueOrDefault("summary_within_length")
                        && validation.Checks.GetValueOrDefault("key_points_within_count")
                        && validation.Checks.GetValueOrDefault("cta_text_within_length"),
                    ["disclosure_required"] = false,
                },
                ["ai_response_id"] = completion.OptionalStringProperty("id") ?? expectedOutput.RequireStringProperty("ai_response_id"),
                ["ai_task_type"] = "cta_explanation",
                ["model_version"] = model,
                ["latency_ms"] = stopwatch.ElapsedMilliseconds,
            };

            return (responseObject, aiRecord);
        }
        catch (HttpRequestException ex)
        {
            return RejectedAiExplanation(expectedOutput, model, "request_failed", ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            return RejectedAiExplanation(expectedOutput, model, "request_timed_out", ex.Message);
        }
        catch (JsonException ex)
        {
            return RejectedAiExplanation(expectedOutput, model, "invalid_json", ex.Message);
        }
        catch (InvalidDataException ex)
        {
            return RejectedAiExplanation(expectedOutput, model, "invalid_response", ex.Message);
        }
    }

    private static (JsonObject Response, JsonObject Record) RejectedAiExplanation(
        JsonObject expectedOutput,
        string model,
        string reason,
        string detail)
    {
        var response = new JsonObject();
        return (response, new JsonObject
        {
            ["scenario"] = expectedOutput.RequireProperty("scenario").DeepClone(),
            ["description"] = expectedOutput.RequireProperty("description").DeepClone(),
            ["prompt_template_version"] = "poc-cta-explainer-v1",
            ["response_status"] = "rejected",
            ["response"] = response.DeepCloneObject(),
            ["validation"] = new JsonObject
            {
                ["fallback_reason"] = reason,
                ["detail"] = detail,
            },
            ["ai_response_id"] = "unavailable",
            ["ai_task_type"] = "cta_explanation",
            ["model_version"] = model,
            ["latency_ms"] = 0,
        });
    }

    private FinalResponseEnvelope BuildFinalResponse(
        string scenario,
        IReadOnlyList<JourneyState> journeys,
        SessionContext session,
        ActiveJourneySelection selection,
        CandidateRetrieval retrieval,
        RankingResponse rankingResponse,
        JsonObject aiRecord,
        JsonObject aiResponse,
        ActivityCatalog catalog)
    {
        var template = RuntimeOutputContractAdapter.FinalResponse(
            _fixtures.LoadScenarioArtifact(scenario, "10-final-response.json"));
        var activeJourney = journeys.First(journey => journey.JourneyId == selection.SelectedJourney.JourneyId);
        var topRecommendation = rankingResponse.RankedRecommendations.First();
        var supportingContent = rankingResponse.RankedRecommendations
            .Skip(1)
            .Take(1)
            .Select(recommendation => new SupportingContent(
                recommendation.ContentId,
                recommendation.Cta.Type,
                recommendation.Cta.Label,
                recommendation.Cta.DeepLink))
            .ToArray();

        return template with
        {
            Scenario = scenario,
            CustomerId = session.CustomerId,
            SessionId = session.SessionId,
            ActiveJourney = new FinalActiveJourney(activeJourney.JourneyId, activeJourney.ServiceCategory),
            NextBestAction = new FinalNextBestAction(
                topRecommendation.ContentId,
                topRecommendation.Cta.Type,
                topRecommendation.Cta.Label,
                topRecommendation.Cta.DeepLink,
                topRecommendation.Score,
                rankingResponse.RankingPolicyVersion),
            SupportingContent = supportingContent,
            SecondaryJourneyPrompt = BuildSecondaryJourneyPrompt(journeys, selection, retrieval, catalog),
            Explanation = BuildExplanation(topRecommendation, aiRecord, aiResponse),
            MetadataRevision = catalog.Assets[topRecommendation.ContentId].MetadataRevision,
            ResponseGeneratedAt = session.Timestamp.AddSeconds(2),
        };
    }

    private AnalyticsEnvelope BuildAnalytics(
        string scenario,
        ActiveJourneySelection selection,
        FinalResponseEnvelope finalResponse,
        JsonObject aiRecord)
    {
        var template = RuntimeOutputContractAdapter.Analytics(
            _fixtures.LoadScenarioArtifact(scenario, "11-analytics-events.json"));
        var events = template.Events.Select(@event =>
        {
            var metadata = @event.Metadata.DeepCloneObject();
            switch (@event.EventType)
            {
                case "active_journey_selected":
                    if (metadata.ContainsKey("ai_suggested_journey_id"))
                    {
                        metadata["ai_suggested_journey_id"] = selection.Interpretation.SuggestedJourneyId;
                    }
                    if (metadata.ContainsKey("ai_confidence"))
                    {
                        metadata["ai_confidence"] = selection.Interpretation.Confidence;
                    }
                    if (metadata.ContainsKey("deterministic_override"))
                    {
                        metadata["deterministic_override"] = selection.DeterministicOverride;
                    }
                    break;
                case "ai_response_accepted":
                    metadata["response_id"] = aiRecord.RequireStringProperty("ai_response_id");
                    metadata["grounding_asset_ids"] = new JsonArray(finalResponse.Explanation.GroundingAssetIds
                        .Select(assetId => (JsonNode)assetId).ToArray());
                    metadata["accepted"] = aiRecord.RequireStringProperty("response_status") == "accepted";
                    metadata["latency_ms"] = long.Parse(aiRecord.RequireProperty("latency_ms").ToJsonString());
                    break;
                case "cta_clicked":
                    metadata["content_id"] = finalResponse.NextBestAction.ContentId;
                    metadata["cta_type"] = finalResponse.NextBestAction.CtaType;
                    metadata["destination"] = finalResponse.NextBestAction.DeepLink;
                    break;
            }
            return @event with { Metadata = metadata };
        }).ToArray();
        return template with { Scenario = scenario, Events = events };
    }

    private static FinalExplanation BuildExplanation(
        RankedRecommendation topRecommendation,
        JsonObject aiRecord,
        JsonObject aiResponse)
    {
        if (aiRecord.RequireStringProperty("response_status") == "accepted")
        {
            return new FinalExplanation(
                "ai_assisted",
                aiRecord.RequireStringProperty("ai_response_id"),
                aiResponse.RequireStringProperty("summary"),
                aiResponse.RequireStringProperty("cta_support_text"),
                aiResponse.RequireArrayProperty("grounding_asset_ids")
                    .Select(static assetId => assetId?.GetValue<string>() ?? string.Empty)
                    .ToArray());
        }

        var label = topRecommendation.Cta.Label;
        return new FinalExplanation(
            "deterministic_fallback",
            null,
            $"Continue with {label} to take the next step in this journey.",
            label,
            Array.Empty<string>());
    }

    private static void WriteOutputs(string outputDirectory, IReadOnlyDictionary<string, JsonObject> outputs)
    {
        Directory.CreateDirectory(outputDirectory);
        foreach (var (name, payload) in outputs)
        {
            JsonExtensions.WriteIndentedJson(Path.Combine(outputDirectory, name), payload);
        }
    }

    private static SecondaryJourneyPrompt? BuildSecondaryJourneyPrompt(
        IReadOnlyList<JourneyState> journeys,
        ActiveJourneySelection selection,
        CandidateRetrieval retrieval,
        ActivityCatalog catalog)
    {
        var secondaryCandidate = retrieval.Candidates
            .FirstOrDefault(candidate => candidate.RetrievalSource == "secondary_journey");
        if (secondaryCandidate is null)
        {
            return null;
        }

        var assetId = secondaryCandidate.AssetId;
        if (!catalog.Assets.TryGetValue(assetId, out var asset))
        {
            return null;
        }

        var selectedJourneyId = selection.SelectedJourney.JourneyId;
        var serviceCategory = secondaryCandidate.ServiceCategory;
        var secondaryJourney = journeys.FirstOrDefault(journey =>
            journey.JourneyId != selectedJourneyId
            && journey.ServiceCategory == serviceCategory);
        if (secondaryJourney is null)
        {
            return null;
        }

        return new SecondaryJourneyPrompt(
            secondaryJourney.JourneyId,
            secondaryJourney.ServiceCategory,
            asset.CtaLabel,
            asset.CtaDeepLink,
            asset.AssetId);
    }

    private static RankingCandidate BuildRankingCandidate(
        RankingCandidate templateCandidate,
        RetrievedCandidate retrievalCandidate,
        ActivityCatalog catalog)
    {
        var assetId = retrievalCandidate.AssetId;
        var asset = catalog.Assets[assetId];
        return templateCandidate with
        {
            ContentId = asset.AssetId,
            AssetType = asset.AssetType,
            ServiceCategory = asset.ServiceCategory,
            CtaType = asset.CtaType,
            CtaDeepLink = asset.CtaDeepLink,
            FunnelStage = retrievalCandidate.FunnelStageMatch,
            RetrievalSource = retrievalCandidate.RetrievalSource,
        };
    }
}

/// <summary>
/// Returns the generated artifacts and output location for one completed scenario run.
/// </summary>
internal sealed record ScenarioRunResult(
    string Scenario,
    string OutputDirectory,
    IReadOnlyDictionary<string, JsonObject> Outputs);
