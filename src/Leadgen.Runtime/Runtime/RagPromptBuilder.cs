using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Serializes the already-selected decision and grounding context for the AI boundary.
/// </summary>
internal sealed class RagPromptBuilder
{
    private readonly GroundingRetriever _groundingRetriever = new();

    public JsonObject Build(
        string scenarioName,
        ScenarioInputs inputs,
        IReadOnlyList<JourneyState> journeys,
        SessionContext session,
        ActiveJourneySelection selection,
        RankingResponse rankingResponse,
        ActivityCatalog catalog,
        JsonObject promptFixture)
    {
        var activeJourney = journeys.FirstOrDefault(journey => journey.JourneyId == selection.SelectedJourney.JourneyId)
            ?? throw new KeyNotFoundException($"Selected journey not found: {selection.SelectedJourney.JourneyId}");
        var selectedAction = rankingResponse.RankedRecommendations.First();
        var grounding = _groundingRetriever.Retrieve(
            inputs,
            session,
            activeJourney,
            selectedAction,
            rankingResponse,
            catalog);
        var prompt = promptFixture.DeepCloneObject();

        prompt["journey_context"] = new JsonObject
        {
            ["journey_id"] = activeJourney.JourneyId,
            ["service_category"] = activeJourney.ServiceCategory,
            ["intent"] = activeJourney.Intent,
            ["stage"] = activeJourney.Stage,
            ["resume_candidate"] = activeJourney.ResumeCandidate,
            ["qualification_state"] = new JsonObject
            {
                ["coverage_region_match"] = activeJourney.QualificationState.CoverageRegionMatch,
                ["serviceability_confirmed"] = activeJourney.QualificationState.ServiceabilityConfirmed,
            },
            ["behavior_summary"] = activeJourney.BehaviorSummary.ToJson(),
            ["journey_score"] = activeJourney.JourneyScore,
            ["last_meaningful_event_at"] = activeJourney.LastMeaningfulEventAt.ToString("O"),
        };
        prompt["customer_context"] = new JsonObject
        {
            ["household_type"] = inputs.Attributes.HouseholdType,
            ["location"] = inputs.Attributes.Location,
        };
        prompt["selected_action"] = new JsonObject
        {
            ["content_id"] = selectedAction.ContentId,
            ["cta_type"] = selectedAction.Cta.Type,
            ["cta_label"] = selectedAction.Cta.Label,
            ["cta_deep_link"] = selectedAction.Cta.DeepLink,
        };
        prompt["grounding_context"] = grounding.ToContextJson();
        prompt["grounding_retrieval"] = grounding.ToDebugJson();
        prompt["scenario"] = scenarioName;
        return prompt;
    }
}
