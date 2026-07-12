namespace Leadgen.Runtime;

/// <summary>
/// Holds the Cosmos emulator connection and container settings used by the local runtime.
/// </summary>
internal sealed record CosmosConfig(
    string Endpoint,
    string Key,
    string Database,
    string ProfilesContainer,
    string JourneysContainer,
    string EventsContainer,
    string DecisionTracesContainer)
{
    private const string DefaultEmulatorKey =
        "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPM" +
        "bIZnqyMsEcaGQy67XIw/Jw==";

    public static CosmosConfig FromEnvironment()
    {
        return new CosmosConfig(
            Environment.GetEnvironmentVariable("COSMOS_ENDPOINT") ?? "http://cosmosdb:8081",
            Environment.GetEnvironmentVariable("COSMOS_KEY") ?? DefaultEmulatorKey,
            Environment.GetEnvironmentVariable("COSMOS_DATABASE") ?? "leadgen-local",
            Environment.GetEnvironmentVariable("COSMOS_PROFILES_CONTAINER") ?? "profiles",
            Environment.GetEnvironmentVariable("COSMOS_JOURNEYS_CONTAINER") ?? "journeys",
            Environment.GetEnvironmentVariable("COSMOS_EVENTS_CONTAINER") ?? "events",
            Environment.GetEnvironmentVariable("COSMOS_DECISION_TRACES_CONTAINER") ?? "decision-traces");
    }
}
