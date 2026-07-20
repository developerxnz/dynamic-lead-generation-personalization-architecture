using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Typed contracts for the runtime's ranking, channel-response, and analytics outputs.
/// JSON is adapted only when crossing fixture, artifact, prompt, or Cosmos boundaries.
/// </summary>
internal sealed record RankingRequest(
    string Scenario,
    string Description,
    RankingCustomerProfile CustomerProfile,
    RankingJourney ActiveJourney,
    RankingAiContext? AiContext,
    RankingContext Context,
    IReadOnlyList<RankingCandidate> Candidates,
    string RankingPolicyVersion,
    IReadOnlyList<SupplementaryJourney> SupplementaryJourneys);

internal sealed record RankingCustomerProfile(
    string CustomerId,
    string Location,
    string HouseholdType);

internal sealed record RankingJourney(
    string JourneyId,
    string ServiceCategory,
    string Intent,
    string Stage,
    string Urgency,
    bool ResumeCandidate,
    QualificationState QualificationState);

internal sealed record RankingAiContext(
    string? SuggestedJourneyId,
    string? SuggestedServiceCategory,
    bool DeterministicOverrideRequired);

internal sealed record RankingContext(
    string Channel,
    string CampaignSource,
    string? CampaignTheme,
    string SessionId,
    bool? MustEnforceEligibility,
    bool? MustEnforceLifecycleRules,
    bool? MustEnforceCompliance,
    string? RegulatoryRegion);

internal sealed record RankingCandidate(
    string ContentId,
    string AssetType,
    string ServiceCategory,
    string CtaType,
    string CtaDeepLink,
    string? Provider,
    int Priority,
    string FunnelStage,
    string RetrievalSource,
    IReadOnlyList<string>? ComplianceFlags);

internal sealed record SupplementaryJourney(
    string JourneyId,
    string ServiceCategory,
    string Intent,
    string Stage,
    bool SecondaryOnly);

internal sealed record RankingResponse(
    string Scenario,
    string Description,
    IReadOnlyList<RankedRecommendation> RankedRecommendations,
    IReadOnlyList<SuppressedCandidate> SuppressedCandidates,
    string RankingPolicyVersion,
    long RankingDurationMilliseconds);

internal sealed record RankedRecommendation(
    string ContentId,
    int Score,
    RecommendationCta Cta,
    IReadOnlyList<string> Reasons);

internal sealed record RecommendationCta(string Type, string Label, string DeepLink);

internal sealed record SuppressedCandidate(string ContentId, string Reason);

internal sealed record FinalResponseEnvelope(
    string Scenario,
    string Description,
    string CustomerId,
    string SessionId,
    FinalActiveJourney ActiveJourney,
    FinalNextBestAction NextBestAction,
    IReadOnlyList<SupportingContent> SupportingContent,
    SecondaryJourneyPrompt? SecondaryJourneyPrompt,
    FinalExplanation Explanation,
    DecisionTrace DecisionTrace,
    string MetadataRevision,
    DateTimeOffset ResponseGeneratedAt);

internal sealed record FinalActiveJourney(string JourneyId, string ServiceCategory);

internal sealed record FinalNextBestAction(
    string ContentId,
    string CtaType,
    string Label,
    string DeepLink,
    int RankingScore,
    string RankingPolicyVersion);

internal sealed record SupportingContent(string ContentId, string CtaType, string Label, string DeepLink);

internal sealed record SecondaryJourneyPrompt(
    string JourneyId,
    string ServiceCategory,
    string Label,
    string DeepLink,
    string ContentId);

internal sealed record FinalExplanation(
    string Source,
    string? AiResponseId,
    string Summary,
    string CtaSupportText,
    IReadOnlyList<string> GroundingAssetIds);

internal sealed record DecisionTrace(
    string ProfileRead,
    string JourneyRead,
    string ActiveJourneySelected,
    string Retrieval,
    string Filtering,
    string Ranking,
    string AiExplanation);

internal sealed record AnalyticsEnvelope(
    string Scenario,
    string Description,
    IReadOnlyList<AnalyticsEvent> Events);

internal static class RuntimeOutputContractAdapter
{
    public static RankingRequest RankingRequest(JsonObject payload) => new(
        payload.RequireStringProperty("scenario"),
        payload.RequireStringProperty("description"),
        RankingCustomerProfile(payload.RequireObjectProperty("customer_profile")),
        RankingJourney(payload.RequireObjectProperty("active_journey")),
        payload["ai_context"] is JsonObject aiContext ? RankingAiContext(aiContext) : null,
        RankingContext(payload.RequireObjectProperty("context")),
        payload.RequireArrayProperty("candidates").OfType<JsonObject>().Select(RankingCandidate).ToArray(),
        payload.RequireStringProperty("ranking_policy_version"),
        payload["supplementary_journeys"] is JsonArray supplementaryJourneys
            ? supplementaryJourneys.OfType<JsonObject>().Select(SupplementaryJourney).ToArray()
            : Array.Empty<SupplementaryJourney>());

    public static RankingResponse RankingResponse(JsonObject payload) => new(
        payload.RequireStringProperty("scenario"),
        payload.RequireStringProperty("description"),
        payload.RequireArrayProperty("ranked_recommendations").OfType<JsonObject>().Select(RankedRecommendation).ToArray(),
        payload.RequireArrayProperty("suppressed_candidates").OfType<JsonObject>().Select(SuppressedCandidate).ToArray(),
        payload.RequireStringProperty("ranking_policy_version"),
        payload.RequireProperty("ranking_duration_ms").GetValue<long>());

    public static FinalResponseEnvelope FinalResponse(JsonObject payload) => new(
        payload.RequireStringProperty("scenario"),
        payload.RequireStringProperty("description"),
        payload.RequireStringProperty("customer_id"),
        payload.RequireStringProperty("session_id"),
        FinalActiveJourney(payload.RequireObjectProperty("active_journey")),
        FinalNextBestAction(payload.RequireObjectProperty("next_best_action")),
        payload.RequireArrayProperty("supporting_content").OfType<JsonObject>().Select(SupportingContent).ToArray(),
        payload["secondary_journey_prompt"] is JsonObject secondaryJourneyPrompt
            ? SecondaryJourneyPrompt(secondaryJourneyPrompt)
            : null,
        FinalExplanation(payload.RequireObjectProperty("explanation")),
        DecisionTrace(payload.RequireObjectProperty("decision_trace")),
        payload.RequireStringProperty("metadata_revision"),
        DateTimeOffset.Parse(payload.RequireStringProperty("response_generated_at")));

    public static AnalyticsEnvelope Analytics(JsonObject payload) => new(
        payload.RequireStringProperty("scenario"),
        payload.RequireStringProperty("description"),
        payload.RequireArrayProperty("events").OfType<JsonObject>().Select(AnalyticsEvent.FromJson).ToArray());

    public static JsonObject ToJson(this RankingRequest request)
    {
        var json = new JsonObject
        {
            ["scenario"] = request.Scenario,
            ["description"] = request.Description,
            ["customer_profile"] = ToJson(request.CustomerProfile),
            ["active_journey"] = ToJson(request.ActiveJourney),
        };
        if (request.AiContext is { } aiContext)
        {
            json["ai_context"] = ToJson(aiContext);
        }
        if (request.SupplementaryJourneys.Count > 0)
        {
            json["supplementary_journeys"] = new JsonArray(request.SupplementaryJourneys
                .Select(journey => (JsonNode)ToJson(journey)).ToArray());
        }
        json["context"] = ToJson(request.Context);
        json["candidates"] = new JsonArray(request.Candidates.Select(candidate => (JsonNode)ToJson(candidate)).ToArray());
        json["ranking_policy_version"] = request.RankingPolicyVersion;
        return json;
    }

    public static JsonObject ToJson(this RankingResponse response) => new()
    {
        ["scenario"] = response.Scenario,
        ["description"] = response.Description,
        ["ranked_recommendations"] = new JsonArray(response.RankedRecommendations
            .Select(recommendation => (JsonNode)ToJson(recommendation)).ToArray()),
        ["suppressed_candidates"] = new JsonArray(response.SuppressedCandidates
            .Select(candidate => (JsonNode)ToJson(candidate)).ToArray()),
        ["ranking_policy_version"] = response.RankingPolicyVersion,
        ["ranking_duration_ms"] = response.RankingDurationMilliseconds,
    };

    public static JsonObject ToJson(this FinalResponseEnvelope response) => new()
    {
        ["scenario"] = response.Scenario,
        ["description"] = response.Description,
        ["customer_id"] = response.CustomerId,
        ["session_id"] = response.SessionId,
        ["active_journey"] = ToJson(response.ActiveJourney),
        ["next_best_action"] = ToJson(response.NextBestAction),
        ["supporting_content"] = new JsonArray(response.SupportingContent
            .Select(content => (JsonNode)ToJson(content)).ToArray()),
        ["secondary_journey_prompt"] = response.SecondaryJourneyPrompt is { } prompt ? ToJson(prompt) : null,
        ["explanation"] = ToJson(response.Explanation),
        ["decision_trace"] = ToJson(response.DecisionTrace),
        ["metadata_revision"] = response.MetadataRevision,
        ["response_generated_at"] = Timestamp(response.ResponseGeneratedAt),
    };

    public static JsonObject ToJson(this AnalyticsEnvelope analytics) => new()
    {
        ["scenario"] = analytics.Scenario,
        ["description"] = analytics.Description,
        ["events"] = new JsonArray(analytics.Events.Select(@event => (JsonNode)@event.ToJson()).ToArray()),
    };

    private static RankingCustomerProfile RankingCustomerProfile(JsonObject payload) => new(
        payload.RequireStringProperty("customer_id"),
        payload.RequireStringProperty("location"),
        payload.RequireStringProperty("household_type"));

    private static RankingJourney RankingJourney(JsonObject payload) => new(
        payload.RequireStringProperty("journey_id"),
        payload.RequireStringProperty("service_category"),
        payload.RequireStringProperty("intent"),
        payload.RequireStringProperty("stage"),
        payload.RequireStringProperty("urgency"),
        payload.OptionalBoolProperty("resume_candidate"),
        ToQualificationState(payload.RequireObjectProperty("qualification_state")));

    private static RankingAiContext RankingAiContext(JsonObject payload) => new(
        payload.OptionalStringProperty("suggested_journey_id"),
        payload.OptionalStringProperty("suggested_service_category"),
        payload.OptionalBoolProperty("deterministic_override_required"));

    private static RankingContext RankingContext(JsonObject payload) => new(
        payload.RequireStringProperty("channel"),
        payload.RequireStringProperty("campaign_source"),
        payload.OptionalStringProperty("campaign_theme"),
        payload.RequireStringProperty("session_id"),
        OptionalBoolean(payload, "must_enforce_eligibility"),
        OptionalBoolean(payload, "must_enforce_lifecycle_rules"),
        OptionalBoolean(payload, "must_enforce_compliance"),
        payload.OptionalStringProperty("regulatory_region"));

    private static RankingCandidate RankingCandidate(JsonObject payload) => new(
        payload.RequireStringProperty("content_id"),
        payload.RequireStringProperty("asset_type"),
        payload.RequireStringProperty("service_category"),
        payload.RequireStringProperty("cta_type"),
        payload.RequireStringProperty("cta_deep_link"),
        payload.OptionalStringProperty("provider"),
        payload.RequireProperty("priority").GetValue<int>(),
        payload.RequireStringProperty("funnel_stage"),
        payload.RequireStringProperty("retrieval_source"),
        payload["compliance_flags"] is JsonArray complianceFlags
            ? complianceFlags.Select(static flag => flag?.GetValue<string>() ?? string.Empty).ToArray()
            : null);

    private static SupplementaryJourney SupplementaryJourney(JsonObject payload) => new(
        payload.RequireStringProperty("journey_id"),
        payload.RequireStringProperty("service_category"),
        payload.RequireStringProperty("intent"),
        payload.RequireStringProperty("stage"),
        payload.OptionalBoolProperty("secondary_only"));

    private static RankedRecommendation RankedRecommendation(JsonObject payload) => new(
        payload.RequireStringProperty("content_id"),
        payload.RequireProperty("score").GetValue<int>(),
        RecommendationCta(payload.RequireObjectProperty("cta")),
        payload.RequireArrayProperty("reasons").Select(static reason => reason?.GetValue<string>() ?? string.Empty).ToArray());

    private static RecommendationCta RecommendationCta(JsonObject payload) => new(
        payload.RequireStringProperty("type"),
        payload.RequireStringProperty("label"),
        payload.RequireStringProperty("deep_link"));

    private static SuppressedCandidate SuppressedCandidate(JsonObject payload) => new(
        payload.RequireStringProperty("content_id"),
        payload.RequireStringProperty("reason"));

    private static FinalActiveJourney FinalActiveJourney(JsonObject payload) => new(
        payload.RequireStringProperty("journey_id"),
        payload.RequireStringProperty("service_category"));

    private static FinalNextBestAction FinalNextBestAction(JsonObject payload) => new(
        payload.RequireStringProperty("content_id"),
        payload.RequireStringProperty("cta_type"),
        payload.RequireStringProperty("label"),
        payload.RequireStringProperty("deep_link"),
        payload.RequireProperty("ranking_score").GetValue<int>(),
        payload.RequireStringProperty("ranking_policy_version"));

    private static SupportingContent SupportingContent(JsonObject payload) => new(
        payload.RequireStringProperty("content_id"),
        payload.RequireStringProperty("cta_type"),
        payload.RequireStringProperty("label"),
        payload.RequireStringProperty("deep_link"));

    private static SecondaryJourneyPrompt SecondaryJourneyPrompt(JsonObject payload) => new(
        payload.RequireStringProperty("journey_id"),
        payload.RequireStringProperty("service_category"),
        payload.RequireStringProperty("label"),
        payload.RequireStringProperty("deep_link"),
        payload.RequireStringProperty("content_id"));

    private static FinalExplanation FinalExplanation(JsonObject payload) => new(
        payload.RequireStringProperty("source"),
        payload.OptionalStringProperty("ai_response_id"),
        payload.RequireStringProperty("summary"),
        payload.RequireStringProperty("cta_support_text"),
        payload.RequireArrayProperty("grounding_asset_ids").Select(static id => id?.GetValue<string>() ?? string.Empty).ToArray());

    private static DecisionTrace DecisionTrace(JsonObject payload) => new(
        payload.RequireStringProperty("profile_read"),
        payload.RequireStringProperty("journey_read"),
        payload.RequireStringProperty("active_journey_selected"),
        payload.RequireStringProperty("retrieval"),
        payload.RequireStringProperty("filtering"),
        payload.RequireStringProperty("ranking"),
        payload.RequireStringProperty("ai_explanation"));

    private static JsonObject ToJson(RankingCustomerProfile profile) => new()
    {
        ["customer_id"] = profile.CustomerId,
        ["location"] = profile.Location,
        ["household_type"] = profile.HouseholdType,
    };

    private static JsonObject ToJson(RankingJourney journey) => new()
    {
        ["journey_id"] = journey.JourneyId,
        ["service_category"] = journey.ServiceCategory,
        ["intent"] = journey.Intent,
        ["stage"] = journey.Stage,
        ["urgency"] = journey.Urgency,
        ["resume_candidate"] = journey.ResumeCandidate,
        ["qualification_state"] = ToJson(journey.QualificationState),
    };

    private static JsonObject ToJson(RankingAiContext context) => new()
    {
        ["suggested_journey_id"] = context.SuggestedJourneyId,
        ["suggested_service_category"] = context.SuggestedServiceCategory,
        ["deterministic_override_required"] = context.DeterministicOverrideRequired,
    };

    private static JsonObject ToJson(RankingContext context)
    {
        var json = new JsonObject
        {
            ["channel"] = context.Channel,
            ["campaign_source"] = context.CampaignSource,
            ["campaign_theme"] = context.CampaignTheme,
            ["session_id"] = context.SessionId,
        };
        AddOptional(json, "must_enforce_eligibility", context.MustEnforceEligibility);
        AddOptional(json, "must_enforce_lifecycle_rules", context.MustEnforceLifecycleRules);
        AddOptional(json, "regulatory_region", context.RegulatoryRegion);
        AddOptional(json, "must_enforce_compliance", context.MustEnforceCompliance);
        return json;
    }

    private static JsonObject ToJson(RankingCandidate candidate)
    {
        var json = new JsonObject
        {
            ["content_id"] = candidate.ContentId,
            ["asset_type"] = candidate.AssetType,
            ["service_category"] = candidate.ServiceCategory,
            ["cta_type"] = candidate.CtaType,
            ["cta_deep_link"] = candidate.CtaDeepLink,
            ["provider"] = candidate.Provider,
            ["priority"] = candidate.Priority,
            ["funnel_stage"] = candidate.FunnelStage,
            ["retrieval_source"] = candidate.RetrievalSource,
        };
        if (candidate.ComplianceFlags is not null)
        {
            json["compliance_flags"] = new JsonArray(candidate.ComplianceFlags.Select(flag => (JsonNode)flag).ToArray());
        }
        return json;
    }

    private static JsonObject ToJson(SupplementaryJourney journey) => new()
    {
        ["journey_id"] = journey.JourneyId,
        ["service_category"] = journey.ServiceCategory,
        ["intent"] = journey.Intent,
        ["stage"] = journey.Stage,
        ["secondary_only"] = journey.SecondaryOnly,
    };

    private static JsonObject ToJson(RankedRecommendation recommendation) => new()
    {
        ["content_id"] = recommendation.ContentId,
        ["score"] = recommendation.Score,
        ["cta"] = ToJson(recommendation.Cta),
        ["reasons"] = new JsonArray(recommendation.Reasons.Select(reason => (JsonNode)reason).ToArray()),
    };

    private static JsonObject ToJson(RecommendationCta cta) => new()
    {
        ["type"] = cta.Type,
        ["label"] = cta.Label,
        ["deep_link"] = cta.DeepLink,
    };

    private static JsonObject ToJson(SuppressedCandidate candidate) => new()
    {
        ["content_id"] = candidate.ContentId,
        ["reason"] = candidate.Reason,
    };

    private static JsonObject ToJson(FinalActiveJourney journey) => new()
    {
        ["journey_id"] = journey.JourneyId,
        ["service_category"] = journey.ServiceCategory,
    };

    private static JsonObject ToJson(FinalNextBestAction action) => new()
    {
        ["content_id"] = action.ContentId,
        ["cta_type"] = action.CtaType,
        ["label"] = action.Label,
        ["deep_link"] = action.DeepLink,
        ["ranking_score"] = action.RankingScore,
        ["ranking_policy_version"] = action.RankingPolicyVersion,
    };

    private static JsonObject ToJson(SupportingContent content) => new()
    {
        ["content_id"] = content.ContentId,
        ["cta_type"] = content.CtaType,
        ["label"] = content.Label,
        ["deep_link"] = content.DeepLink,
    };

    private static JsonObject ToJson(SecondaryJourneyPrompt prompt) => new()
    {
        ["journey_id"] = prompt.JourneyId,
        ["service_category"] = prompt.ServiceCategory,
        ["label"] = prompt.Label,
        ["deep_link"] = prompt.DeepLink,
        ["content_id"] = prompt.ContentId,
    };

    private static JsonObject ToJson(FinalExplanation explanation) => new()
    {
        ["source"] = explanation.Source,
        ["ai_response_id"] = explanation.AiResponseId,
        ["summary"] = explanation.Summary,
        ["cta_support_text"] = explanation.CtaSupportText,
        ["grounding_asset_ids"] = new JsonArray(explanation.GroundingAssetIds.Select(id => (JsonNode)id).ToArray()),
    };

    private static JsonObject ToJson(DecisionTrace trace) => new()
    {
        ["profile_read"] = trace.ProfileRead,
        ["journey_read"] = trace.JourneyRead,
        ["active_journey_selected"] = trace.ActiveJourneySelected,
        ["retrieval"] = trace.Retrieval,
        ["filtering"] = trace.Filtering,
        ["ranking"] = trace.Ranking,
        ["ai_explanation"] = trace.AiExplanation,
    };

    private static JsonObject ToJson(QualificationState state) => new()
    {
        ["coverage_region_match"] = state.CoverageRegionMatch,
        ["serviceability_confirmed"] = state.ServiceabilityConfirmed,
        ["hard_exclusions"] = new JsonArray(state.HardExclusions.Select(value => (JsonNode)value).ToArray()),
        ["suppression_flags"] = new JsonArray(state.SuppressionFlags.Select(value => (JsonNode)value).ToArray()),
    };

    private static QualificationState ToQualificationState(JsonObject payload) => new(
        payload.OptionalBoolProperty("coverage_region_match"),
        payload.OptionalBoolProperty("serviceability_confirmed"),
        payload.RequireArrayProperty("hard_exclusions").Select(static value => value?.GetValue<string>() ?? string.Empty).ToArray(),
        payload.RequireArrayProperty("suppression_flags").Select(static value => value?.GetValue<string>() ?? string.Empty).ToArray());

    private static bool? OptionalBoolean(JsonObject payload, string propertyName) =>
        payload[propertyName]?.GetValue<bool>();

    private static void AddOptional(JsonObject payload, string propertyName, bool? value)
    {
        if (value is not null)
        {
            payload[propertyName] = value.Value;
        }
    }

    private static void AddOptional(JsonObject payload, string propertyName, string? value)
    {
        if (value is not null)
        {
            payload[propertyName] = value;
        }
    }

    private static string Timestamp(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
}
