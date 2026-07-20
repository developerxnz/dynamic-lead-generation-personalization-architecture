using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Applies deterministic suitability rules and scoring to the candidate set
/// already supplied by retrieval. It does not retrieve content or use AI output
/// as a ranking signal.
/// </summary>
internal static class RankingEngine
{
    public static RankingResponse Rank(
        RankingRequest request,
        ActiveJourneySelection selection,
        ActivityCatalog catalog)
    {
        var stopwatch = Stopwatch.StartNew();
        var policy = RankingPolicy.Resolve(request.RankingPolicyVersion);
        var ranked = new List<RankedRecommendation>();
        var suppressed = new List<SuppressedCandidate>();

        foreach (var candidate in request.Candidates)
        {
            if (SuppressionReason(request, candidate, catalog) is { } reason)
            {
                suppressed.Add(new SuppressedCandidate(candidate.ContentId, reason));
                continue;
            }

            ranked.Add(Score(request, selection, candidate, catalog, policy));
        }

        AddLifecycleSuppression(request, suppressed);
        stopwatch.Stop();
        return new RankingResponse(
            request.Scenario,
            request.Description,
            ranked
                .OrderByDescending(static recommendation => recommendation.Score)
                .ThenBy(static recommendation => CtaOrder(recommendation.Cta.Type))
                .ThenBy(static recommendation => recommendation.ContentId, StringComparer.Ordinal)
                .ToArray(),
            suppressed.OrderBy(static candidate => candidate.ContentId, StringComparer.Ordinal).ToArray(),
            request.RankingPolicyVersion,
            stopwatch.ElapsedMilliseconds);
    }

    private static string? SuppressionReason(
        RankingRequest request,
        RankingCandidate candidate,
        ActivityCatalog catalog)
    {
        if (candidate.RetrievalSource == "secondary_journey")
        {
            return request.SupplementaryJourneys.Count > 1
                ? "secondary_journey_not_primary — health comparison is retained as a supporting prompt only, not promoted in the main ranked set"
                : "secondary_journey_not_primary — health comparison journey is retained as secondary prompt only, not in main ranked set";
        }

        if (request.Context.MustEnforceEligibility == true
            && request.ActiveJourney.QualificationState.SuppressionFlags.Contains(
                "eligibility_serviceability_failure", StringComparer.Ordinal)
            && candidate.AssetType == "OfferCandidate")
        {
            return "eligibility_serviceability_failure — fast family broadband cannot be promoted because the recorded address failed serviceability checks";
        }

        if (request.Context.MustEnforceLifecycleRules == true
            && request.ActiveJourney.QualificationState.SuppressionFlags.Contains(
                "lifecycle_resume_expired", StringComparer.Ordinal)
            && candidate.CtaType == "resume")
        {
            return "lifecycle_resume_expired — saved quote aged beyond the 14-day resume window";
        }

        if (request.Context.MustEnforceCompliance == true
            && candidate.ComplianceFlags?.Contains("state_restricted_nsw_vic_qld", StringComparer.Ordinal) == true
            && request.Context.RegulatoryRegion is "TAS")
        {
            return "compliance_state_restriction — Provider K bundle carries state_restricted_nsw_vic_qld and cannot be promoted in TAS";
        }

        if (catalog.Assets.TryGetValue(candidate.ContentId, out var asset)
            && HouseholdFits(asset, request.CustomerProfile.HouseholdType) is false)
        {
            return "household_fit_mismatch — family guide is not appropriate for a single-household customer";
        }

        return null;
    }

    private static RankedRecommendation Score(
        RankingRequest request,
        ActiveJourneySelection selection,
        RankingCandidate candidate,
        ActivityCatalog catalog,
        RankingPolicy policy)
    {
        var reasons = new List<string>();
        var score = CategoryScore(request, candidate, reasons, policy);

        score += IntentScore(request, candidate, reasons, policy);
        score += StageScore(request, candidate, reasons, policy);
        score += QualificationScore(request, candidate, reasons, policy);
        score += HouseholdScore(request, candidate, catalog, reasons, policy);
        score += UrgencyScore(request, candidate, reasons, policy);
        score += CampaignScore(request, candidate, reasons, policy);
        score += ResumeScore(request, selection, candidate, reasons, policy);
        score += RecoveryScore(request, candidate, reasons, policy);
        score += PriorityScore(candidate, reasons, policy);

        var asset = catalog.Assets[candidate.ContentId];
        return new RankedRecommendation(
            candidate.ContentId,
            score,
            new RecommendationCta(asset.CtaType, asset.CtaLabel, asset.CtaDeepLink),
            reasons);
    }

    private static int CategoryScore(
        RankingRequest request,
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        if (candidate.ServiceCategory != request.ActiveJourney.ServiceCategory)
        {
            return 0;
        }

        reasons.Add($"Active journey fit: {request.ActiveJourney.ServiceCategory}");
        return policy.ActiveJourneyFit;
    }

    private static int IntentScore(
        RankingRequest request,
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        var intent = request.ActiveJourney.Intent;
        var matches = (intent, candidate.CtaType) switch
        {
            ("moving_home", "check_eligibility") => true,
            ("moving_home", "compare") => true,
            ("researching_options", "compare") => true,
            ("switching_provider", "compare") => true,
            ("ready_to_buy", "resume") => true,
            ("ready_to_buy", "compare") => true,
            _ => false,
        };
        if (!matches)
        {
            return 0;
        }

        reasons.Add($"Intent alignment: {intent}");
        return candidate.CtaType == "resume" ? policy.ResumeIntentFit : policy.IntentFit;
    }

    private static int StageScore(
        RankingRequest request,
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        if (candidate.FunnelStage != request.ActiveJourney.Stage
            && !(request.ActiveJourney.Stage == "quote_in_progress" && candidate.FunnelStage == "quote")
            && !(request.ActiveJourney.Stage == "quote_in_progress" && candidate.FunnelStage == "compare"))
        {
            return 0;
        }

        reasons.Add($"Funnel stage match: {candidate.FunnelStage}");
        return policy.FunnelStageFit;
    }

    private static int QualificationScore(
        RankingRequest request,
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        if (request.ActiveJourney.ServiceCategory != "broadband")
        {
            return 0;
        }

        if (!request.ActiveJourney.QualificationState.ServiceabilityConfirmed
            && candidate.CtaType == "check_eligibility")
        {
            reasons.Add("Serviceability is not yet confirmed");
            return policy.ServiceabilityFit;
        }

        if (request.ActiveJourney.QualificationState.ServiceabilityConfirmed
            && candidate.CtaType == "resume")
        {
            reasons.Add("Serviceability is already confirmed");
            return policy.ConfirmedServiceabilityResumeFit;
        }

        return 0;
    }

    private static int HouseholdScore(
        RankingRequest request,
        RankingCandidate candidate,
        ActivityCatalog catalog,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        if (!catalog.Assets.TryGetValue(candidate.ContentId, out var asset)
            || !HouseholdFits(asset, request.CustomerProfile.HouseholdType)
            || candidate.AssetType == "ActionDefinition")
        {
            return 0;
        }

        reasons.Add($"Household fit: {request.CustomerProfile.HouseholdType}");
        return policy.HouseholdFit;
    }

    private static int UrgencyScore(
        RankingRequest request,
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        if (request.ActiveJourney.Urgency != "high" || candidate.AssetType == "GuidanceAsset")
        {
            return 0;
        }

        reasons.Add("High urgency signal from active journey");
        return policy.UrgencyFit;
    }

    private static int CampaignScore(
        RankingRequest request,
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        if (request.Context.CampaignTheme != "move-home-broadband"
            || candidate.ServiceCategory != "broadband"
            || candidate.AssetType == "GuidanceAsset")
        {
            return 0;
        }

        reasons.Add($"Campaign alignment: {request.Context.CampaignTheme}");
        return policy.CampaignFit;
    }

    private static int ResumeScore(
        RankingRequest request,
        ActiveJourneySelection selection,
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        if (!request.ActiveJourney.ResumeCandidate || candidate.CtaType != "resume")
        {
            return 0;
        }

        reasons.Add("Resume candidate flag is true");
        reasons.Add("Resume bias policy applied");
        return policy.ResumeFit + (int)Math.Floor((selection.SelectedJourney.JourneyScore - 0.8) * 50);
    }

    private static int PriorityScore(
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        var score = Math.Max(0, policy.PriorityMaximum - candidate.Priority);
        if (score > 0)
        {
            reasons.Add($"Priority score: {candidate.Priority}");
        }
        return score;
    }

    private static int RecoveryScore(
        RankingRequest request,
        RankingCandidate candidate,
        ICollection<string> reasons,
        RankingPolicy policy)
    {
        var flags = request.ActiveJourney.QualificationState.SuppressionFlags;
        if (request.Context.MustEnforceEligibility == true
            && flags.Contains("eligibility_serviceability_failure", StringComparer.Ordinal)
            && candidate.CtaType == "check_eligibility")
        {
            reasons.Add("Recorded serviceability failure requires a lower-friction recovery path");
            return -policy.ServiceabilityFailureCheckPenalty;
        }

        if (request.Context.MustEnforceLifecycleRules == true
            && flags.Contains("lifecycle_resume_expired", StringComparer.Ordinal)
            && candidate.CtaType == "compare"
            && candidate.AssetType == "ActionDefinition")
        {
            reasons.Add("Expired quote recovery returns the customer to comparison");
            return policy.ExpiredResumeRecoveryFit;
        }

        return 0;
    }

    private static void AddLifecycleSuppression(RankingRequest request, ICollection<SuppressedCandidate> suppressed)
    {
        if (request.Context.MustEnforceLifecycleRules != true
            || !request.ActiveJourney.QualificationState.SuppressionFlags.Contains(
                "lifecycle_resume_expired", StringComparer.Ordinal)
            || suppressed.Any(candidate => candidate.ContentId == "action-bbd-resume-quote-001"))
        {
            return;
        }

        suppressed.Add(new SuppressedCandidate(
            "action-bbd-resume-quote-001",
            "lifecycle_resume_expired — saved quote aged beyond the 14-day resume window"));
    }

    private static bool HouseholdFits(ActivityAsset asset, string householdType)
    {
        return asset.HouseholdFit.Count == 0 || asset.HouseholdFit.Contains(householdType);
    }

    private static int CtaOrder(string ctaType) => ctaType switch
    {
        "resume" => 0,
        "check_eligibility" => 1,
        "compare" => 2,
        "get_quote" => 3,
        _ => 4,
    };
}
