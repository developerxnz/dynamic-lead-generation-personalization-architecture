using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Leadgen.Runtime;

/// <summary>
/// Selects grounded snippets for an already-ranked recommendation.
/// </summary>
internal sealed class GroundingRetriever
{
    private static readonly Regex TokenRegex = new("[a-z0-9]+", RegexOptions.Compiled);
    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "a", "an", "and", "are", "at", "be", "by", "for", "from", "how", "in", "is", "it",
        "of", "on", "or", "the", "this", "to", "up", "we", "with", "your",
    };
    private static readonly IReadOnlyDictionary<string, HashSet<string>> StageEquivalents =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["discover"] = new(StringComparer.Ordinal) { "discover", "research" },
            ["research"] = new(StringComparer.Ordinal) { "discover", "research", "compare" },
            ["compare"] = new(StringComparer.Ordinal) { "research", "compare", "quote" },
            ["quote"] = new(StringComparer.Ordinal) { "compare", "quote" },
            ["quote_in_progress"] = new(StringComparer.Ordinal) { "quote" },
        };

    public GroundingRetrieval Retrieve(
        ScenarioInputs inputs,
        SessionContext session,
        JourneyState activeJourney,
        RankedRecommendation selectedAction,
        RankingResponse rankingResponse,
        ActivityCatalog catalog,
        int maxChunks = 2)
    {
        var rankedAssetIds = rankingResponse.RankedRecommendations
            .Select(static recommendation => recommendation.ContentId)
            .ToArray();
        var queryText = QueryText(session, activeJourney, selectedAction);
        var queryTokens = Tokenize(queryText);
        var scored = catalog.Snippets.Values
            .Where(snippet => snippet.ServiceCategory == activeJourney.ServiceCategory)
            .Select(snippet => ScoreSnippet(
                snippet,
                LinkedAssets(snippet, catalog),
                queryTokens,
                selectedAction.ContentId,
                rankedAssetIds,
                activeJourney,
                inputs.Attributes.HouseholdType))
            .Where(static entry => entry is not null)
            .Cast<ScoredSnippet>()
            .ToArray();

        var chosen = new List<ScoredSnippet>();
        var selected = scored
            .Where(entry => entry.AssetId == selectedAction.ContentId
                || entry.Snippet.LinkedAssetIds.Contains(selectedAction.ContentId, StringComparer.Ordinal))
            .OrderBy(entry => SelectedActionSnippetRank(entry, selectedAction.ContentId))
            .FirstOrDefault();
        if (selected is not null)
        {
            chosen.Add(selected);
        }

        foreach (var entry in scored
                     .OrderByDescending(static item => item.Score)
                     .ThenBy(static item => item.Snippet.SnippetId, StringComparer.Ordinal))
        {
            if (chosen.Any(chosenEntry => chosenEntry.Snippet.SnippetId == entry.Snippet.SnippetId))
            {
                continue;
            }

            chosen.Add(entry);
            if (chosen.Count == maxChunks)
            {
                break;
            }
        }

        return new GroundingRetrieval(
            queryText,
            queryTokens.OrderBy(static token => token, StringComparer.Ordinal).ToArray(),
            chosen.Take(maxChunks).Select(entry => new GroundingResult(
                entry.Snippet.SnippetId,
                entry.AssetId,
                entry.Snippet.Content,
                entry.Score,
                catalog.Assets[entry.AssetId].MetadataRevision,
                entry.Reasons)).ToArray());
    }

    private static IReadOnlyList<ActivityAsset> LinkedAssets(GroundingSnippet snippet, ActivityCatalog catalog) =>
        snippet.LinkedAssetIds
            .Select(assetId => catalog.Assets.GetValueOrDefault(assetId))
            .Where(static asset => asset is not null)
            .Cast<ActivityAsset>()
            .ToArray();

    private static ScoredSnippet? ScoreSnippet(
        GroundingSnippet snippet,
        IReadOnlyList<ActivityAsset> linkedAssets,
        HashSet<string> queryTokens,
        string selectedActionId,
        IReadOnlyList<string> rankedAssetIds,
        JourneyState activeJourney,
        string householdType)
    {
        if (linkedAssets.Count == 0)
        {
            return null;
        }

        var score = 0;
        var reasons = new List<string>();
        var linkedAssetIds = linkedAssets.Select(static asset => asset.AssetId).ToArray();
        if (linkedAssetIds.Contains(selectedActionId, StringComparer.Ordinal))
        {
            score += 100;
            reasons.Add("Linked to selected action");
        }

        for (var index = 0; index < Math.Min(3, rankedAssetIds.Count); index++)
        {
            if (linkedAssetIds.Contains(rankedAssetIds[index], StringComparer.Ordinal))
            {
                score += 36 - (index * 8);
                reasons.Add($"Linked to ranked candidate #{index + 1}");
            }
        }

        if (linkedAssets.Any(asset => StageMatches(asset, activeJourney.Stage)))
        {
            score += 12;
            reasons.Add("Matches active journey stage");
        }
        if (linkedAssets.Any(asset => asset.HouseholdFit.Contains(householdType)))
        {
            score += 8;
            reasons.Add("Fits household type");
        }

        var overlap = queryTokens.Intersect(SnippetTokens(snippet, linkedAssets), StringComparer.Ordinal)
            .OrderBy(static token => token, StringComparer.Ordinal)
            .ToArray();
        if (overlap.Length > 0)
        {
            score += Math.Min(overlap.Length * 5, 40);
            reasons.Add($"Keyword overlap: {string.Join(", ", overlap)}");
        }

        return new ScoredSnippet(snippet, score, reasons, BestAssetId(linkedAssets, selectedActionId, rankedAssetIds));
    }

    private static string BestAssetId(
        IReadOnlyList<ActivityAsset> linkedAssets,
        string selectedActionId,
        IReadOnlyList<string> rankedAssetIds)
    {
        if (linkedAssets[0].AssetId == selectedActionId || linkedAssets.Count > 1)
        {
            return linkedAssets[0].AssetId;
        }

        return rankedAssetIds.FirstOrDefault(assetId => linkedAssets.Any(asset => asset.AssetId == assetId))
            ?? linkedAssets[0].AssetId;
    }

    private static (int ExactPrimaryLink, int ExclusiveLink, int NegativeScore, string SnippetId) SelectedActionSnippetRank(
        ScoredSnippet entry,
        string selectedActionId) =>
        (
            entry.Snippet.LinkedAssetIds.Count > 0 && entry.Snippet.LinkedAssetIds[0] == selectedActionId ? 0 : 1,
            entry.Snippet.LinkedAssetIds.SequenceEqual(new[] { selectedActionId }, StringComparer.Ordinal) ? 0 : 1,
            -entry.Score,
            entry.Snippet.SnippetId);

    private static string QueryText(SessionContext session, JourneyState journey, RankedRecommendation action) =>
        string.Join(" ", new[]
        {
            session.QueryText, session.CurrentUrl, session.CampaignTheme, journey.Intent, journey.Stage,
            action.Cta.Label, action.ContentId,
        }.Where(static part => !string.IsNullOrWhiteSpace(part)));

    private static HashSet<string> SnippetTokens(GroundingSnippet snippet, IReadOnlyList<ActivityAsset> assets) =>
        Tokenize(new[] { snippet.Content, string.Join(" ", snippet.Tags) }
            .Concat(assets.SelectMany(asset => new[]
            {
                asset.RetrievalSummary, asset.AiSupport.PlainLanguageSummary, asset.AiSupport.ApprovedExplainerText,
                string.Join(" ", asset.AiSupport.RetrievalTags), asset.ConversionGoal,
            }))
            .ToArray());

    private static HashSet<string> Tokenize(params string?[] parts)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in parts.Where(static part => !string.IsNullOrWhiteSpace(part)))
        {
            foreach (Match match in TokenRegex.Matches(part!.ToLowerInvariant().Replace("_", " ", StringComparison.Ordinal)))
            {
                if (match.Value.Length > 1 && !Stopwords.Contains(match.Value))
                {
                    tokens.Add(match.Value);
                }
            }
        }
        return tokens;
    }

    private static bool StageMatches(ActivityAsset asset, string stage) =>
        asset.FunnelStages.Any(StageEquivalents.GetValueOrDefault(
            stage,
            new HashSet<string>(StringComparer.Ordinal) { stage }).Contains);

    private sealed record ScoredSnippet(
        GroundingSnippet Snippet,
        int Score,
        IReadOnlyList<string> Reasons,
        string AssetId);
}

internal sealed record GroundingRetrieval(
    string QueryText,
    IReadOnlyList<string> QueryTokens,
    IReadOnlyList<GroundingResult> Results)
{
    public JsonArray ToContextJson() => new(Results.Select(result => (JsonNode)new JsonObject
    {
        ["snippet_id"] = result.SnippetId,
        ["asset_id"] = result.AssetId,
        ["content"] = result.Content,
    }).ToArray());

    public JsonObject ToDebugJson() => new()
    {
        ["query_text"] = QueryText,
        ["query_tokens"] = new JsonArray(QueryTokens.Select(token => (JsonNode)token).ToArray()),
        ["results"] = new JsonArray(Results.Select(result => (JsonNode)new JsonObject
        {
            ["snippet_id"] = result.SnippetId,
            ["asset_id"] = result.AssetId,
            ["score"] = result.Score,
            ["metadata_revision"] = result.MetadataRevision,
            ["reasons"] = new JsonArray(result.Reasons.Select(reason => (JsonNode)reason).ToArray()),
        }).ToArray()),
    };
}

internal sealed record GroundingResult(
    string SnippetId,
    string AssetId,
    string Content,
    int Score,
    string MetadataRevision,
    IReadOnlyList<string> Reasons);
