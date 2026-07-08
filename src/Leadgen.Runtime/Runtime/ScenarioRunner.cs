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

        if (options.Source != "fixtures")
        {
            throw new NotSupportedException("The initial C# runtime supports fixture-backed scenarios only. Use the Python scripts for Cosmos-backed runs.");
        }

        var inputs = _fixtures.LoadScenarioInputs(options.Scenario);
        var catalog = ActivityCatalog.Load(_paths);
        var selection = _fixtures.LoadScenarioArtifact(options.Scenario, "04-active-journey-selection.json");
        var retrieval = _fixtures.LoadScenarioArtifact(options.Scenario, "05-candidate-retrieval.json");
        var rankingRequest = _fixtures.LoadScenarioArtifact(options.Scenario, "06-ranking-request.json");
        var rankingResponse = _fixtures.LoadScenarioArtifact(options.Scenario, "07-ranking-response.json");
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
        var finalResponse = BuildFinalResponse(options.Scenario, aiRecord, aiResponse);
        var analytics = BuildAnalytics(options.Scenario, finalResponse, aiRecord);
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

        Console.WriteLine($"Ran scenario: {options.Scenario}");
        Console.WriteLine($"  source: {options.Source}");
        Console.WriteLine($"  ai_mode: {options.AiMode}");
        Console.WriteLine($"  prompt_source: {options.PromptSource}");
        Console.WriteLine($"  output_dir: {outputDirectory}");
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

    private JsonObject BuildFinalResponse(string scenario, JsonObject aiRecord, JsonObject aiResponse)
    {
        var finalResponse = _fixtures.LoadScenarioArtifact(scenario, "10-final-response.json").DeepCloneObject();
        var explanation = finalResponse.RequireObjectProperty("explanation");
        explanation["ai_response_id"] = aiRecord.RequireProperty("ai_response_id").DeepClone();
        explanation["summary"] = aiResponse.RequireProperty("summary").DeepClone();
        explanation["cta_support_text"] = aiResponse.RequireProperty("cta_support_text").DeepClone();
        explanation["grounding_asset_ids"] = aiResponse.RequireArrayProperty("grounding_asset_ids").DeepCloneArray();
        return finalResponse;
    }

    private JsonObject BuildAnalytics(string scenario, JsonObject finalResponse, JsonObject aiRecord)
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
}
