#!/usr/bin/env python3
"""
Minimal local smoke test for the Cosmos DB Emulator wiring.

This script intentionally validates only the infrastructure slice:
1. connect to the configured emulator endpoint
2. create a local database if it does not exist
3. create a small healthcheck container if it does not exist
4. read both resources back successfully
"""

from __future__ import annotations

import os
from typing import Any

from azure.cosmos import PartitionKey, exceptions

from local_runtime.config import CosmosConfig
from local_runtime.cosmos_store import create_client

HEALTHCHECK_CONTAINER = os.environ.get(
    "COSMOS_HEALTHCHECK_CONTAINER", "devcontainer-healthcheck"
)


def ensure_database(client: Any, database_id: str):
    try:
        client.create_database(id=database_id)
    except exceptions.CosmosResourceExistsError:
        pass

    database = client.get_database_client(database_id)
    database.read()
    return database


def ensure_container(database, container_id: str):
    try:
        database.create_container(
            id=container_id,
            partition_key=PartitionKey(path="/id"),
        )
    except exceptions.CosmosResourceExistsError:
        pass

    container = database.get_container_client(container_id)
    container.read()
    return container


def main() -> None:
    config = CosmosConfig.from_env()
    client = create_client(config)
    database = ensure_database(client, config.database)
    container = ensure_container(database, HEALTHCHECK_CONTAINER)

    print("Cosmos DB Emulator smoke test passed.")
    print(f"  endpoint:  {config.endpoint}")
    print(f"  database:  {database.id}")
    print(f"  container: {container.id}")


if __name__ == "__main__":
    main()
