using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Handles Cosmos-backed scenario state loading, seeding, inspection, reset, and runtime output persistence.
/// </summary>
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
        if (inputs.Profile is not null)
        {
            var profileDocument = inputs.Profile.DeepCloneObject();
            profileDocument["id"] = inputs.CustomerId;
            profileDocument["source_session_id"] = inputs.Session.RequireProperty("session_id").DeepClone();

            await containers.Profiles.UpsertItemAsync(
                ToCosmosDocument(profileDocument),
                new PartitionKey(inputs.CustomerId));
        }

        foreach (var journey in inputs.Journeys.RequireArrayProperty("journeys").OfType<JsonObject>())
        {
            var document = journey.DeepCloneObject();
            document["id"] = journey.RequireStringProperty("journey_id");
            document["scenario"] = inputs.Journeys.RequireProperty("scenario").DeepClone();
            document["customer_id"] = inputs.Journeys.RequireProperty("customer_id").DeepClone();
            document["source_session_id"] = inputs.Session.RequireProperty("session_id").DeepClone();

            await containers.Journeys.UpsertItemAsync(
                ToCosmosDocument(document),
                new PartitionKey(inputs.CustomerId));
        }
    }

    public async Task<ScenarioInputs> LoadScenarioInputsAsync(string scenario, FixtureStore fixtures)
    {
        var fixtureInputs = fixtures.LoadScenarioInputs(scenario);
        var customerId = fixtureInputs.CustomerId;
        var containers = await EnsureRuntimeContainersAsync();

        JsonObject? profile = null;
        try
        {
            profile = FromCosmosDocument((await containers.Profiles.ReadItemAsync<JObject>(
                customerId,
                new PartitionKey(customerId))).Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }

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

    public async Task PersistRuntimeOutputsAsync(
        string scenario,
        string customerId,
        JsonObject finalResponse,
        JsonObject analytics,
        JsonObject journeyInterpretation)
    {
        var containers = await EnsureRuntimeContainersAsync();

        var traceDocument = new JsonObject
        {
            ["id"] = $"{scenario}:{finalResponse.RequireStringProperty("session_id")}",
            ["customer_id"] = customerId,
            ["scenario"] = scenario,
            ["final_response"] = finalResponse.DeepCloneObject(),
            ["journey_interpretation"] = journeyInterpretation.DeepCloneObject(),
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

    public async Task ResetScenarioAsync(string customerId)
    {
        var containers = await EnsureRuntimeContainersAsync();
        await DeleteItemsByCustomerAsync(containers.Profiles, customerId);
        await DeleteItemsByCustomerAsync(containers.Journeys, customerId);
        await DeleteItemsByCustomerAsync(containers.Events, customerId);
        await DeleteItemsByCustomerAsync(containers.DecisionTraces, customerId);
    }

    public async Task<JsonObject> InspectScenarioStateAsync(string customerId)
    {
        var containers = await EnsureRuntimeContainersAsync();

        JsonObject? profile = null;
        try
        {
            profile = FromCosmosDocument((await containers.Profiles.ReadItemAsync<JObject>(
                customerId,
                new PartitionKey(customerId))).Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }

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

        return new JsonObject
        {
            ["profile"] = profile,
            ["journeys"] = new JsonArray(journeys.ToArray()),
        };
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

    private static async Task DeleteItemsByCustomerAsync(Container container, string customerId)
    {
        var query = new QueryDefinition("SELECT c.id FROM c WHERE c.customer_id = @customer_id")
            .WithParameter("@customer_id", customerId);
        var iterator = container.GetItemQueryIterator<JObject>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(customerId) });

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            foreach (var item in page.Resource)
            {
                var itemId = item["id"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    await container.DeleteItemAsync<JObject>(itemId, new PartitionKey(customerId));
                }
            }
        }
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

/// <summary>
/// Groups the Cosmos containers used by the runtime for profiles, journeys, events, and decision traces.
/// </summary>
internal sealed record CosmosRuntimeContainers(
    Container Profiles,
    Container Journeys,
    Container Events,
    Container DecisionTraces);
