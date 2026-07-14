using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Reads checked-in scenario fixtures and expected artifacts from the mock-data directory.
/// </summary>
internal sealed class FixtureStore
{
    private readonly RepositoryPaths _paths;

    public FixtureStore(RepositoryPaths paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<string> ListScenarios()
    {
        return Directory
            .EnumerateDirectories(_paths.ScenariosDirectory)
            .Select(Path.GetFileName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }

    public ScenarioInputs LoadScenarioInputs(string scenario)
    {
        var directory = GetScenarioDirectory(scenario);
        var profilePath = Path.Combine(directory, "01-customer-profile.json");
        return new ScenarioInputs(
            File.Exists(profilePath) ? JsonExtensions.LoadJsonObject(profilePath) : null,
            JsonExtensions.LoadJsonObject(Path.Combine(directory, "02-journey-states.json")),
            JsonExtensions.LoadJsonObject(Path.Combine(directory, "03-session-request.json")));
    }

    public JsonObject LoadScenarioArtifact(string scenario, string fileName)
    {
        return JsonExtensions.LoadJsonObject(Path.Combine(GetScenarioDirectory(scenario), fileName));
    }

    private string GetScenarioDirectory(string scenario)
    {
        var directory = Path.Combine(_paths.ScenariosDirectory, scenario);
        if (!Directory.Exists(directory))
        {
            throw new FileNotFoundException($"Unknown scenario: {scenario}");
        }

        return directory;
    }
}

/// <summary>
/// Bundles the core fixture inputs needed to run a scenario.
/// </summary>
internal sealed record ScenarioInputs(JsonObject? Profile, JsonObject Journeys, JsonObject Session)
{
    public IReadOnlyList<JourneyState> JourneyStates => JourneyContractAdapter.JourneyStates(Journeys);

    public SessionContext SessionContext => JourneyContractAdapter.Session(Session);

    public string CustomerId =>
        Profile?.OptionalStringProperty("customer_id")
        ?? Session.RequireStringProperty("customer_id");
}
