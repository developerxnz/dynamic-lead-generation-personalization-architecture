from __future__ import annotations

from collections.abc import Iterable

from azure.cosmos import CosmosClient, PartitionKey, exceptions

from local_runtime.config import CosmosConfig

CONTAINER_PARTITION_KEYS = {
    "profiles": "/customer_id",
    "journeys": "/customer_id",
    "events": "/customer_id",
    "decision-traces": "/customer_id",
}


def create_client(config: CosmosConfig) -> CosmosClient:
    return CosmosClient(config.endpoint, credential=config.key)


def ensure_database(client: CosmosClient, database_name: str):
    try:
        client.create_database(id=database_name)
    except exceptions.CosmosResourceExistsError:
        pass

    database = client.get_database_client(database_name)
    database.read()
    return database


def ensure_container(database, container_name: str):
    partition_key = CONTAINER_PARTITION_KEYS.get(container_name, "/customer_id")
    try:
        database.create_container(
            id=container_name,
            partition_key=PartitionKey(path=partition_key),
        )
    except exceptions.CosmosResourceExistsError:
        pass

    container = database.get_container_client(container_name)
    container.read()
    return container


def ensure_runtime_containers(database, config: CosmosConfig) -> dict[str, object]:
    return {
        "profiles": ensure_container(database, config.profiles_container),
        "journeys": ensure_container(database, config.journeys_container),
        "events": ensure_container(database, config.events_container),
        "decision-traces": ensure_container(database, config.decision_traces_container),
    }


def upsert_items(container, items: Iterable[dict]) -> int:
    count = 0
    for item in items:
        container.upsert_item(item)
        count += 1
    return count


def delete_items_by_customer(container, customer_id: str) -> int:
    query = "SELECT c.id FROM c WHERE c.customer_id = @customer_id"
    parameters = [{"name": "@customer_id", "value": customer_id}]
    ids = [
        item["id"]
        for item in container.query_items(
            query=query,
            parameters=parameters,
            enable_cross_partition_query=True,
        )
    ]

    for item_id in ids:
        container.delete_item(item=item_id, partition_key=customer_id)

    return len(ids)
