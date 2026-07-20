using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json;

namespace Leadgen.Runtime;

internal sealed record QualificationState(
    [property: JsonProperty("coverage_region_match")] bool CoverageRegionMatch,
    [property: JsonProperty("serviceability_confirmed")] bool ServiceabilityConfirmed,
    [property: JsonProperty("hard_exclusions")] IReadOnlyList<string> HardExclusions,
    [property: JsonProperty("suppression_flags")] IReadOnlyList<string> SuppressionFlags);

internal sealed record CustomerAttributes(
    [property: JsonProperty("household_type")] string HouseholdType,
    [property: JsonProperty("employment_type")] string EmploymentType,
    [property: JsonProperty("location")] string Location,
    [property: JsonProperty("budget_range")] string BudgetRange,
    [property: JsonProperty("life_stage")] string LifeStage);

internal sealed record JourneyBehaviorSummary(
    [property: JsonProperty("recent_quote_started")] bool RecentQuoteStarted,
    [property: JsonProperty("provider_comparisons_7d")] int ProviderComparisons7d,
    [property: JsonProperty("pages_viewed_7d")] IReadOnlyList<string> PagesViewed7d,
    [property: JsonProperty("quote_started_at", NullValueHandling = NullValueHandling.Ignore)] DateTimeOffset? QuoteStartedAt,
    [property: JsonProperty("quote_abandoned_at", NullValueHandling = NullValueHandling.Ignore)] DateTimeOffset? QuoteAbandonedAt,
    [property: JsonProperty("quote_completion_pct", NullValueHandling = NullValueHandling.Ignore)] int? QuoteCompletionPercentage);

internal sealed record JourneyDecisionSupport(
    [property: JsonProperty("journey_score")] double JourneyScore,
    [property: JsonProperty("ai_journey_summary")] string AiJourneySummary);

/// <summary>
/// Represents the durable customer profile stored in Cosmos.
/// </summary>
internal sealed record CosmosCustomerProfileDocument(
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("customer_id")] string CustomerId,
    [property: JsonProperty("scenario")] string Scenario,
    [property: JsonProperty("description")] string Description,
    [property: JsonProperty("attributes")] CustomerAttributes Attributes,
    [property: JsonProperty("source_session_id")] string SourceSessionId);

/// <summary>
/// Represents the complete journey record stored in Cosmos, rather than only its bounded decisioning summary.
/// </summary>
internal sealed record CosmosJourneyDocument(
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("scenario")] string Scenario,
    [property: JsonProperty("customer_id")] string CustomerId,
    [property: JsonProperty("source_session_id")] string SourceSessionId,
    [property: JsonProperty("journey_id")] string JourneyId,
    [property: JsonProperty("service_category")] string ServiceCategory,
    [property: JsonProperty("status")] string Status,
    [property: JsonProperty("intent")] string Intent,
    [property: JsonProperty("stage")] string Stage,
    [property: JsonProperty("urgency")] string Urgency,
    [property: JsonProperty("switching_intent")] string SwitchingIntent,
    [property: JsonProperty("renewal_window_days")] int? RenewalWindowDays,
    [property: JsonProperty("resume_candidate")] bool ResumeCandidate,
    [property: JsonProperty("qualification_state")] QualificationState QualificationState,
    [property: JsonProperty("behavior_summary")] JourneyBehaviorSummary BehaviorSummary,
    [property: JsonProperty("decision_support")] JourneyDecisionSupport DecisionSupport,
    [property: JsonProperty("last_meaningful_event_at")] DateTimeOffset LastMeaningfulEventAt);

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

internal sealed record AiExplanationResponse(
    string Summary,
    IReadOnlyList<string> KeyPoints,
    string CtaSupportText,
    IReadOnlyList<string> GroundingAssetIds)
{
    public static AiExplanationResponse FromJson(JsonObject payload) => new(
        payload.RequireStringProperty("summary"),
        payload.RequireArrayProperty("key_points").Select(static value => value?.GetValue<string>() ?? string.Empty).ToArray(),
        payload.RequireStringProperty("cta_support_text"),
        payload.RequireArrayProperty("grounding_asset_ids").Select(static value => value?.GetValue<string>() ?? string.Empty).ToArray());

    public JsonObject ToJson() => new()
    {
        ["summary"] = Summary,
        ["key_points"] = new JsonArray(KeyPoints.Select(static value => (JsonNode)value).ToArray()),
        ["cta_support_text"] = CtaSupportText,
        ["grounding_asset_ids"] = new JsonArray(GroundingAssetIds.Select(static value => (JsonNode)value).ToArray()),
    };
}

internal sealed record AnalyticsEvent(
    string EventType,
    string CustomerId,
    string SessionId,
    string JourneyId,
    DateTimeOffset Timestamp,
    JsonObject Metadata,
    string? Note)
{
    public static AnalyticsEvent FromJson(JsonObject payload) => new(
        payload.RequireStringProperty("event_type"),
        payload.RequireStringProperty("customer_id"),
        payload.RequireStringProperty("session_id"),
        payload.RequireStringProperty("journey_id"),
        DateTimeOffset.Parse(payload.RequireStringProperty("timestamp")),
        payload.RequireObjectProperty("metadata").DeepCloneObject(),
        payload.OptionalStringProperty("note"));

    public JsonObject ToJson()
    {
        var json = new JsonObject
        {
            ["event_type"] = EventType,
            ["customer_id"] = CustomerId,
            ["session_id"] = SessionId,
            ["journey_id"] = JourneyId,
            ["timestamp"] = Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["metadata"] = Metadata.DeepCloneObject(),
        };
        if (Note is not null)
        {
            json["note"] = Note;
        }
        return json;
    }
}

internal sealed record DecisionTraceDocument(
    string Id,
    string CustomerId,
    string Scenario,
    FinalResponseEnvelope FinalResponse,
    JourneyInterpretation JourneyInterpretation)
{
    public JsonObject ToJson() => new()
    {
        ["id"] = Id,
        ["customer_id"] = CustomerId,
        ["scenario"] = Scenario,
        ["final_response"] = FinalResponse.ToJson(),
        ["journey_interpretation"] = JourneyInterpretation.ToJson(),
    };
}

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
        OptionalLong(payload["latency_ms"]),
        payload.OptionalStringProperty("fallback_reason"),
        payload.OptionalStringProperty("detail"));

    public static CosmosCustomerProfileDocument? CustomerProfile(
        JsonObject? profile,
        SessionContext session)
    {
        if (profile is null)
        {
            return null;
        }

        return new CosmosCustomerProfileDocument(
            session.CustomerId,
            profile.RequireStringProperty("customer_id"),
            profile.RequireStringProperty("scenario"),
            profile.RequireStringProperty("description"),
            CustomerAttributes(profile.RequireObjectProperty("attributes")),
            session.SessionId);
    }

    public static IReadOnlyList<CosmosJourneyDocument> CosmosJourneys(
        JsonObject payload,
        SessionContext session)
    {
        var scenario = payload.RequireStringProperty("scenario");
        var customerId = payload.RequireStringProperty("customer_id");
        return payload.RequireArrayProperty("journeys")
            .OfType<JsonObject>()
            .Select(journey => CosmosJourney(journey, scenario, customerId, session.SessionId))
            .ToArray();
    }

    public static JsonObject ToFixtureJson(this CosmosCustomerProfileDocument profile) => new()
    {
        ["id"] = profile.Id,
        ["customer_id"] = profile.CustomerId,
        ["scenario"] = profile.Scenario,
        ["description"] = profile.Description,
        ["attributes"] = ToJson(profile.Attributes),
        ["source_session_id"] = profile.SourceSessionId,
    };

    public static JsonObject ToFixtureJson(this CosmosJourneyDocument journey) => new()
    {
        ["id"] = journey.Id,
        ["scenario"] = journey.Scenario,
        ["customer_id"] = journey.CustomerId,
        ["source_session_id"] = journey.SourceSessionId,
        ["journey_id"] = journey.JourneyId,
        ["service_category"] = journey.ServiceCategory,
        ["status"] = journey.Status,
        ["intent"] = journey.Intent,
        ["stage"] = journey.Stage,
        ["urgency"] = journey.Urgency,
        ["switching_intent"] = journey.SwitchingIntent,
        ["renewal_window_days"] = journey.RenewalWindowDays,
        ["resume_candidate"] = journey.ResumeCandidate,
        ["qualification_state"] = ToJson(journey.QualificationState),
        ["behavior_summary"] = ToJson(journey.BehaviorSummary),
        ["decision_support"] = ToJson(journey.DecisionSupport),
        ["last_meaningful_event_at"] = Timestamp(journey.LastMeaningfulEventAt),
    };

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

    private static CosmosJourneyDocument CosmosJourney(
        JsonObject journey,
        string scenario,
        string customerId,
        string sourceSessionId) => new(
        journey.RequireStringProperty("journey_id"),
        scenario,
        customerId,
        sourceSessionId,
        journey.RequireStringProperty("journey_id"),
        journey.RequireStringProperty("service_category"),
        journey.RequireStringProperty("status"),
        journey.RequireStringProperty("intent"),
        journey.RequireStringProperty("stage"),
        journey.RequireStringProperty("urgency"),
        journey.RequireStringProperty("switching_intent"),
        journey["renewal_window_days"]?.GetValue<int>(),
        journey.OptionalBoolProperty("resume_candidate"),
        ToQualificationState(journey.RequireObjectProperty("qualification_state")),
        JourneyBehaviorSummary(journey.RequireObjectProperty("behavior_summary")),
        JourneyDecisionSupport(journey.RequireObjectProperty("decision_support")),
        DateTimeOffset.Parse(journey.RequireStringProperty("last_meaningful_event_at")));

    private static CustomerAttributes CustomerAttributes(JsonObject payload) => new(
        payload.RequireStringProperty("household_type"),
        payload.RequireStringProperty("employment_type"),
        payload.RequireStringProperty("location"),
        payload.RequireStringProperty("budget_range"),
        payload.RequireStringProperty("life_stage"));

    private static JourneyBehaviorSummary JourneyBehaviorSummary(JsonObject payload) => new(
        payload.OptionalBoolProperty("recent_quote_started"),
        payload.RequireProperty("provider_comparisons_7d").GetValue<int>(),
        Strings(payload, "pages_viewed_7d"),
        OptionalTimestamp(payload, "quote_started_at"),
        OptionalTimestamp(payload, "quote_abandoned_at"),
        payload["quote_completion_pct"]?.GetValue<int>());

    private static JourneyDecisionSupport JourneyDecisionSupport(JsonObject payload) => new(
        payload.RequireProperty("journey_score").GetValue<double>(),
        payload.RequireStringProperty("ai_journey_summary"));

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

    private static JsonObject ToJson(CustomerAttributes attributes) => new()
    {
        ["household_type"] = attributes.HouseholdType,
        ["employment_type"] = attributes.EmploymentType,
        ["location"] = attributes.Location,
        ["budget_range"] = attributes.BudgetRange,
        ["life_stage"] = attributes.LifeStage,
    };

    private static JsonObject ToJson(JourneyBehaviorSummary behavior)
    {
        var json = new JsonObject
        {
            ["recent_quote_started"] = behavior.RecentQuoteStarted,
            ["provider_comparisons_7d"] = behavior.ProviderComparisons7d,
            ["pages_viewed_7d"] = new JsonArray(behavior.PagesViewed7d.Select(static value => (JsonNode)value).ToArray()),
        };
        if (behavior.QuoteStartedAt is { } startedAt)
        {
            json["quote_started_at"] = Timestamp(startedAt);
        }
        if (behavior.QuoteAbandonedAt is { } abandonedAt)
        {
            json["quote_abandoned_at"] = Timestamp(abandonedAt);
        }
        if (behavior.QuoteCompletionPercentage is { } completionPercentage)
        {
            json["quote_completion_pct"] = completionPercentage;
        }
        return json;
    }

    private static JsonObject ToJson(JourneyDecisionSupport decisionSupport) => new()
    {
        ["journey_score"] = decisionSupport.JourneyScore,
        ["ai_journey_summary"] = decisionSupport.AiJourneySummary,
    };

    private static DateTimeOffset? OptionalTimestamp(JsonObject payload, string propertyName) =>
        payload.OptionalStringProperty(propertyName) is { } timestamp
            ? DateTimeOffset.Parse(timestamp)
            : null;

    private static string Timestamp(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

    private static IReadOnlyList<string> Strings(JsonObject payload, string propertyName) =>
        payload.RequireArrayProperty(propertyName).Select(static node => node?.GetValue<string>() ?? string.Empty).ToArray();

    private static long? OptionalLong(JsonNode? value)
    {
        if (value is not JsonValue jsonValue)
        {
            return null;
        }

        if (jsonValue.TryGetValue<long>(out var longValue))
        {
            return longValue;
        }

        return jsonValue.TryGetValue<int>(out var intValue) ? intValue : null;
    }
}
