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
        var rankingResponse = BuildRankingResponse(options.Scenario, catalog);
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
            inputs,
            inputs.Journeys,
            inputs.Session,
            selection,
            retrieval,
            rankingResponse,
            aiRecord,
            aiResponse,
            catalog);
        var analytics = BuildAnalytics(
            options.Scenario,
            inputs,
            inputs.Journeys,
            inputs.Session,
            selection,
            retrieval,
            rankingResponse,
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
            ["05-candidate-retrieval.json"] = retrieval,
            ["06-ranking-request.json"] = rankingRequest,
            ["07-ranking-response.json"] = rankingResponse,
            ["08-ai-prompt-input.json"] = promptInput,
            ["09-ai-output.json"] = aiRecord,
            ["10-final-response.json"] = finalResponse,
            ["11-analytics-events.json"] = analytics,
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
                interpretationJson);
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

    private JsonObject BuildRetrieval(
        string scenario,
        IReadOnlyList<JourneyState> journeys,
        SessionContext session,
        ActiveJourneySelection selection,
        ActivityCatalog catalog)
    {
        var retrieval = _fixtures.LoadScenarioArtifact(scenario, "05-candidate-retrieval.json").DeepCloneObject();
        var activeJourney = selection.SelectedJourney;
        var activeJourneyNode = retrieval.RequireObjectProperty("retrieval_query").RequireObjectProperty("active_journey");
        activeJourneyNode["service_category"] = activeJourney.ServiceCategory;
        activeJourneyNode["stage"] = activeJourney.Stage;
        activeJourneyNode["intent"] = activeJourney.Intent;
        if (activeJourney.ResumeCandidate)
        {
            activeJourneyNode["resume_candidate"] = true;
        }
        else
        {
            activeJourneyNode.Remove("resume_candidate");
        }

        var contextNode = retrieval.RequireObjectProperty("retrieval_query").RequireObjectProperty("context");
        contextNode["region"] = session.Region;
        contextNode["channel"] = session.Channel;

        if (retrieval.RequireObjectProperty("retrieval_query")["secondary_journey"] is JsonObject secondaryJourneyNode)
        {
            var secondaryJourney = journeys.FirstOrDefault(journey =>
                journey.JourneyId != activeJourney.JourneyId
                && journey.ServiceCategory == secondaryJourneyNode.RequireStringProperty("service_category"));

            if (secondaryJourney is not null)
            {
                secondaryJourneyNode["service_category"] = secondaryJourney.ServiceCategory;
                secondaryJourneyNode["stage"] = secondaryJourney.Stage;
                secondaryJourneyNode["intent"] = secondaryJourney.Intent;
            }
        }

        foreach (var candidate in retrieval.RequireArrayProperty("candidates_returned").OfType<JsonObject>())
        {
            var assetId = candidate.RequireStringProperty("asset_id");
            if (!catalog.Assets.TryGetValue(assetId, out var asset))
            {
                continue;
            }

            candidate["asset_type"] = asset.AssetType;
            candidate["service_category"] = asset.ServiceCategory;
        }

        retrieval["total_candidates"] = retrieval.RequireArrayProperty("candidates_returned").Count;
        return retrieval;
    }

    private JsonObject BuildRankingRequest(
        string scenario,
        ScenarioInputs inputs,
        IReadOnlyList<JourneyState> journeys,
        SessionContext session,
        ActiveJourneySelection selection,
        JsonObject retrieval,
        ActivityCatalog catalog)
    {
        var request = _fixtures.LoadScenarioArtifact(scenario, "06-ranking-request.json").DeepCloneObject();
        var activeJourney = journeys.First(journey => journey.JourneyId == selection.SelectedJourney.JourneyId);

        request["scenario"] = scenario;
        request["customer_profile"] = new JsonObject
        {
            ["customer_id"] = session.CustomerId,
            ["lead_score"] = inputs.RequireCustomerSummary().RequireProperty("lead_score").DeepClone(),
            ["location"] = inputs.RequireAttributes().RequireProperty("location").DeepClone(),
            ["household_type"] = inputs.RequireAttributes().RequireProperty("household_type").DeepClone(),
        };
        request["active_journey"] = new JsonObject
        {
            ["journey_id"] = activeJourney.JourneyId,
            ["service_category"] = activeJourney.ServiceCategory,
            ["intent"] = activeJourney.Intent,
            ["stage"] = activeJourney.Stage,
            ["urgency"] = activeJourney.Urgency,
            ["resume_candidate"] = activeJourney.ResumeCandidate,
            ["qualification_state"] = new JsonObject
            {
                ["coverage_region_match"] = activeJourney.QualificationState.CoverageRegionMatch,
                ["serviceability_confirmed"] = activeJourney.QualificationState.ServiceabilityConfirmed,
                ["hard_exclusions"] = new JsonArray(activeJourney.QualificationState.HardExclusions.Select(static value => (JsonNode)value).ToArray()),
                ["suppression_flags"] = new JsonArray(activeJourney.QualificationState.SuppressionFlags.Select(static value => (JsonNode)value).ToArray()),
            },
        };

        if (request["ai_context"] is JsonObject aiContext)
        {
            var suggestedJourneyId = selection.Interpretation.SuggestedJourneyId;
            aiContext["suggested_journey_id"] = suggestedJourneyId;
            if (suggestedJourneyId is not null)
            {
                var suggestedJourney = journeys.FirstOrDefault(journey => journey.JourneyId == suggestedJourneyId);
                aiContext["suggested_service_category"] = suggestedJourney?.ServiceCategory;
            }
            else
            {
                aiContext["suggested_service_category"] = null;
            }
            aiContext["deterministic_override_required"] = selection.DeterministicOverride;
        }

        var context = request.RequireObjectProperty("context").DeepCloneObject();
        context["channel"] = session.Channel;
        context["campaign_source"] = session.EntryPoint;
        context["campaign_theme"] = session.CampaignTheme;
        context["session_id"] = session.SessionId;
        request["context"] = context;
        var retrievalCandidatesById = retrieval.RequireArrayProperty("candidates_returned")
            .OfType<JsonObject>()
            .ToDictionary(
                static candidate => candidate.RequireStringProperty("asset_id"),
                static candidate => candidate,
                StringComparer.Ordinal);
        request["candidates"] = new JsonArray(request.RequireArrayProperty("candidates")
            .OfType<JsonObject>()
            .Select(candidate =>
            {
                var contentId = candidate.RequireStringProperty("content_id");
                return BuildRankingCandidate(candidate, retrievalCandidatesById[contentId], catalog);
            })
            .Cast<JsonNode>()
            .ToArray());

        return request;
    }

    private JsonObject BuildRankingResponse(string scenario, ActivityCatalog catalog)
    {
        var response = _fixtures.LoadScenarioArtifact(scenario, "07-ranking-response.json").DeepCloneObject();
        foreach (var recommendation in response.RequireArrayProperty("ranked_recommendations").OfType<JsonObject>())
        {
            var contentId = recommendation.RequireStringProperty("content_id");
            if (!catalog.Assets.TryGetValue(contentId, out var asset))
            {
                continue;
            }

            recommendation["cta"] = new JsonObject
            {
                ["type"] = asset.CtaType,
                ["label"] = asset.CtaLabel,
                ["deep_link"] = asset.CtaDeepLink,
            };
        }

        return response;
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

    private JsonObject BuildFinalResponse(
        string scenario,
        ScenarioInputs inputs,
        JsonObject journeysPayload,
        JsonObject session,
        JsonObject selection,
        JsonObject retrieval,
        JsonObject rankingResponse,
        JsonObject aiRecord,
        JsonObject aiResponse,
        ActivityCatalog catalog)
    {
        var template = _fixtures.LoadScenarioArtifact(scenario, "10-final-response.json");
        var selectedJourneyId = selection.RequireStringProperty("selected_journey_id");
        var activeJourney = journeysPayload.RequireArrayProperty("journeys")
            .OfType<JsonObject>()
            .First(journey => journey.RequireStringProperty("journey_id") == selectedJourneyId);
        var topRecommendation = rankingResponse.RequireArrayProperty("ranked_recommendations")
            .OfType<JsonObject>()
            .First();
        var supportingContent = rankingResponse.RequireArrayProperty("ranked_recommendations")
            .OfType<JsonObject>()
            .Skip(1)
            .Take(1)
            .Select(static recommendation => (JsonNode)new JsonObject
            {
                ["content_id"] = recommendation.RequireProperty("content_id").DeepClone(),
                ["cta_type"] = recommendation.RequireObjectProperty("cta").RequireProperty("type").DeepClone(),
                ["label"] = recommendation.RequireObjectProperty("cta").RequireProperty("label").DeepClone(),
                ["deep_link"] = recommendation.RequireObjectProperty("cta").RequireProperty("deep_link").DeepClone(),
            })
            .ToArray();

        return new JsonObject
        {
            ["scenario"] = scenario,
            ["description"] = template.RequireProperty("description").DeepClone(),
            ["customer_id"] = session.RequireProperty("customer_id").DeepClone(),
            ["session_id"] = session.RequireProperty("session_id").DeepClone(),
            ["active_journey"] = new JsonObject
            {
                ["journey_id"] = activeJourney.RequireProperty("journey_id").DeepClone(),
                ["service_category"] = activeJourney.RequireProperty("service_category").DeepClone(),
            },
            ["next_best_action"] = new JsonObject
            {
                ["content_id"] = topRecommendation.RequireProperty("content_id").DeepClone(),
                ["cta_type"] = topRecommendation.RequireObjectProperty("cta").RequireProperty("type").DeepClone(),
                ["label"] = topRecommendation.RequireObjectProperty("cta").RequireProperty("label").DeepClone(),
                ["deep_link"] = topRecommendation.RequireObjectProperty("cta").RequireProperty("deep_link").DeepClone(),
                ["ranking_score"] = topRecommendation.RequireProperty("score").DeepClone(),
                ["ranking_policy_version"] = rankingResponse.RequireProperty("ranking_policy_version").DeepClone(),
            },
            ["supporting_content"] = new JsonArray(supportingContent),
            ["secondary_journey_prompt"] = BuildSecondaryJourneyPrompt(journeysPayload, selection, retrieval, catalog),
            ["explanation"] = BuildExplanation(topRecommendation, aiRecord, aiResponse),
            ["decision_trace"] = template.RequireObjectProperty("decision_trace").DeepCloneObject(),
            ["metadata_revision"] = catalog.Assets[topRecommendation.RequireStringProperty("content_id")].MetadataRevision,
            ["response_generated_at"] = AddSeconds(session.RequireStringProperty("timestamp"), 2),
        };
    }

    private JsonObject BuildAnalytics(
        string scenario,
        ScenarioInputs inputs,
        JsonObject journeysPayload,
        JsonObject session,
        JsonObject selection,
        JsonObject retrieval,
        JsonObject rankingResponse,
        JsonObject finalResponse,
        JsonObject aiRecord)
    {
        var analytics = _fixtures.LoadScenarioArtifact(scenario, "11-analytics-events.json").DeepCloneObject();
        foreach (var eventNode in analytics.RequireArrayProperty("events").OfType<JsonObject>())
        {
            var metadata = eventNode.RequireObjectProperty("metadata");
            switch (eventNode.RequireStringProperty("event_type"))
            {
                case "active_journey_selected":
                    var aiInterpretation = selection.RequireObjectProperty("ai_interpretation");
                    if (metadata.ContainsKey("ai_suggested_journey_id"))
                    {
                        metadata["ai_suggested_journey_id"] = aiInterpretation["suggested_journey_id"]?.DeepClone();
                    }
                    if (metadata.ContainsKey("ai_confidence"))
                    {
                        metadata["ai_confidence"] = aiInterpretation["confidence"]?.DeepClone();
                    }
                    if (metadata.ContainsKey("deterministic_override"))
                    {
                        metadata["deterministic_override"] = selection.RequireProperty("deterministic_override").DeepClone();
                    }
                    break;
                case "ai_response_accepted":
                    metadata["response_id"] = aiRecord.RequireProperty("ai_response_id").DeepClone();
                    metadata["grounding_asset_ids"] = finalResponse
                        .RequireObjectProperty("explanation")
                        .RequireArrayProperty("grounding_asset_ids")
                        .DeepCloneArray();
                    metadata["accepted"] = aiRecord.RequireStringProperty("response_status") == "accepted";
                    metadata["latency_ms"] = aiRecord.RequireProperty("latency_ms").DeepClone();
                    break;
                case "cta_clicked":
                    var nextBestAction = finalResponse.RequireObjectProperty("next_best_action");
                    metadata["content_id"] = nextBestAction.RequireProperty("content_id").DeepClone();
                    metadata["cta_type"] = nextBestAction.RequireProperty("cta_type").DeepClone();
                    metadata["destination"] = nextBestAction.RequireProperty("deep_link").DeepClone();
                    break;
            }
        }
        return analytics;
    }

    private static JsonObject BuildExplanation(
        JsonObject topRecommendation,
        JsonObject aiRecord,
        JsonObject aiResponse)
    {
        if (aiRecord.RequireStringProperty("response_status") == "accepted")
        {
            return new JsonObject
            {
                ["source"] = "ai_assisted",
                ["ai_response_id"] = aiRecord.RequireProperty("ai_response_id").DeepClone(),
                ["summary"] = aiResponse.RequireProperty("summary").DeepClone(),
                ["cta_support_text"] = aiResponse.RequireProperty("cta_support_text").DeepClone(),
                ["grounding_asset_ids"] = aiResponse.RequireArrayProperty("grounding_asset_ids").DeepCloneArray(),
            };
        }

        var label = topRecommendation.RequireObjectProperty("cta").RequireStringProperty("label");
        return new JsonObject
        {
            ["source"] = "deterministic_fallback",
            ["ai_response_id"] = null,
            ["summary"] = $"Continue with {label} to take the next step in this journey.",
            ["cta_support_text"] = label,
            ["grounding_asset_ids"] = new JsonArray(),
        };
    }

    private static void WriteOutputs(string outputDirectory, IReadOnlyDictionary<string, JsonObject> outputs)
    {
        Directory.CreateDirectory(outputDirectory);
        foreach (var (name, payload) in outputs)
        {
            JsonExtensions.WriteIndentedJson(Path.Combine(outputDirectory, name), payload);
        }
    }

    private static JsonObject? BuildSecondaryJourneyPrompt(
        JsonObject journeysPayload,
        JsonObject selection,
        JsonObject retrieval,
        ActivityCatalog catalog)
    {
        var secondaryCandidate = retrieval.RequireArrayProperty("candidates_returned")
            .OfType<JsonObject>()
            .FirstOrDefault(candidate => candidate.RequireStringProperty("retrieval_source") == "secondary_journey");
        if (secondaryCandidate is null)
        {
            return null;
        }

        var assetId = secondaryCandidate.RequireStringProperty("asset_id");
        if (!catalog.Assets.TryGetValue(assetId, out var asset))
        {
            return null;
        }

        var selectedJourneyId = selection.RequireStringProperty("selected_journey_id");
        var serviceCategory = secondaryCandidate.RequireStringProperty("service_category");
        var secondaryJourney = journeysPayload.RequireArrayProperty("journeys")
            .OfType<JsonObject>()
            .FirstOrDefault(journey =>
                journey.RequireStringProperty("journey_id") != selectedJourneyId
                && journey.RequireStringProperty("service_category") == serviceCategory);
        if (secondaryJourney is null)
        {
            return null;
        }

        return new JsonObject
        {
            ["journey_id"] = secondaryJourney.RequireProperty("journey_id").DeepClone(),
            ["service_category"] = secondaryJourney.RequireProperty("service_category").DeepClone(),
            ["label"] = asset.CtaLabel,
            ["deep_link"] = asset.CtaDeepLink,
            ["content_id"] = asset.AssetId,
        };
    }

    private static JsonObject BuildRankingCandidate(
        JsonObject templateCandidate,
        JsonObject retrievalCandidate,
        ActivityCatalog catalog)
    {
        var assetId = retrievalCandidate.RequireStringProperty("asset_id");
        var asset = catalog.Assets[assetId].Raw;
        var candidate = templateCandidate.DeepCloneObject();
        candidate["content_id"] = asset.RequireProperty("assetId").DeepClone();
        candidate["asset_type"] = asset.RequireProperty("assetType").DeepClone();
        candidate["service_category"] = asset.RequireProperty("serviceCategory").DeepClone();
        candidate["cta_type"] = asset.RequireObjectProperty("cta").RequireProperty("type").DeepClone();
        candidate["cta_deep_link"] = asset.RequireObjectProperty("cta").RequireProperty("deepLink").DeepClone();
        candidate["provider"] = asset["provider"]?.DeepClone();
        candidate["priority"] = asset.RequireProperty("priority").DeepClone();
        candidate["funnel_stage"] = retrievalCandidate.RequireProperty("funnel_stage_match").DeepClone();
        candidate["retrieval_source"] = retrievalCandidate.RequireProperty("retrieval_source").DeepClone();
        return candidate;
    }

    private static string AddSeconds(string timestamp, int seconds)
    {
        return DateTimeOffset.Parse(timestamp).AddSeconds(seconds).ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}

/// <summary>
/// Returns the generated artifacts and output location for one completed scenario run.
/// </summary>
internal sealed record ScenarioRunResult(
    string Scenario,
    string OutputDirectory,
    IReadOnlyDictionary<string, JsonObject> Outputs);
