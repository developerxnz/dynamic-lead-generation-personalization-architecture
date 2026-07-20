using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Converts retrieval contracts at fixture and artifact boundaries.
/// </summary>
internal static class RetrievalContractAdapter
{
    public static CandidateRetrieval FromJson(JsonObject payload) => new(
        payload.RequireStringProperty("scenario"),
        payload.RequireStringProperty("description"),
        RetrievalQuery(payload.RequireObjectProperty("retrieval_query")),
        payload.RequireArrayProperty("candidates_returned").OfType<JsonObject>().Select(RetrievedCandidate).ToArray(),
        payload.RequireProperty("retrieval_duration_ms").GetValue<long>(),
        payload.RequireArrayProperty("excluded_at_retrieval").OfType<JsonObject>().Select(ExcludedCandidate).ToArray());

    public static JsonObject ToJson(this CandidateRetrieval retrieval) => new()
    {
        ["scenario"] = retrieval.Scenario,
        ["description"] = retrieval.Description,
        ["retrieval_query"] = ToJson(retrieval.Query),
        ["candidates_returned"] = new JsonArray(retrieval.Candidates.Select(candidate => (JsonNode)ToJson(candidate)).ToArray()),
        ["total_candidates"] = retrieval.Candidates.Count,
        ["retrieval_duration_ms"] = retrieval.DurationMilliseconds,
        ["excluded_at_retrieval"] = new JsonArray(retrieval.ExcludedCandidates.Select(candidate => (JsonNode)ToJson(candidate)).ToArray()),
    };

    private static RetrievalQuery RetrievalQuery(JsonObject payload) => new(
        RetrievalJourney(payload.RequireObjectProperty("active_journey")),
        payload["secondary_journey"] is JsonObject secondary ? RetrievalJourney(secondary) : null,
        RetrievalContext(payload.RequireObjectProperty("context")));

    private static RetrievalJourney RetrievalJourney(JsonObject payload) => new(
        payload.RequireStringProperty("service_category"),
        payload.RequireStringProperty("stage"),
        payload.RequireStringProperty("intent"),
        payload.OptionalBoolProperty("resume_candidate"),
        payload["max_secondary_candidates"]?.GetValue<int>());

    private static RetrievalContext RetrievalContext(JsonObject payload) => new(
        payload.RequireStringProperty("region"),
        payload.RequireStringProperty("channel"),
        payload.RequireStringProperty("lifecycle_status_filter"),
        payload.OptionalStringProperty("compliance_filter"));

    private static RetrievedCandidate RetrievedCandidate(JsonObject payload) => new(
        payload.RequireStringProperty("asset_id"),
        payload.RequireStringProperty("asset_type"),
        payload.RequireStringProperty("service_category"),
        payload.RequireStringProperty("funnel_stage_match"),
        payload.RequireStringProperty("retrieval_source"),
        payload.OptionalStringProperty("note"));

    private static ExcludedCandidate ExcludedCandidate(JsonObject payload) => new(
        payload.RequireStringProperty("asset_id"),
        payload.RequireStringProperty("reason"));

    private static JsonObject ToJson(RetrievalQuery query) => new()
    {
        ["active_journey"] = ToJson(query.ActiveJourney),
        ["secondary_journey"] = query.SecondaryJourney is { } secondary ? ToJson(secondary) : null,
        ["context"] = ToJson(query.Context),
    };

    private static JsonObject ToJson(RetrievalJourney journey)
    {
        var json = new JsonObject
        {
            ["service_category"] = journey.ServiceCategory,
            ["stage"] = journey.Stage,
            ["intent"] = journey.Intent,
        };
        if (journey.ResumeCandidate)
        {
            json["resume_candidate"] = true;
        }
        if (journey.MaxSecondaryCandidates is { } maxSecondaryCandidates)
        {
            json["max_secondary_candidates"] = maxSecondaryCandidates;
        }
        return json;
    }

    private static JsonObject ToJson(RetrievalContext context)
    {
        var json = new JsonObject
        {
            ["region"] = context.Region,
            ["channel"] = context.Channel,
            ["lifecycle_status_filter"] = context.LifecycleStatusFilter,
        };
        if (context.ComplianceFilter is not null)
        {
            json["compliance_filter"] = context.ComplianceFilter;
        }
        return json;
    }

    private static JsonObject ToJson(RetrievedCandidate candidate)
    {
        var json = new JsonObject
        {
            ["asset_id"] = candidate.AssetId,
            ["asset_type"] = candidate.AssetType,
            ["service_category"] = candidate.ServiceCategory,
            ["funnel_stage_match"] = candidate.FunnelStageMatch,
            ["retrieval_source"] = candidate.RetrievalSource,
        };
        if (candidate.Note is not null)
        {
            json["note"] = candidate.Note;
        }
        return json;
    }

    private static JsonObject ToJson(ExcludedCandidate candidate) => new()
    {
        ["asset_id"] = candidate.AssetId,
        ["reason"] = candidate.Reason,
    };
}
