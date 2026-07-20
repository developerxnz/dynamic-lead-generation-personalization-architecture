namespace Leadgen.Runtime;

internal sealed record CandidateRetrieval(
    string Scenario,
    string Description,
    RetrievalQuery Query,
    IReadOnlyList<RetrievedCandidate> Candidates,
    long DurationMilliseconds,
    IReadOnlyList<ExcludedCandidate> ExcludedCandidates);

internal sealed record RetrievalQuery(
    RetrievalJourney ActiveJourney,
    RetrievalJourney? SecondaryJourney,
    RetrievalContext Context);

internal sealed record RetrievalJourney(
    string ServiceCategory,
    string Stage,
    string Intent,
    bool ResumeCandidate,
    int? MaxSecondaryCandidates);

internal sealed record RetrievalContext(
    string Region,
    string Channel,
    string LifecycleStatusFilter,
    string? ComplianceFilter);

internal sealed record RetrievedCandidate(
    string AssetId,
    string AssetType,
    string ServiceCategory,
    string FunnelStageMatch,
    string RetrievalSource,
    string? Note);

internal sealed record ExcludedCandidate(string AssetId, string Reason);
