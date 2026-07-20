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
            await containers.Profiles.UpsertItemAsync(
                inputs.Profile,
                new PartitionKey(inputs.CustomerId));
        }

        foreach (var journey in inputs.JourneyStates.Select(
                     journey => journey.ToCosmosDocument(inputs.Scenario, inputs.SessionContext.SessionId)))
        {
            await containers.Journeys.UpsertItemAsync(
                journey,
                new PartitionKey(inputs.CustomerId));
        }
    }

    public async Task<ScenarioInputs> LoadScenarioInputsAsync(string scenario, FixtureStore fixtures)
    {
        var fixtureInputs = fixtures.LoadScenarioInputs(scenario);
        var customerId = fixtureInputs.CustomerId;
        var containers = await EnsureRuntimeContainersAsync();

        CosmosCustomerProfileDocument? profile = null;
        try
        {
            profile = (await containers.Profiles.ReadItemAsync<CosmosCustomerProfileDocument>(
                customerId,
                new PartitionKey(customerId))).Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }

        var journeyQuery = new QueryDefinition(
            "SELECT * FROM c WHERE c.customer_id = @customer_id ORDER BY c.journey_id")
            .WithParameter("@customer_id", customerId);
        var iterator = containers.Journeys.GetItemQueryIterator<CosmosJourneyDocument>(
            journeyQuery,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(customerId) });

        var journeys = new List<JourneyState>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            journeys.AddRange(page.Resource.Select(static journey => journey.ToJourneyState()));
        }

        return new ScenarioInputs(
            scenario,
            profile,
            journeys,
            fixtureInputs.SessionContext);
    }

    public async Task PersistRuntimeOutputsAsync(
        string scenario,
        string customerId,
        FinalResponseEnvelope finalResponse,
        AnalyticsEnvelope analytics,
        JourneyInterpretation journeyInterpretation)
    {
        var containers = await EnsureRuntimeContainersAsync();

        var traceDocument = new DecisionTraceDocument(
            $"{scenario}:{finalResponse.SessionId}",
            customerId,
            scenario,
            finalResponse,
            journeyInterpretation);
        await containers.DecisionTraces.UpsertItemAsync(
            ToCosmosDocument(traceDocument.ToJson()),
            new PartitionKey(customerId));

        foreach (var analyticsEvent in analytics.Events)
        {
            var eventDocument = analyticsEvent.ToJson();
            eventDocument["id"] =
                $"{scenario}:{analyticsEvent.EventType}:{analyticsEvent.Timestamp:O}:{analyticsEvent.SessionId}";
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
            profile = (await containers.Profiles.ReadItemAsync<CosmosCustomerProfileDocument>(
                customerId,
                new PartitionKey(customerId))).Resource.ToFixtureJson();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }

        var journeyQuery = new QueryDefinition(
            "SELECT * FROM c WHERE c.customer_id = @customer_id ORDER BY c.journey_id")
            .WithParameter("@customer_id", customerId);
        var iterator = containers.Journeys.GetItemQueryIterator<CosmosJourneyDocument>(
            journeyQuery,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(customerId) });

        var journeys = new List<JsonNode>();
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync();
            journeys.AddRange(page.Resource.Select(static journey => (JsonNode)journey.ToFixtureJson()));
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
