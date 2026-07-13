using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Selects the active journey from deterministic session and state evidence.
/// </summary>
internal static class DeterministicJourneySelector
{
    public static JsonObject Select(JsonArray journeySummaries, JsonObject session, JsonObject interpretation)
    {
        var candidates = journeySummaries.OfType<JsonObject>()
            .Select(journey => BuildCandidate(journey, session))
            .OrderByDescending(static candidate => candidate.Score)
            .ThenByDescending(static candidate => candidate.Journey.RequireStringProperty("last_meaningful_event_at"), StringComparer.Ordinal)
            .ToArray();
        var selected = candidates.First();
        var aiSuggestedJourneyId = interpretation.OptionalStringProperty("suggested_journey_id");
        var aiAccepted = interpretation.OptionalStringProperty("status") == "accepted";
        var overrideRequired = aiAccepted
            && aiSuggestedJourneyId != selected.Journey.RequireStringProperty("journey_id")
            && selected.Alignment == "strong";

        return new JsonObject
        {
            ["selected_journey_id"] = selected.Journey.RequireProperty("journey_id").DeepClone(),
            ["selected_service_category"] = selected.Journey.RequireProperty("service_category").DeepClone(),
            ["selection_method"] = candidates.Length == 1
                ? "deterministic_single_journey"
                : "deterministic_with_ai_support",
            ["reason_summary"] = ReasonSummary(selected, candidates.Length, interpretation, overrideRequired),
            ["candidate_journeys"] = new JsonArray(candidates.Select(candidate => (JsonNode)new JsonObject
            {
                ["journey_id"] = candidate.Journey.RequireProperty("journey_id").DeepClone(),
                ["service_category"] = candidate.Journey.RequireProperty("service_category").DeepClone(),
                ["journey_score"] = candidate.Journey.RequireProperty("journey_score").DeepClone(),
                ["recency_score"] = candidate.RecencyScore,
                ["session_signal_alignment"] = candidate.Alignment,
                ["campaign_alignment"] = candidate.CampaignAligned,
                ["selection_reasons"] = new JsonArray(candidate.Reasons.Select(static reason => (JsonNode)reason).ToArray()),
            }).ToArray()),
            ["ai_interpretation"] = interpretation.DeepCloneObject(),
            ["deterministic_override"] = overrideRequired,
        };
    }

    private static Candidate BuildCandidate(JsonObject journey, JsonObject session)
    {
        var category = journey.RequireStringProperty("service_category");
        var evidence = string.Join(" ", new[]
        {
            session.OptionalStringProperty("query_text"),
            session.OptionalStringProperty("current_url"),
            session.OptionalStringProperty("campaign_theme"),
            session.OptionalStringProperty("entry_point"),
        }.Where(static value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();
        var aliases = Aliases(category);
        var url = session.OptionalStringProperty("current_url")?.ToLowerInvariant() ?? string.Empty;
        var campaign = session.OptionalStringProperty("campaign_theme")?.ToLowerInvariant() ?? string.Empty;
        var urlAligned = aliases.Any(url.Contains);
        var campaignAligned = aliases.Any(campaign.Contains);
        var evidenceAligned = aliases.Any(evidence.Contains);
        var alignment = urlAligned || campaignAligned ? "strong" : evidenceAligned ? "medium" : "weak";
        var alignmentScore = alignment switch
        {
            "strong" => 2.0,
            "medium" => 1.0,
            _ => 0.0,
        };
        var recencyScore = DateTimeOffset.TryParse(journey.RequireStringProperty("last_meaningful_event_at"), out var lastEvent)
            ? Math.Max(0.0, 1.0 - Math.Min(1.0, (DateTimeOffset.UtcNow - lastEvent).TotalDays / 90.0))
            : 0.0;
        var score = journey.RequireProperty("journey_score").GetValue<double>()
            + alignmentScore
            + (campaignAligned ? 0.5 : 0)
            + (journey.OptionalBoolProperty("resume_candidate") ? 0.1 : 0)
            + (recencyScore * 0.1);
        var reasons = new List<string>();
        if (urlAligned)
        {
            reasons.Add("Current URL aligns with this journey.");
        }
        if (campaignAligned)
        {
            reasons.Add("Campaign theme aligns with this journey.");
        }
        if (!urlAligned && evidenceAligned)
        {
            reasons.Add("Current session evidence aligns with this journey.");
        }
        if (journey.OptionalBoolProperty("resume_candidate"))
        {
            reasons.Add("Saved progression supports a low-friction resume.");
        }
        if (reasons.Count == 0)
        {
            reasons.Add("No direct session alignment; deterministic journey score and recency remain available.");
        }

        return new Candidate(journey, score, recencyScore, alignment, campaignAligned, reasons);
    }

    private static string[] Aliases(string category) => category switch
    {
        "health_insurance" => new[] { "health", "cover", "insurance" },
        "broadband" => new[] { "broadband", "internet", "wifi", "moving-home", "move-home" },
        "novated_leasing" => new[] { "novated", "lease", "leasing", "vehicle", "car" },
        _ => category.Split('_', StringSplitOptions.RemoveEmptyEntries),
    };

    private static string ReasonSummary(Candidate selected, int candidateCount, JsonObject interpretation, bool overrideRequired)
    {
        if (candidateCount == 1)
        {
            return "Only one active journey is available for this customer.";
        }

        if (overrideRequired)
        {
            return "Deterministic current-session evidence overrides the AI journey suggestion.";
        }

        if (interpretation.OptionalStringProperty("status") == "unavailable")
        {
            return "AI interpretation was unavailable, so deterministic session and journey evidence selected the active journey.";
        }

        return $"Deterministic session alignment selected the {selected.Journey.RequireStringProperty("service_category")} journey.";
    }

    private sealed record Candidate(
        JsonObject Journey,
        double Score,
        double RecencyScore,
        string Alignment,
        bool CampaignAligned,
        IReadOnlyList<string> Reasons);
}
