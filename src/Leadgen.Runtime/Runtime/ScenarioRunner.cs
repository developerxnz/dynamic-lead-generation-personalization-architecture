using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

internal sealed class ScenarioRunner
{
    private readonly RepositoryPaths _paths;
    private readonly FixtureStore _fixtures;
    private readonly RagPromptBuilder _ragPromptBuilder = new();
    private readonly HttpClient _httpClient = new();

    public ScenarioRunner(RepositoryPaths paths)
    {
        _paths = paths;
        _fixtures = new FixtureStore(paths);
    }

    public async Task RunAsync(CliOptions options)
    {
        if (!_fixtures.ListScenarios().Contains(options.Scenario, StringComparer.Ordinal))
        {
            throw new ArgumentException($"Unknown scenario: {options.Scenario}");
        }

        var inputs = await LoadInputsAsync(options);
        var catalog = ActivityCatalog.Load(_paths);
        var selection = BuildSelection(options.Scenario, inputs.Journeys);
        var retrieval = BuildRetrieval(options.Scenario, inputs.Journeys, inputs.Session, selection, catalog);
        var rankingRequest = BuildRankingRequest(
            options.Scenario,
            inputs.Profile,
            inputs.Journeys,
            inputs.Session,
            selection,
            retrieval,
            catalog);
        var rankingResponse = BuildRankingResponse(options.Scenario, catalog);
        var promptFixture = _fixtures.LoadScenarioArtifact(options.Scenario, "08-ai-prompt-input.json");
        var promptInput = options.PromptSource == "rag"
            ? _ragPromptBuilder.Build(
                options.Scenario,
                inputs.Profile,
                inputs.Journeys,
                inputs.Session,
                selection,
                rankingResponse,
                catalog,
                promptFixture)
            : promptFixture.DeepCloneObject();

        var (aiResponse, aiRecord) = await RunAiExplanationAsync(options, promptInput);
        var finalResponse = BuildFinalResponse(
            options.Scenario,
            inputs.Profile,
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
            inputs.Profile,
            inputs.Journeys,
            inputs.Session,
            selection,
            retrieval,
            rankingResponse,
            finalResponse,
            aiRecord);
        var outputDirectory = options.OutputDir
            ?? Path.Combine(Path.GetTempPath(), "leadgen-scenario-runs", options.Scenario);

        WriteOutputs(outputDirectory, new Dictionary<string, JsonObject>
        {
            ["04-active-journey-selection.json"] = selection,
            ["05-candidate-retrieval.json"] = retrieval,
            ["06-ranking-request.json"] = rankingRequest,
            ["07-ranking-response.json"] = rankingResponse,
            ["08-ai-prompt-input.json"] = promptInput,
            ["09-ai-output.json"] = aiRecord,
            ["10-final-response.json"] = finalResponse,
            ["11-analytics-events.json"] = analytics,
        });

        if (options.Source == "cosmos")
        {
            var config = CosmosConfig.FromEnvironment();
            await using var store = new CosmosRuntimeStore(config);
            await store.PersistRuntimeOutputsAsync(options.Scenario, inputs.Profile, finalResponse, analytics);
        }

        Console.WriteLine($"Ran scenario: {options.Scenario}");
        Console.WriteLine($"  source: {options.Source}");
        Console.WriteLine($"  ai_mode: {options.AiMode}");
        Console.WriteLine($"  prompt_source: {options.PromptSource}");
        Console.WriteLine($"  output_dir: {outputDirectory}");
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

    private JsonObject BuildSelection(string scenario, JsonObject journeysPayload)
    {
        var selection = _fixtures.LoadScenarioArtifact(scenario, "04-active-journey-selection.json").DeepCloneObject();
        var journeysById = journeysPayload.RequireArrayProperty("journeys")
            .OfType<JsonObject>()
            .ToDictionary(
                static journey => journey.RequireStringProperty("journey_id"),
                static journey => journey,
                StringComparer.Ordinal);

        var selectedJourneyId = selection.RequireStringProperty("selected_journey_id");
        if (journeysById.TryGetValue(selectedJourneyId, out var selectedJourney))
        {
            selection["selected_service_category"] = selectedJourney.RequireProperty("service_category").DeepClone();
        }

        foreach (var candidate in selection.RequireArrayProperty("candidate_journeys").OfType<JsonObject>())
        {
            var journeyId = candidate.RequireStringProperty("journey_id");
            if (!journeysById.TryGetValue(journeyId, out var journey))
            {
                continue;
            }

            candidate["service_category"] = journey.RequireProperty("service_category").DeepClone();
            candidate["journey_score"] = journey
                .RequireObjectProperty("decision_support")
                .RequireProperty("journey_score")
                .DeepClone();
        }

        return selection;
    }

    private JsonObject BuildRetrieval(
        string scenario,
        JsonObject journeysPayload,
        JsonObject session,
        JsonObject selection,
        ActivityCatalog catalog)
    {
        var retrieval = _fixtures.LoadScenarioArtifact(scenario, "05-candidate-retrieval.json").DeepCloneObject();
        var journeysById = journeysPayload.RequireArrayProperty("journeys")
            .OfType<JsonObject>()
            .ToDictionary(
                static journey => journey.RequireStringProperty("journey_id"),
                static journey => journey,
                StringComparer.Ordinal);

        var activeJourney = journeysById[selection.RequireStringProperty("selected_journey_id")];
        var activeJourneyNode = retrieval.RequireObjectProperty("retrieval_query").RequireObjectProperty("active_journey");
        activeJourneyNode["service_category"] = activeJourney.RequireProperty("service_category").DeepClone();
        activeJourneyNode["stage"] = activeJourney.RequireProperty("stage").DeepClone();
        activeJourneyNode["intent"] = activeJourney.RequireProperty("intent").DeepClone();
        if (activeJourney.OptionalBoolProperty("resume_candidate"))
        {
            activeJourneyNode["resume_candidate"] = true;
        }
        else
        {
            activeJourneyNode.Remove("resume_candidate");
        }

        var contextNode = retrieval.RequireObjectProperty("retrieval_query").RequireObjectProperty("context");
        contextNode["region"] = session.RequireProperty("region").DeepClone();
        contextNode["channel"] = session.RequireProperty("channel").DeepClone();

        if (retrieval.RequireObjectProperty("retrieval_query")["secondary_journey"] is JsonObject secondaryJourneyNode)
        {
            var secondaryJourney = journeysById.Values.FirstOrDefault(journey =>
                journey.RequireStringProperty("journey_id") != activeJourney.RequireStringProperty("journey_id")
                && journey.RequireStringProperty("service_category") == secondaryJourneyNode.RequireStringProperty("service_category"));

            if (secondaryJourney is not null)
            {
                secondaryJourneyNode["service_category"] = secondaryJourney.RequireProperty("service_category").DeepClone();
                secondaryJourneyNode["stage"] = secondaryJourney.RequireProperty("stage").DeepClone();
                secondaryJourneyNode["intent"] = secondaryJourney.RequireProperty("intent").DeepClone();
            }
        }

        foreach (var candidate in retrieval.RequireArrayProperty("candidates_returned").OfType<JsonObject>())
        {
            var assetId = candidate.RequireStringProperty("asset_id");
            if (!catalog.Assets.TryGetValue(assetId, out var asset))
            {
                continue;
            }

            candidate["asset_type"] = asset.RequireProperty("assetType").DeepClone();
            candidate["service_category"] = asset.RequireProperty("serviceCategory").DeepClone();
        }

        retrieval["total_candidates"] = retrieval.RequireArrayProperty("candidates_returned").Count;
        return retrieval;
    }

    private JsonObject BuildRankingRequest(
        string scenario,
        JsonObject profile,
        JsonObject journeysPayload,
        JsonObject session,
        JsonObject selection,
        JsonObject retrieval,
        ActivityCatalog catalog)
    {
        var request = _fixtures.LoadScenarioArtifact(scenario, "06-ranking-request.json").DeepCloneObject();
        var activeJourney = journeysPayload.RequireArrayProperty("journeys")
            .OfType<JsonObject>()
            .First(journey => journey.RequireStringProperty("journey_id") == selection.RequireStringProperty("selected_journey_id"));

        request["scenario"] = scenario;
        request["customer_profile"] = new JsonObject
        {
            ["customer_id"] = profile.RequireProperty("customer_id").DeepClone(),
            ["lead_score"] = profile.RequireObjectProperty("customer_summary").RequireProperty("lead_score").DeepClone(),
            ["location"] = profile.RequireObjectProperty("profile").RequireProperty("location").DeepClone(),
            ["household_type"] = profile.RequireObjectProperty("profile").RequireProperty("household_type").DeepClone(),
            ["is_returning_customer"] = profile.RequireObjectProperty("customer_summary").RequireProperty("is_returning_customer").DeepClone(),
        };
        request["active_journey"] = new JsonObject
        {
            ["journey_id"] = activeJourney.RequireProperty("journey_id").DeepClone(),
            ["service_category"] = activeJourney.RequireProperty("service_category").DeepClone(),
            ["intent"] = activeJourney.RequireProperty("intent").DeepClone(),
            ["stage"] = activeJourney.RequireProperty("stage").DeepClone(),
            ["urgency"] = activeJourney.RequireProperty("urgency").DeepClone(),
            ["resume_candidate"] = activeJourney.RequireProperty("resume_candidate").DeepClone(),
            ["qualification_state"] = activeJourney.RequireObjectProperty("qualification_state").DeepCloneObject(),
        };

        if (request["ai_context"] is JsonObject aiContext)
        {
            var suggestedJourneyId = selection.RequireObjectProperty("ai_interpretation").RequireStringProperty("suggested_journey_id");
            aiContext["suggested_journey_id"] = suggestedJourneyId;

            var suggestedJourney = journeysPayload.RequireArrayProperty("journeys")
                .OfType<JsonObject>()
                .FirstOrDefault(journey => journey.RequireStringProperty("journey_id") == suggestedJourneyId);
            aiContext["suggested_service_category"] = suggestedJourney?.RequireProperty("service_category").DeepClone();
            aiContext["deterministic_override_required"] = selection.OptionalBoolProperty("deterministic_override");
        }

        request["context"] = new JsonObject
        {
            ["channel"] = session.RequireProperty("channel").DeepClone(),
            ["campaign_source"] = session.RequireProperty("entry_point").DeepClone(),
            ["campaign_theme"] = session["campaign_theme"]?.DeepClone(),
            ["session_id"] = session.RequireProperty("session_id").DeepClone(),
        };
        request["candidates"] = new JsonArray(retrieval.RequireArrayProperty("candidates_returned")
            .OfType<JsonObject>()
            .Select(candidate => BuildRankingCandidate(candidate, catalog))
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
                ["type"] = asset.RequireObjectProperty("cta").RequireProperty("type").DeepClone(),
                ["label"] = asset.RequireObjectProperty("cta").RequireProperty("label").DeepClone(),
                ["deep_link"] = asset.RequireObjectProperty("cta").RequireProperty("deepLink").DeepClone(),
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

        JsonObject responseObject;
        try
        {
            responseObject = JsonNode.Parse(raw).RequireObject("ai_response");
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Ollama returned invalid JSON content: {ex.Message}");
        }

        responseObject = PromptUtilities.NormalizeGroundingAssetIds(responseObject, promptInput);
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

    private JsonObject BuildFinalResponse(
        string scenario,
        JsonObject profile,
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
            ["explanation"] = new JsonObject
            {
                ["source"] = "ai_assisted",
                ["ai_response_id"] = aiRecord.RequireProperty("ai_response_id").DeepClone(),
                ["summary"] = aiResponse.RequireProperty("summary").DeepClone(),
                ["cta_support_text"] = aiResponse.RequireProperty("cta_support_text").DeepClone(),
                ["grounding_asset_ids"] = aiResponse.RequireArrayProperty("grounding_asset_ids").DeepCloneArray(),
            },
            ["decision_trace"] = template.RequireObjectProperty("decision_trace").DeepCloneObject(),
            ["metadata_revision"] = catalog.Assets[topRecommendation.RequireStringProperty("content_id")]
                .RequireProperty("metadataRevision")
                .DeepClone(),
            ["response_generated_at"] = AddSeconds(session.RequireStringProperty("timestamp"), 2),
        };
    }

    private JsonObject BuildAnalytics(
        string scenario,
        JsonObject profile,
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
            ["label"] = asset.RequireObjectProperty("cta").RequireProperty("label").DeepClone(),
            ["deep_link"] = asset.RequireObjectProperty("cta").RequireProperty("deepLink").DeepClone(),
            ["content_id"] = asset.RequireProperty("assetId").DeepClone(),
        };
    }

    private static JsonObject BuildRankingCandidate(JsonObject retrievalCandidate, ActivityCatalog catalog)
    {
        var assetId = retrievalCandidate.RequireStringProperty("asset_id");
        var asset = catalog.Assets[assetId];
        return new JsonObject
        {
            ["content_id"] = asset.RequireProperty("assetId").DeepClone(),
            ["asset_type"] = asset.RequireProperty("assetType").DeepClone(),
            ["service_category"] = asset.RequireProperty("serviceCategory").DeepClone(),
            ["cta_type"] = asset.RequireObjectProperty("cta").RequireProperty("type").DeepClone(),
            ["cta_deep_link"] = asset.RequireObjectProperty("cta").RequireProperty("deepLink").DeepClone(),
            ["provider"] = asset["provider"]?.DeepClone(),
            ["priority"] = asset.RequireProperty("priority").DeepClone(),
            ["funnel_stage"] = retrievalCandidate.RequireProperty("funnel_stage_match").DeepClone(),
            ["retrieval_source"] = retrievalCandidate.RequireProperty("retrieval_source").DeepClone(),
        };
    }

    private static string AddSeconds(string timestamp, int seconds)
    {
        return DateTimeOffset.Parse(timestamp).AddSeconds(seconds).ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}
