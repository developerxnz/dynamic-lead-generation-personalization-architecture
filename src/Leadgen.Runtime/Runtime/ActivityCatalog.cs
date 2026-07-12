using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Loads the shared activity metadata and grounding snippets used during retrieval and ranking assembly.
/// </summary>
internal sealed class ActivityCatalog
{
    public required IReadOnlyDictionary<string, JsonObject> Assets { get; init; }
    public required IReadOnlyDictionary<string, JsonObject> Snippets { get; init; }

    public static ActivityCatalog Load(RepositoryPaths paths)
    {
        var healthAssets = LoadAssets(Path.Combine(paths.SharedDirectory, "activities-health-insurance.json"));
        var broadbandAssets = LoadAssets(Path.Combine(paths.SharedDirectory, "activities-broadband.json"));
        var snippetsPayload = JsonExtensions.LoadJsonObject(Path.Combine(paths.SharedDirectory, "grounding-snippets.json"));
        var snippets = snippetsPayload.RequireArrayProperty("snippets")
            .OfType<JsonObject>()
            .ToDictionary(
                static snippet => snippet.RequireStringProperty("snippetId"),
                static snippet => snippet,
                StringComparer.Ordinal);

        return new ActivityCatalog
        {
            Assets = healthAssets.Concat(broadbandAssets)
                .ToDictionary(
                    static asset => asset.RequireStringProperty("assetId"),
                    static asset => asset,
                    StringComparer.Ordinal),
            Snippets = snippets,
        };
    }

    private static IEnumerable<JsonObject> LoadAssets(string path)
    {
        var payload = JsonExtensions.LoadJsonObject(path);
        return payload.RequireArrayProperty("assets").OfType<JsonObject>();
    }
}
