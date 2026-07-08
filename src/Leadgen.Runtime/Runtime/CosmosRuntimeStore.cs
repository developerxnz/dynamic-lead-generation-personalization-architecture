using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

internal sealed class CosmosRuntimeStore : IAsyncDisposable
{
    private readonly CosmosClient _client;
    private readonly CosmosConfig _config;

    public CosmosRuntimeStore(CosmosConfig config)
    {
        _config = config;
        _client = new CosmosClient(
            config.Endpoint,
            config.Key,
            new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true,
            });
    }

    public async Task<CosmosRuntimeContainers> EnsureRuntimeContainersAsync()
    {
        var database = (await _client.CreateDatabaseIfNotExistsAsync(_config.Database)).Database;
        return new CosmosRuntimeContainers(
            await EnsureContainerAsync(database, _config.ProfilesContainer),
            await EnsureContainerAsync(database, _config.JourneysContainer),
            await EnsureContainerAsync(database, _config.EventsContainer),
            await EnsureContainerAsync(database, _config.DecisionTracesContainer));
    }

    public async Task SeedScenarioAsync(ScenarioInputs inputs)
    {
        var containers = await EnsureRuntimeContainersAsync();
        var profileDocument = inputs.Profile.DeepCloneObject();
        profileDocument["id"] = inputs.Profile.RequireStringProperty("customer_id");
        profileDocument["source_session_id"] = inputs.Session.RequireProperty("session_id").DeepClone();

        await containers.Profiles.UpsertItemAsync(
            ToCosmosDocument(profileDocument),
            new PartitionKey(inputs.Profile.RequireStringProperty("customer_id")));

        foreach (var journey in inputs.Journeys.RequireArrayProperty("journeys").OfType<JsonObject>())
        {
            var document = journey.DeepCloneObject();
            document["id"] = journey.RequireStringProperty("journey_id");
            document["scenario"] = inputs.Journeys.RequireProperty("scenario").DeepClone();
            document["customer_id"] = inputs.Journeys.RequireProperty("customer_id").DeepClone();
            document["source_session_id"] = inputs.Session.RequireProperty("session_id").DeepClone();

            await containers.Journeys.UpsertItemAsync(
                ToCosmosDocument(document),
                new PartitionKey(inputs.Profile.RequireStringProperty("customer_id")));
        }
    }

    public async Task<ScenarioInputs> LoadScenarioInputsAsync(string scenario, FixtureStore fixtures)
    {
        var fixtureInputs = fixtures.LoadScenarioInputs(scenario);
        var customerId = fixtureInputs.Profile.RequireStringProperty("customer_id");
        var containers = await EnsureRuntimeContainersAsync();

        var profile = FromCosmosDocument((await containers.Profiles.ReadItemAsync<JObject>(
            customerId,
            new PartitionKey(customerId))).Resource);

        var journeyQuery = new QueryDefinition(
            "SELECT * FROM c WHERE c.customer_id = @customer_id ORDER BY c.journey_id")
            .WithParameter("@customer_id", customerId);
        var iterator = containers.Journeys.GetItemQueryIterator<JObject>(
            journeyQuery,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(customerId) });

        var journeys = new List<JsonNode>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            journeys.AddRange(page.Resource.Where(static item => item is not null).Select(static item => (JsonNode)FromCosmosDocument(item!)));
        }

        return new ScenarioInputs(
            profile,
            new JsonObject
            {
                ["scenario"] = scenario,
                ["customer_id"] = customerId,
                ["journeys"] = new JsonArray(journeys.ToArray()),
            },
            fixtureInputs.Session.DeepCloneObject());
    }

    public async Task PersistRuntimeOutputsAsync(string scenario, JsonObject profile, JsonObject finalResponse, JsonObject analytics)
    {
        var containers = await EnsureRuntimeContainersAsync();
        var customerId = profile.RequireStringProperty("customer_id");

        var traceDocument = new JsonObject
        {
            ["id"] = $"{scenario}:{finalResponse.RequireStringProperty("session_id")}",
            ["customer_id"] = customerId,
            ["scenario"] = scenario,
            ["final_response"] = finalResponse.DeepCloneObject(),
        };
        await containers.DecisionTraces.UpsertItemAsync(ToCosmosDocument(traceDocument), new PartitionKey(customerId));

        foreach (var eventNode in analytics.RequireArrayProperty("events").OfType<JsonObject>())
        {
            var eventDocument = eventNode.DeepCloneObject();
            eventDocument["id"] =
                $"{scenario}:{eventNode.RequireStringProperty("event_type")}:{eventNode.RequireStringProperty("timestamp")}:{eventNode.RequireStringProperty("session_id")}";
            eventDocument["scenario"] = scenario;
            await containers.Events.UpsertItemAsync(ToCosmosDocument(eventDocument), new PartitionKey(customerId));
        }
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private static async Task<Container> EnsureContainerAsync(Database database, string containerName)
    {
        var response = await database.CreateContainerIfNotExistsAsync(
            containerName,
            "/customer_id");
        return response.Container;
    }

    private static JObject ToCosmosDocument(JsonObject document)
    {
        return JObject.Parse(document.ToJsonString());
    }

    private static JsonObject FromCosmosDocument(JObject document)
    {
        return JsonNode.Parse(document.ToString())!.RequireObject("cosmos_document");
    }
}

internal sealed record CosmosRuntimeContainers(
    Container Profiles,
    Container Journeys,
    Container Events,
    Container DecisionTraces);
