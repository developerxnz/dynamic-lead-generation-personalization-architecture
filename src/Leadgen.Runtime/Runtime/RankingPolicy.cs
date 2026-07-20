namespace Leadgen.Runtime;

/// <summary>
/// Contains the explicit, versioned contributions used by deterministic ranking.
/// </summary>
internal sealed record RankingPolicy(
    int ActiveJourneyFit,
    int IntentFit,
    int ResumeIntentFit,
    int FunnelStageFit,
    int ServiceabilityFit,
    int ConfirmedServiceabilityResumeFit,
    int HouseholdFit,
    int UrgencyFit,
    int CampaignFit,
    int ResumeFit,
    int ServiceabilityFailureCheckPenalty,
    int ExpiredResumeRecoveryFit,
    int PriorityMaximum)
{
    private static readonly RankingPolicy Default = new(
        ActiveJourneyFit: 8,
        IntentFit: 6,
        ResumeIntentFit: 7,
        FunnelStageFit: 5,
        ServiceabilityFit: 7,
        ConfirmedServiceabilityResumeFit: 4,
        HouseholdFit: 4,
        UrgencyFit: 3,
        CampaignFit: 3,
        ResumeFit: 7,
        ServiceabilityFailureCheckPenalty: 20,
        ExpiredResumeRecoveryFit: 9,
        PriorityMaximum: 5);

    private static readonly IReadOnlyDictionary<string, RankingPolicy> Policies =
        new Dictionary<string, RankingPolicy>(StringComparer.Ordinal)
        {
            ["broadband-v1"] = Default,
            ["health-v1"] = Default,
        };

    public static RankingPolicy Resolve(string version) =>
        Policies.TryGetValue(version, out var policy)
            ? policy
            : throw new ArgumentException($"Unknown ranking policy version: {version}");
}
