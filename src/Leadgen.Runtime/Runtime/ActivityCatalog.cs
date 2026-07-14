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
    string? Provider,
    int Priority,
    JsonObject Raw)
{
    public static ActivityAsset FromJson(JsonObject json)
    {
        var cta = json.RequireObjectProperty("cta");
        return new(
            json.RequireStringProperty("assetId"),
            json.RequireStringProperty("assetType"),
            json.RequireStringProperty("serviceCategory"),
            json.RequireStringProperty("metadataRevision"),
            cta.RequireStringProperty("type"),
            cta.RequireStringProperty("label"),
            cta.RequireStringProperty("deepLink"),
            json.OptionalStringProperty("provider"),
            json.RequireProperty("priority").GetValue<int>(),
            json.DeepCloneObject());
    }
}

internal sealed record GroundingSnippet(
    string SnippetId,
    string ServiceCategory,
    string Content,
    IReadOnlyList<string> LinkedAssetIds,
    JsonObject Raw)
{
    public static GroundingSnippet FromJson(JsonObject json) => new(
        json.RequireStringProperty("snippetId"),
        json.RequireStringProperty("serviceCategory"),
        json.RequireStringProperty("content"),
        json.RequireArrayProperty("linkedAssets").Select(static value => value?.GetValue<string>() ?? string.Empty).ToArray(),
        json.DeepCloneObject());
}
