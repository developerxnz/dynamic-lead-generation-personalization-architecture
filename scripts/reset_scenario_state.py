#!/usr/bin/env python3
from __future__ import annotations

import argparse

from local_runtime.config import CosmosConfig
from local_runtime.cosmos_store import (
    create_client,
    delete_items_by_customer,
    ensure_database,
    ensure_runtime_containers,
)
from local_runtime.fixtures import list_scenarios, load_scenario_inputs


def reset_scenario(scenario_name: str) -> None:
    profile, _, _ = load_scenario_inputs(scenario_name)
    customer_id = profile["customer_id"]

    config = CosmosConfig.from_env()
    client = create_client(config)
    database = ensure_database(client, config.database)
    containers = ensure_runtime_containers(database, config)

    deleted = {
        name: delete_items_by_customer(container, customer_id)
        for name, container in containers.items()
    }

    print(f"Reset scenario state: {scenario_name}")
    print(f"  customer_id: {customer_id}")
    for container_name, count in deleted.items():
        print(f"  {container_name}: deleted {count}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Delete stored local Cosmos DB Emulator state for one mock-data scenario."
    )
    parser.add_argument("scenario", choices=list_scenarios())
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    reset_scenario(args.scenario)


if __name__ == "__main__":
    main()
