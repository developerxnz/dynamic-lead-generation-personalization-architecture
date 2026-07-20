using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Builds the RAG-backed prompt payload by selecting grounded snippets for the chosen action and journey.
/// </summary>
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
        ScenarioInputs inputs,
        IReadOnlyList<JourneyState> journeys,
        SessionContext session,
        ActiveJourneySelection selection,
        JsonObject rankingResponse,
        ActivityCatalog catalog,
        JsonObject promptFixture)
    {
        var promptInput = promptFixture.DeepCloneObject();
        var customerAttributes = inputs.RequireAttributes();
        var activeJourney = ActiveJourney(journeys, selection);
        var selectedAction = SelectedAction(rankingResponse);
        var (groundingContext, retrievalDebug) = BuildGroundingContext(
            inputs,
            session,
            activeJourney,
            selectedAction,
            rankingResponse,
            catalog);

        promptInput["journey_context"] = new JsonObject
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
            ["behavior_summary"] = activeJourney.BehaviorSummary.DeepCloneObject(),
            ["journey_score"] = activeJourney.JourneyScore,
            ["last_meaningful_event_at"] = activeJourney.LastMeaningfulEventAt.ToString("O"),
        };
        promptInput["customer_context"] = new JsonObject
        {
            ["household_type"] = customerAttributes.RequireProperty("household_type").DeepClone(),
            ["location"] = customerAttributes.RequireProperty("location").DeepClone(),
        };
        promptInput["selected_action"] = selectedAction;
        promptInput["grounding_context"] = groundingContext;
        promptInput["grounding_retrieval"] = retrievalDebug;
        promptInput["scenario"] = scenarioName;
        return promptInput;
    }

    private static JourneyState ActiveJourney(
        IReadOnlyList<JourneyState> journeys,
        ActiveJourneySelection selection)
    {
        var activeJourney = journeys.FirstOrDefault(journey => journey.JourneyId == selection.SelectedJourney.JourneyId);

        return activeJourney ?? throw new KeyNotFoundException($"Selected journey not found: {selection.SelectedJourney.JourneyId}");
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
        ScenarioInputs inputs,
        SessionContext session,
        JourneyState activeJourney,
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
        var serviceCategory = activeJourney.ServiceCategory;

        var scored = new List<ScoredSnippet>();
        foreach (var snippet in catalog.Snippets.Values)
        {
            if (snippet.ServiceCategory != serviceCategory)
            {
                continue;
            }

            var linkedAssets = snippet.LinkedAssetIds
                .Where(static assetId => !string.IsNullOrWhiteSpace(assetId))
                .Select(assetId => catalog.Assets.GetValueOrDefault(assetId!))
                .Where(static asset => asset is not null)
                .Select(static asset => asset!.Raw)
                .ToList();

            if (linkedAssets.Count == 0)
            {
                continue;
            }

            var (score, reasons) = ScoreSnippet(
                snippet.Raw,
                linkedAssets,
                queryTokens,
                selectedActionId,
                rankedAssetIds,
                activeJourney,
                inputs);

            scored.Add(new ScoredSnippet(
                snippet.Raw,
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
                ["metadata_revision"] = catalog.Assets[entry.AssetId].MetadataRevision,
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
        JourneyState activeJourney,
        ScenarioInputs inputs)
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

        if (linkedAssets.Any(asset => StageMatches(asset, activeJourney.Stage)))
        {
            score += 12;
            reasons.Add("Matches active journey stage");
        }

        var householdType = inputs.RequireAttributes().OptionalStringProperty("household_type");
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

    private static string QueryText(SessionContext session, JourneyState activeJourney, JsonObject selectedAction)
    {
        var parts = new[]
        {
            session.QueryText,
            session.CurrentUrl,
            session.CampaignTheme,
            activeJourney.Intent,
            activeJourney.Stage,
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
