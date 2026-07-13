using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Produces bounded, non-authoritative journey interpretation for the current session.
/// </summary>
internal sealed class AiJourneyInterpreter
{
    private readonly FixtureStore _fixtures;
    private readonly HttpClient _httpClient = new();

    public AiJourneyInterpreter(FixtureStore fixtures)
    {
        _fixtures = fixtures;
    }

    public async Task<JsonObject> InterpretAsync(
        CliOptions options,
        ScenarioInputs inputs,
        JsonArray journeySummaries)
    {
        if (options.AiMode == "expected")
        {
            var expected = _fixtures.LoadScenarioArtifact(options.Scenario, "04-active-journey-selection.json")
                .RequireObjectProperty("ai_interpretation")
                .DeepCloneObject();
            return Accepted(expected, "fixture", "expected", 0);
        }

        if (options.AiMode == "unavailable")
        {
            return Unavailable(
                "forced_unavailable",
                "AI interpretation is disabled by --ai-mode unavailable.");
        }
        if (options.AiMode == "invalid")
        {
            try
            {
                Validate(new JsonObject
                {
                    ["suggested_journey_id"] = "journey-not-in-candidate-set",
                    ["confidence"] = 1.2,
                    ["reason_summary"] = "Synthetic invalid interpretation for validation coverage.",
                }, journeySummaries);
            }
            catch (InvalidDataException ex)
            {
                return Unavailable("invalid_response", ex.Message);
            }
        }

        var prompt = BuildPrompt(inputs, journeySummaries);
        var model = Environment.GetEnvironmentVariable("MODEL")
            ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL")
            ?? "llama3.1:8b";
        var requestBody = new JsonObject
        {
            ["model"] = model,
            ["messages"] = new JsonArray(
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = "You interpret session intent for a lead-generation platform. Return JSON only. "
                        + "You may suggest only one supplied journey ID. You do not decide eligibility, compliance, "
                        + "ranking, suitability, or the final active journey.",
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = prompt.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                }),
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["temperature"] = 0.1,
        };

        try
        {
            var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://ollama:11434";
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/v1/chat/completions")
            {
                Content = JsonContent.Create(requestBody),
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "ollama");

            var stopwatch = Stopwatch.StartNew();
            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            stopwatch.Stop();

            var completion = JsonNode.Parse(content).RequireObject("journey_interpretation_completion");
            var raw = completion.RequireArrayProperty("choices")[0]
                ?.RequireObject("choice")
                .RequireObjectProperty("message")
                .RequireStringProperty("content")
                ?? throw new InvalidDataException("Missing choices[0].message.content in Ollama response.");
            var interpretation = JsonNode.Parse(raw).RequireObject("journey_interpretation");
            Validate(interpretation, journeySummaries);
            return Accepted(interpretation, "ollama", model, stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            return Unavailable("request_failed", ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            return Unavailable("request_timed_out", ex.Message);
        }
        catch (JsonException ex)
        {
            return Unavailable("invalid_json", ex.Message);
        }
        catch (InvalidDataException ex)
        {
            return Unavailable("invalid_response", ex.Message);
        }
    }

    private static JsonObject BuildPrompt(ScenarioInputs inputs, JsonArray journeySummaries)
    {
        return new JsonObject
        {
            ["task"] = "Suggest the journey most relevant to this session. Return suggested_journey_id, confidence, and reason_summary.",
            ["session"] = new JsonObject
            {
                ["query_text"] = inputs.Session["query_text"]?.DeepClone(),
                ["current_url"] = inputs.Session["current_url"]?.DeepClone(),
                ["entry_point"] = inputs.Session["entry_point"]?.DeepClone(),
                ["campaign_theme"] = inputs.Session["campaign_theme"]?.DeepClone(),
                ["region"] = inputs.Session["region"]?.DeepClone(),
                ["channel"] = inputs.Session["channel"]?.DeepClone(),
            },
            ["candidate_journeys"] = journeySummaries.DeepCloneArray(),
            ["response_contract"] = new JsonObject
            {
                ["required_fields"] = new JsonArray("suggested_journey_id", "confidence", "reason_summary"),
                ["confidence_range"] = "0.0 to 1.0",
                ["protected_decisions"] = "Do not return eligibility, compliance, suitability, ranking, or action decisions.",
            },
        };
    }

    private static void Validate(JsonObject interpretation, JsonArray journeySummaries)
    {
        var suggestedJourneyId = interpretation.RequireStringProperty("suggested_journey_id");
        var confidence = interpretation.RequireProperty("confidence").GetValue<double>();
        var reason = interpretation.RequireStringProperty("reason_summary");
        var validIds = journeySummaries.OfType<JsonObject>()
            .Select(static journey => journey.RequireStringProperty("journey_id"))
            .ToHashSet(StringComparer.Ordinal);

        if (!validIds.Contains(suggestedJourneyId))
        {
            throw new InvalidDataException("AI suggested a journey outside the supplied candidate set.");
        }

        if (confidence is < 0 or > 1 || string.IsNullOrWhiteSpace(reason) || reason.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 60)
        {
            throw new InvalidDataException("AI journey interpretation violates the response contract.");
        }
    }

    private static JsonObject Accepted(JsonObject interpretation, string source, string model, long latencyMilliseconds)
    {
        return new JsonObject
        {
            ["status"] = "accepted",
            ["source"] = source,
            ["model_version"] = model,
            ["latency_ms"] = latencyMilliseconds,
            ["suggested_journey_id"] = interpretation.RequireProperty("suggested_journey_id").DeepClone(),
            ["confidence"] = interpretation.RequireProperty("confidence").DeepClone(),
            ["reason_summary"] = interpretation.RequireProperty("reason_summary").DeepClone(),
        };
    }

    private static JsonObject Unavailable(string reason, string detail)
    {
        return new JsonObject
        {
            ["status"] = "unavailable",
            ["fallback_reason"] = reason,
            ["detail"] = detail,
        };
    }
}
