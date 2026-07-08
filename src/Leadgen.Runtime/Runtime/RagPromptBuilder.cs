using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

internal sealed class RagPromptBuilder
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

    public JsonObject Build(
        string scenarioName,
        JsonObject profile,
        JsonObject journeysPayload,
        JsonObject session,
        JsonObject activeSelection,
        JsonObject rankingResponse,
        ActivityCatalog catalog,
        JsonObject promptFixture)
    {
        var promptInput = promptFixture.DeepCloneObject();
        var activeJourney = ActiveJourney(journeysPayload, activeSelection);
        var selectedAction = SelectedAction(rankingResponse);
        var (groundingContext, retrievalDebug) = BuildGroundingContext(
            profile,
            session,
            activeJourney,
            selectedAction,
            rankingResponse,
            catalog);

        promptInput["journey_context"] = new JsonObject
        {
            ["journey_id"] = activeJourney.RequireStringProperty("journey_id"),
            ["service_category"] = activeJourney.RequireStringProperty("service_category"),
            ["intent"] = activeJourney.RequireStringProperty("intent"),
            ["stage"] = activeJourney.RequireStringProperty("stage"),
            ["resume_candidate"] = activeJourney.OptionalBoolProperty("resume_candidate"),
            ["qualification_state"] = new JsonObject
            {
                ["coverage_region_match"] = activeJourney
                    .RequireObjectProperty("qualification_state")
                    .RequireProperty("coverage_region_match")
                    .DeepClone(),
                ["serviceability_confirmed"] = activeJourney
                    .RequireObjectProperty("qualification_state")
                    .RequireProperty("serviceability_confirmed")
                    .DeepClone(),
            },
            ["behavior_summary"] = activeJourney.RequireObjectProperty("behavior_summary").DeepCloneObject(),
            ["journey_score"] = activeJourney
                .RequireObjectProperty("decision_support")
                .RequireProperty("journey_score")
                .DeepClone(),
            ["last_meaningful_event_at"] = activeJourney.RequireProperty("last_meaningful_event_at").DeepClone(),
        };
        promptInput["customer_context"] = new JsonObject
        {
            ["household_type"] = profile.RequireObjectProperty("profile").RequireProperty("household_type").DeepClone(),
            ["location"] = profile.RequireObjectProperty("profile").RequireProperty("location").DeepClone(),
            ["is_returning_customer"] = profile.RequireObjectProperty("customer_summary").RequireProperty("is_returning_customer").DeepClone(),
        };
        promptInput["selected_action"] = selectedAction;
        promptInput["grounding_context"] = groundingContext;
        promptInput["grounding_retrieval"] = retrievalDebug;
        promptInput["scenario"] = scenarioName;
        return promptInput;
    }

    private static JsonObject ActiveJourney(JsonObject journeysPayload, JsonObject activeSelection)
    {
        var selectedJourneyId = activeSelection.RequireStringProperty("selected_journey_id");
        var activeJourney = journeysPayload.RequireArrayProperty("journeys")
            .OfType<JsonObject>()
            .FirstOrDefault(journey => journey.RequireStringProperty("journey_id") == selectedJourneyId);

        return activeJourney ?? throw new KeyNotFoundException($"Selected journey not found: {selectedJourneyId}");
    }

    private static JsonObject SelectedAction(JsonObject rankingResponse)
    {
        var topRecommendation = rankingResponse.RequireArrayProperty("ranked_recommendations")
            .OfType<JsonObject>()
            .First();

        return new JsonObject
        {
            ["content_id"] = topRecommendation.RequireStringProperty("content_id"),
            ["cta_type"] = topRecommendation.RequireObjectProperty("cta").RequireStringProperty("type"),
            ["cta_label"] = topRecommendation.RequireObjectProperty("cta").RequireStringProperty("label"),
            ["cta_deep_link"] = topRecommendation.RequireObjectProperty("cta").RequireStringProperty("deep_link"),
        };
    }

    private static (JsonArray GroundingContext, JsonObject RetrievalDebug) BuildGroundingContext(
        JsonObject profile,
        JsonObject session,
        JsonObject activeJourney,
        JsonObject selectedAction,
        JsonObject rankingResponse,
        ActivityCatalog catalog,
        int maxChunks = 2)
    {
        var rankedAssetIds = rankingResponse.RequireArrayProperty("ranked_recommendations")
            .OfType<JsonObject>()
            .Select(static recommendation => recommendation.RequireStringProperty("content_id"))
            .ToArray();
        var selectedActionId = selectedAction.RequireStringProperty("content_id");
        var queryText = QueryText(session, activeJourney, selectedAction);
        var queryTokens = Tokenize(queryText);
        var region = session.OptionalStringProperty("region");
        var serviceCategory = activeJourney.RequireStringProperty("service_category");

        var scored = new List<ScoredSnippet>();
        foreach (var snippet in catalog.Snippets.Values)
        {
            if (snippet.RequireStringProperty("serviceCategory") != serviceCategory)
            {
                continue;
            }

            var linkedAssets = snippet.RequireArrayProperty("linkedAssets")
                .Select(static node => node?.GetValue<string>())
                .Where(static assetId => !string.IsNullOrWhiteSpace(assetId))
                .Select(assetId => catalog.Assets.GetValueOrDefault(assetId!))
                .Where(static asset => asset is not null)
                .Cast<JsonObject>()
                .ToList();

            if (linkedAssets.Count == 0 || !linkedAssets.Any(asset => RegionMatches(asset, region)))
            {
                continue;
            }

            var (score, reasons) = ScoreSnippet(
                snippet,
                linkedAssets,
                queryTokens,
                selectedActionId,
                rankedAssetIds,
                activeJourney,
                profile,
                region);

            scored.Add(new ScoredSnippet(
                snippet,
                linkedAssets,
                score,
                reasons,
                BestAssetId(linkedAssets, selectedActionId, rankedAssetIds)));
        }

        var selectedEntries = scored
            .Where(entry => entry.AssetId == selectedActionId
                || entry.Snippet.RequireArrayProperty("linkedAssets").Any(node => node?.GetValue<string>() == selectedActionId))
            .OrderBy(entry => SelectedActionSnippetRank(entry, selectedActionId))
            .ToList();

        var chosen = new List<ScoredSnippet>();
        var seenSnippetIds = new HashSet<string>(StringComparer.Ordinal);

        if (selectedEntries.Count > 0)
        {
            var first = selectedEntries[0];
            chosen.Add(first);
            seenSnippetIds.Add(first.Snippet.RequireStringProperty("snippetId"));
        }

        foreach (var entry in scored.OrderByDescending(static item => item.Score).ThenBy(item => item.Snippet.RequireStringProperty("snippetId"), StringComparer.Ordinal))
        {
            var snippetId = entry.Snippet.RequireStringProperty("snippetId");
            if (seenSnippetIds.Contains(snippetId))
            {
                continue;
            }

            chosen.Add(entry);
            seenSnippetIds.Add(snippetId);
            if (chosen.Count >= maxChunks)
            {
                break;
            }
        }

        var groundingContext = new JsonArray(chosen.Take(maxChunks).Select(entry => (JsonNode)new JsonObject
        {
            ["snippet_id"] = entry.Snippet.RequireStringProperty("snippetId"),
            ["asset_id"] = entry.AssetId,
            ["content"] = entry.Snippet.RequireStringProperty("content"),
        }).ToArray());

        var retrievalDebug = new JsonObject
        {
            ["query_text"] = queryText,
            ["query_tokens"] = new JsonArray(queryTokens.OrderBy(static token => token, StringComparer.Ordinal).Select(static token => (JsonNode)token).ToArray()),
            ["results"] = new JsonArray(chosen.Take(maxChunks).Select(entry => (JsonNode)new JsonObject
            {
                ["snippet_id"] = entry.Snippet.RequireStringProperty("snippetId"),
                ["asset_id"] = entry.AssetId,
                ["score"] = entry.Score,
                ["metadata_revision"] = catalog.Assets[entry.AssetId].RequireStringProperty("metadataRevision"),
                ["reasons"] = new JsonArray(entry.Reasons.Select(static reason => (JsonNode)reason).ToArray()),
            }).ToArray()),
        };

        return (groundingContext, retrievalDebug);
    }

    private static (int Score, List<string> Reasons) ScoreSnippet(
        JsonObject snippet,
        IReadOnlyList<JsonObject> linkedAssets,
        HashSet<string> queryTokens,
        string selectedActionId,
        IReadOnlyList<string> rankedAssetIds,
        JsonObject activeJourney,
        JsonObject profile,
        string? region)
    {
        var score = 0;
        var reasons = new List<string>();
        var linkedAssetIds = linkedAssets.Select(static asset => asset.RequireStringProperty("assetId")).ToArray();

        if (linkedAssetIds.Contains(selectedActionId, StringComparer.Ordinal))
        {
            score += 100;
            reasons.Add("Linked to selected action");
        }

        for (var index = 0; index < Math.Min(3, rankedAssetIds.Count); index++)
        {
            if (!linkedAssetIds.Contains(rankedAssetIds[index], StringComparer.Ordinal))
            {
                continue;
            }

            var weight = 36 - (index * 8);
            score += weight;
            reasons.Add($"Linked to ranked candidate #{index + 1}");
        }

        if (linkedAssets.Any(asset => StageMatches(asset, activeJourney.RequireStringProperty("stage"))))
        {
            score += 12;
            reasons.Add("Matches active journey stage");
        }

        if (linkedAssets.Any(asset => RegionMatches(asset, region)))
        {
            score += 8;
            reasons.Add("Available in session region");
        }

        var householdType = profile.RequireObjectProperty("profile").OptionalStringProperty("household_type");
        if (linkedAssets.Any(asset => HouseholdMatches(asset, householdType)))
        {
            score += 8;
            reasons.Add("Fits household type");
        }

        var overlap = queryTokens.Intersect(SnippetTokens(snippet, linkedAssets), StringComparer.Ordinal).OrderBy(static token => token, StringComparer.Ordinal).ToArray();
        if (overlap.Length > 0)
        {
            var overlapScore = Math.Min(overlap.Length * 5, 40);
            score += overlapScore;
            reasons.Add($"Keyword overlap: {string.Join(", ", overlap)}");
        }

        return (score, reasons);
    }

    private static string BestAssetId(IReadOnlyList<JsonObject> linkedAssets, string selectedActionId, IReadOnlyList<string> rankedAssetIds)
    {
        var firstAssetId = linkedAssets[0].RequireStringProperty("assetId");
        if (firstAssetId == selectedActionId || linkedAssets.Count > 1)
        {
            return firstAssetId;
        }

        foreach (var rankedAssetId in rankedAssetIds)
        {
            var matchedAsset = linkedAssets.FirstOrDefault(asset => asset.RequireStringProperty("assetId") == rankedAssetId);
            if (matchedAsset is not null)
            {
                return matchedAsset.RequireStringProperty("assetId");
            }
        }

        return firstAssetId;
    }

    private static (int ExactPrimaryLink, int ExclusiveLink, int NegativeScore, string SnippetId) SelectedActionSnippetRank(
        ScoredSnippet entry,
        string selectedActionId)
    {
        var linkedAssetIds = entry.Snippet.RequireArrayProperty("linkedAssets")
            .Select(static node => node?.GetValue<string>())
            .Where(static assetId => !string.IsNullOrWhiteSpace(assetId))
            .Cast<string>()
            .ToArray();
        var exactPrimaryLink = linkedAssetIds.Length > 0 && linkedAssetIds[0] == selectedActionId ? 0 : 1;
        var exclusiveLink = linkedAssetIds.SequenceEqual(new[] { selectedActionId }, StringComparer.Ordinal) ? 0 : 1;
        return (exactPrimaryLink, exclusiveLink, -entry.Score, entry.Snippet.RequireStringProperty("snippetId"));
    }

    private static string QueryText(JsonObject session, JsonObject activeJourney, JsonObject selectedAction)
    {
        var parts = new[]
        {
            session.OptionalStringProperty("query_text"),
            session.OptionalStringProperty("current_url"),
            session.OptionalStringProperty("campaign_theme"),
            activeJourney.OptionalStringProperty("intent"),
            activeJourney.OptionalStringProperty("stage"),
            selectedAction.OptionalStringProperty("cta_label"),
            selectedAction.OptionalStringProperty("content_id"),
        };

        return string.Join(" ", parts.Where(static part => !string.IsNullOrWhiteSpace(part)));
    }

    private static HashSet<string> SnippetTokens(JsonObject snippet, IReadOnlyList<JsonObject> linkedAssets)
    {
        var fields = new List<string?>();
        fields.Add(snippet.OptionalStringProperty("content"));
        fields.Add(string.Join(" ", snippet.RequireArrayProperty("tags").Select(static tag => tag?.GetValue<string>() ?? string.Empty)));
        foreach (var asset in linkedAssets)
        {
            var aiFields = asset.RequireObjectProperty("aiSupportFields");
            fields.Add(asset.OptionalStringProperty("retrievalSummary"));
            fields.Add(aiFields.OptionalStringProperty("plainLanguageSummary"));
            fields.Add(aiFields.OptionalStringProperty("approvedExplainerText"));
            fields.Add(string.Join(" ", aiFields.RequireArrayProperty("retrievalTags").Select(static tag => tag?.GetValue<string>() ?? string.Empty)));
            fields.Add(asset.OptionalStringProperty("subtype"));
            fields.Add(asset.OptionalStringProperty("conversionGoal"));
        }

        return Tokenize(fields.ToArray());
    }

    private static HashSet<string> Tokenize(params string?[] parts)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            foreach (Match match in TokenRegex.Matches(part.ToLowerInvariant().Replace("_", " ", StringComparison.Ordinal)))
            {
                var token = match.Value;
                if (token.Length > 1 && !Stopwords.Contains(token))
                {
                    tokens.Add(token);
                }
            }
        }

        return tokens;
    }

    private static bool StageMatches(JsonObject asset, string stage)
    {
        var equivalents = StageEquivalents.GetValueOrDefault(stage, new HashSet<string>(StringComparer.Ordinal) { stage });
        return asset.RequireArrayProperty("funnelStages")
            .Select(static node => node?.GetValue<string>())
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Any(candidate => equivalents.Contains(candidate!));
    }

    private static bool RegionMatches(JsonObject asset, string? region)
    {
        if (region is null)
        {
            return true;
        }

        return asset.RequireArrayProperty("region")
            .Select(static node => node?.GetValue<string>())
            .Any(candidate => candidate == region);
    }

    private static bool HouseholdMatches(JsonObject asset, string? householdType)
    {
        if (string.IsNullOrWhiteSpace(householdType))
        {
            return false;
        }

        return asset.RequireObjectProperty("serviceSpecific")
            .RequireArrayProperty("householdFit")
            .Select(static node => node?.GetValue<string>())
            .Any(candidate => candidate == householdType);
    }

    private sealed record ScoredSnippet(
        JsonObject Snippet,
        IReadOnlyList<JsonObject> LinkedAssets,
        int Score,
        IReadOnlyList<string> Reasons,
        string AssetId);
}
