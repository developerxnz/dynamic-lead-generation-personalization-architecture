using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

internal sealed record QualificationState(
    bool CoverageRegionMatch,
    bool ServiceabilityConfirmed,
    IReadOnlyList<string> HardExclusions,
    IReadOnlyList<string> SuppressionFlags);

internal sealed record JourneyState(
    string JourneyId,
    string CustomerId,
    string ServiceCategory,
    string Intent,
    string Stage,
    string Urgency,
    bool ResumeCandidate,
    QualificationState QualificationState,
    JsonObject BehaviorSummary,
    double JourneyScore,
    string AiJourneySummary,
    DateTimeOffset LastMeaningfulEventAt);

internal sealed record SessionContext(
    string CustomerId,
    string SessionId,
    string Channel,
    string EntryPoint,
    string? CampaignTheme,
    string? CurrentUrl,
    string? QueryText,
    string Region,
    DateTimeOffset Timestamp);

internal sealed record JourneySummary(
    string JourneyId,
    string ServiceCategory,
    string Intent,
    string Stage,
    bool ResumeCandidate,
    QualificationState QualificationState,
    JsonObject BehaviorSummary,
    double JourneyScore,
    DateTimeOffset LastMeaningfulEventAt,
    string AiJourneySummary);

internal sealed record JourneyInterpretation(
    string Status,
    string? SuggestedJourneyId,
    double? Confidence,
    string? ReasonSummary,
    string? Source,
    string? ModelVersion,
    long? LatencyMilliseconds,
    string? FallbackReason,
    string? Detail)
{
    public bool IsAccepted => Status == "accepted";
}

internal sealed record ActiveJourneyCandidate(
    JourneySummary Journey,
    double RecencyScore,
    string SessionSignalAlignment,
    bool CampaignAlignment,
    IReadOnlyList<string> SelectionReasons);

internal sealed record ActiveJourneySelection(
    JourneySummary SelectedJourney,
    string SelectionMethod,
    string ReasonSummary,
    IReadOnlyList<ActiveJourneyCandidate> Candidates,
    JourneyInterpretation Interpretation,
    bool DeterministicOverride);

internal static class JourneyContractAdapter
{
    public static IReadOnlyList<JourneyState> JourneyStates(JsonObject payload) =>
        payload.RequireArrayProperty("journeys").OfType<JsonObject>().Select(ToJourneyState).ToArray();

    public static SessionContext Session(JsonObject payload) => new(
        payload.RequireStringProperty("customer_id"),
        payload.RequireStringProperty("session_id"),
        payload.RequireStringProperty("channel"),
        payload.RequireStringProperty("entry_point"),
        payload.OptionalStringProperty("campaign_theme"),
        payload.OptionalStringProperty("current_url"),
        payload.OptionalStringProperty("query_text"),
        payload.RequireStringProperty("region"),
        DateTimeOffset.Parse(payload.RequireStringProperty("timestamp")));

    public static JourneyInterpretation Interpretation(JsonObject payload) => new(
        payload.RequireStringProperty("status"),
        payload.OptionalStringProperty("suggested_journey_id"),
        payload["confidence"]?.GetValue<double>(),
        payload.OptionalStringProperty("reason_summary"),
        payload.OptionalStringProperty("source"),
        payload.OptionalStringProperty("model_version"),
        payload["latency_ms"]?.GetValue<long>(),
        payload.OptionalStringProperty("fallback_reason"),
        payload.OptionalStringProperty("detail"));

    public static JsonObject ToJson(this JourneySummary summary) => new()
    {
        ["journey_id"] = summary.JourneyId,
        ["service_category"] = summary.ServiceCategory,
        ["intent"] = summary.Intent,
        ["stage"] = summary.Stage,
        ["resume_candidate"] = summary.ResumeCandidate,
        ["qualification_state"] = ToJson(summary.QualificationState),
        ["behavior_summary"] = summary.BehaviorSummary.DeepCloneObject(),
        ["journey_score"] = summary.JourneyScore,
        ["last_meaningful_event_at"] = summary.LastMeaningfulEventAt.ToString("O"),
        ["ai_journey_summary"] = summary.AiJourneySummary,
    };

    public static JsonObject ToJson(this JourneyInterpretation interpretation)
    {
        var json = new JsonObject { ["status"] = interpretation.Status };
        if (interpretation.IsAccepted)
        {
            json["source"] = interpretation.Source;
            json["model_version"] = interpretation.ModelVersion;
            json["latency_ms"] = interpretation.LatencyMilliseconds;
            json["suggested_journey_id"] = interpretation.SuggestedJourneyId;
            json["confidence"] = interpretation.Confidence;
            json["reason_summary"] = interpretation.ReasonSummary;
        }
        else
        {
            json["fallback_reason"] = interpretation.FallbackReason;
            json["detail"] = interpretation.Detail;
        }
        return json;
    }

    public static JsonObject ToJson(this ActiveJourneySelection selection) => new()
    {
        ["selected_journey_id"] = selection.SelectedJourney.JourneyId,
        ["selected_service_category"] = selection.SelectedJourney.ServiceCategory,
        ["selection_method"] = selection.SelectionMethod,
        ["reason_summary"] = selection.ReasonSummary,
        ["candidate_journeys"] = new JsonArray(selection.Candidates.Select(candidate => (JsonNode)new JsonObject
        {
            ["journey_id"] = candidate.Journey.JourneyId,
            ["service_category"] = candidate.Journey.ServiceCategory,
            ["journey_score"] = candidate.Journey.JourneyScore,
            ["recency_score"] = candidate.RecencyScore,
            ["session_signal_alignment"] = candidate.SessionSignalAlignment,
            ["campaign_alignment"] = candidate.CampaignAlignment,
            ["selection_reasons"] = new JsonArray(candidate.SelectionReasons.Select(static reason => (JsonNode)reason).ToArray()),
        }).ToArray()),
        ["ai_interpretation"] = selection.Interpretation.ToJson(),
        ["deterministic_override"] = selection.DeterministicOverride,
    };

    private static JourneyState ToJourneyState(JsonObject journey) => new(
        journey.RequireStringProperty("journey_id"),
        journey.RequireStringProperty("customer_id"),
        journey.RequireStringProperty("service_category"),
        journey.RequireStringProperty("intent"),
        journey.RequireStringProperty("stage"),
        journey.RequireStringProperty("urgency"),
        journey.OptionalBoolProperty("resume_candidate"),
        ToQualificationState(journey.RequireObjectProperty("qualification_state")),
        journey.RequireObjectProperty("behavior_summary").DeepCloneObject(),
        journey.RequireObjectProperty("decision_support").RequireProperty("journey_score").GetValue<double>(),
        journey.RequireObjectProperty("decision_support").RequireStringProperty("ai_journey_summary"),
        DateTimeOffset.Parse(journey.RequireStringProperty("last_meaningful_event_at")));

    private static QualificationState ToQualificationState(JsonObject payload) => new(
        payload.OptionalBoolProperty("coverage_region_match"),
        payload.OptionalBoolProperty("serviceability_confirmed"),
        Strings(payload, "hard_exclusions"),
        Strings(payload, "suppression_flags"));

    private static JsonObject ToJson(QualificationState state) => new()
    {
        ["coverage_region_match"] = state.CoverageRegionMatch,
        ["serviceability_confirmed"] = state.ServiceabilityConfirmed,
        ["hard_exclusions"] = new JsonArray(state.HardExclusions.Select(static value => (JsonNode)value).ToArray()),
        ["suppression_flags"] = new JsonArray(state.SuppressionFlags.Select(static value => (JsonNode)value).ToArray()),
    };

    private static IReadOnlyList<string> Strings(JsonObject payload, string propertyName) =>
        payload.RequireArrayProperty(propertyName).Select(static node => node?.GetValue<string>() ?? string.Empty).ToArray();
}
