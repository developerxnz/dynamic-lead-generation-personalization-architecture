using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Loads the shared activity metadata and grounding snippets used during retrieval and ranking assembly.
/// </summary>
internal sealed class ActivityCatalog
{
    public required IReadOnlyDictionary<string, ActivityAsset> Assets { get; init; }
    public required IReadOnlyDictionary<string, GroundingSnippet> Snippets { get; init; }

    public static ActivityCatalog Load(RepositoryPaths paths)
    {
        var healthAssets = LoadAssets(Path.Combine(paths.SharedDirectory, "activities-health-insurance.json"));
        var broadbandAssets = LoadAssets(Path.Combine(paths.SharedDirectory, "activities-broadband.json"));
        var snippetsPayload = JsonExtensions.LoadJsonObject(Path.Combine(paths.SharedDirectory, "grounding-snippets.json"));
        var snippets = snippetsPayload.RequireArrayProperty("snippets")
            .OfType<JsonObject>()
            .ToDictionary(
                static snippet => snippet.RequireStringProperty("snippetId"),
                static snippet => GroundingSnippet.FromJson(snippet),
                StringComparer.Ordinal);

        return new ActivityCatalog
        {
            Assets = healthAssets.Concat(broadbandAssets)
                .ToDictionary(
                    static asset => asset.AssetId,
                    StringComparer.Ordinal),
            Snippets = snippets,
        };
    }

    private static IEnumerable<ActivityAsset> LoadAssets(string path)
    {
        var payload = JsonExtensions.LoadJsonObject(path);
        return payload.RequireArrayProperty("assets").OfType<JsonObject>().Select(ActivityAsset.FromJson);
    }
}

internal sealed record ActivityAsset(
    string AssetId,
    string AssetType,
    string ServiceCategory,
    string MetadataRevision,
    string CtaType,
    string CtaLabel,
    string CtaDeepLink,
    IReadOnlySet<string> FunnelStages,
    IReadOnlySet<string> HouseholdFit,
    string ConversionGoal,
    string RetrievalSummary,
    AiSupportFields AiSupport)
{
    public static ActivityAsset FromJson(JsonObject json)
    {
        var cta = json.RequireObjectProperty("cta");
        var serviceSpecific = json.RequireObjectProperty("serviceSpecific");
        var aiSupport = json.RequireObjectProperty("aiSupportFields");
        return new(
            json.RequireStringProperty("assetId"),
            json.RequireStringProperty("assetType"),
            json.RequireStringProperty("serviceCategory"),
            json.RequireStringProperty("metadataRevision"),
            cta.RequireStringProperty("type"),
            cta.RequireStringProperty("label"),
            cta.RequireStringProperty("deepLink"),
            Strings(json, "funnelStages"),
            Strings(serviceSpecific, "householdFit"),
            json.RequireStringProperty("conversionGoal"),
            json.RequireStringProperty("retrievalSummary"),
            AiSupportFields.FromJson(aiSupport));
    }

    private static IReadOnlySet<string> Strings(JsonObject payload, string propertyName) =>
        payload.RequireArrayProperty(propertyName)
            .Select(static value => value?.GetValue<string>())
            .Where(static value => value is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
}

internal sealed record AiSupportFields(
    string PlainLanguageSummary,
    string ApprovedExplainerText,
    IReadOnlySet<string> RetrievalTags)
{
    public static AiSupportFields FromJson(JsonObject json) => new(
        json.RequireStringProperty("plainLanguageSummary"),
        json.RequireStringProperty("approvedExplainerText"),
        json.RequireArrayProperty("retrievalTags")
            .Select(static value => value?.GetValue<string>())
            .Where(static value => value is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal));
}

internal sealed record GroundingSnippet(
    string SnippetId,
    string ServiceCategory,
    string Content,
    IReadOnlyList<string> LinkedAssetIds,
    IReadOnlySet<string> Tags)
{
    public static GroundingSnippet FromJson(JsonObject json) => new(
        json.RequireStringProperty("snippetId"),
        json.RequireStringProperty("serviceCategory"),
        json.RequireStringProperty("content"),
        json.RequireArrayProperty("linkedAssets").Select(static value => value?.GetValue<string>() ?? string.Empty).ToArray(),
        json.RequireArrayProperty("tags")
            .Select(static value => value?.GetValue<string>())
            .Where(static value => value is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal));
}
