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

    public async Task<JourneyInterpretation> InterpretAsync(
        CliOptions options,
        SessionContext session,
        IReadOnlyList<JourneySummary> journeySummaries)
    {
        if (options.AiMode == "expected")
        {
            var expected = _fixtures.LoadScenarioArtifact(options.Scenario, "04-active-journey-selection.json")
                .RequireObjectProperty("ai_interpretation")
                .DeepCloneObject();
            return new JourneyInterpretation(
                "accepted",
                expected.RequireStringProperty("suggested_journey_id"),
                expected.RequireProperty("confidence").GetValue<double>(),
                expected.RequireStringProperty("reason_summary"),
                "fixture",
                "expected",
                0,
                null,
                null);
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
                Validate(
                    "journey-not-in-candidate-set",
                    1.2,
                    "Synthetic invalid interpretation for validation coverage.",
                    journeySummaries);
            }
            catch (InvalidDataException ex)
            {
                return Unavailable("invalid_response", ex.Message);
            }
        }

        var prompt = BuildPrompt(session, journeySummaries);
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
            var suggestedJourneyId = interpretation.RequireStringProperty("suggested_journey_id");
            var confidence = interpretation.RequireProperty("confidence").GetValue<double>();
            var reasonSummary = interpretation.RequireStringProperty("reason_summary");
            Validate(suggestedJourneyId, confidence, reasonSummary, journeySummaries);
            return new JourneyInterpretation(
                "accepted", suggestedJourneyId, confidence, reasonSummary,
                "ollama", model, stopwatch.ElapsedMilliseconds, null, null);
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

    private static JsonObject BuildPrompt(SessionContext session, IReadOnlyList<JourneySummary> journeySummaries)
    {
        return new JsonObject
        {
            ["task"] = "Suggest the journey most relevant to this session. Return suggested_journey_id, confidence, and reason_summary.",
            ["session"] = new JsonObject
            {
                ["query_text"] = session.QueryText,
                ["current_url"] = session.CurrentUrl,
                ["entry_point"] = session.EntryPoint,
                ["campaign_theme"] = session.CampaignTheme,
                ["region"] = session.Region,
                ["channel"] = session.Channel,
            },
            ["candidate_journeys"] = new JsonArray(journeySummaries.Select(static summary => (JsonNode)summary.ToJson()).ToArray()),
            ["response_contract"] = new JsonObject
            {
                ["required_fields"] = new JsonArray("suggested_journey_id", "confidence", "reason_summary"),
                ["confidence_range"] = "0.0 to 1.0",
                ["protected_decisions"] = "Do not return eligibility, compliance, suitability, ranking, or action decisions.",
            },
        };
    }

    private static void Validate(
        string suggestedJourneyId,
        double confidence,
        string reason,
        IReadOnlyList<JourneySummary> journeySummaries)
    {
        var validIds = journeySummaries
            .Select(static journey => journey.JourneyId)
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

    private static JourneyInterpretation Unavailable(string reason, string detail) =>
        new("unavailable", null, null, null, null, null, null, reason, detail);
}
