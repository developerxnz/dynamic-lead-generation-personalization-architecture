using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Produces the bounded journey view allowed into live decisioning and AI prompts.
/// </summary>
internal static class JourneySummaryBuilder
{
    public static IReadOnlyList<JourneySummary> Build(IReadOnlyList<JourneyState> journeys)
    {
        return journeys.Select(BuildJourneySummary).ToArray();
    }

    private static JourneySummary BuildJourneySummary(JourneyState journey) => new(
        journey.JourneyId, journey.ServiceCategory, journey.Intent, journey.Stage,
        journey.ResumeCandidate, journey.QualificationState, journey.BehaviorSummary,
        journey.JourneyScore, journey.LastMeaningfulEventAt, journey.AiJourneySummary);
}
