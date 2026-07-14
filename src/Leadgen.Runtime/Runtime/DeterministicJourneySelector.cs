namespace Leadgen.Runtime;

/// <summary>
/// Selects the active journey from deterministic session and state evidence.
/// </summary>
internal static class DeterministicJourneySelector
{
    public static ActiveJourneySelection Select(
        IReadOnlyList<JourneySummary> journeySummaries,
        SessionContext session,
        JourneyInterpretation interpretation)
    {
        var candidates = journeySummaries
            .Select(journey => BuildCandidate(journey, session))
            .OrderByDescending(static candidate => candidate.Score)
            .ThenByDescending(static candidate => candidate.Candidate.Journey.LastMeaningfulEventAt)
            .ToArray();
        var selected = candidates[0].Candidate;
        var overrideRequired = interpretation.IsAccepted
            && interpretation.SuggestedJourneyId != selected.Journey.JourneyId
            && selected.SessionSignalAlignment == "strong";

        return new ActiveJourneySelection(
            selected.Journey,
            candidates.Length == 1 ? "deterministic_single_journey" : "deterministic_with_ai_support",
            ReasonSummary(selected, candidates.Length, interpretation, overrideRequired),
            candidates.Select(static candidate => candidate.Candidate).ToArray(),
            interpretation,
            overrideRequired);
    }

    private static ScoredCandidate BuildCandidate(JourneySummary journey, SessionContext session)
    {
        var evidence = string.Join(" ", new[]
        {
            session.QueryText, session.CurrentUrl, session.CampaignTheme, session.EntryPoint,
        }.Where(static value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();
        var aliases = Aliases(journey.ServiceCategory);
        var url = session.CurrentUrl?.ToLowerInvariant() ?? string.Empty;
        var campaign = session.CampaignTheme?.ToLowerInvariant() ?? string.Empty;
        var urlAligned = aliases.Any(url.Contains);
        var campaignAligned = aliases.Any(campaign.Contains);
        var evidenceAligned = aliases.Any(evidence.Contains);
        var alignment = urlAligned || campaignAligned ? "strong" : evidenceAligned ? "medium" : "weak";
        var alignmentScore = alignment == "strong" ? 2.0 : alignment == "medium" ? 1.0 : 0.0;
        var recencyScore = Math.Max(0.0, 1.0 - Math.Min(1.0, (DateTimeOffset.UtcNow - journey.LastMeaningfulEventAt).TotalDays / 90.0));
        var score = journey.JourneyScore + alignmentScore + (campaignAligned ? 0.5 : 0)
            + (journey.ResumeCandidate ? 0.1 : 0) + (recencyScore * 0.1);
        var reasons = new List<string>();
        if (urlAligned) reasons.Add("Current URL aligns with this journey.");
        if (campaignAligned) reasons.Add("Campaign theme aligns with this journey.");
        if (!urlAligned && evidenceAligned) reasons.Add("Current session evidence aligns with this journey.");
        if (journey.ResumeCandidate) reasons.Add("Saved progression supports a low-friction resume.");
        if (reasons.Count == 0) reasons.Add("No direct session alignment; deterministic journey score and recency remain available.");

        return new ScoredCandidate(
            new ActiveJourneyCandidate(journey, recencyScore, alignment, campaignAligned, reasons),
            score);
    }

    private static string[] Aliases(string category) => category switch
    {
        "health_insurance" => new[] { "health", "cover", "insurance" },
        "broadband" => new[] { "broadband", "internet", "wifi", "moving-home", "move-home" },
        "novated_leasing" => new[] { "novated", "lease", "leasing", "vehicle", "car" },
        _ => category.Split('_', StringSplitOptions.RemoveEmptyEntries),
    };

    private static string ReasonSummary(
        ActiveJourneyCandidate selected,
        int candidateCount,
        JourneyInterpretation interpretation,
        bool overrideRequired) =>
        candidateCount == 1
            ? "Only one active journey is available for this customer."
            : overrideRequired
                ? "Deterministic current-session evidence overrides the AI journey suggestion."
                : !interpretation.IsAccepted
                    ? "AI interpretation was unavailable, so deterministic session and journey evidence selected the active journey."
                    : $"Deterministic session alignment selected the {selected.Journey.ServiceCategory} journey.";

    private sealed record ScoredCandidate(ActiveJourneyCandidate Candidate, double Score);
}
