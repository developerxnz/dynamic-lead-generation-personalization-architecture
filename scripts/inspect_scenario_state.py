#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json

from local_runtime.config import CosmosConfig
from local_runtime.cosmos_store import create_client, ensure_database, ensure_runtime_containers
from local_runtime.fixtures import list_scenarios, load_scenario_inputs


def inspect_scenario(scenario_name: str) -> None:
    profile_fixture, _, _ = load_scenario_inputs(scenario_name)
    customer_id = profile_fixture["customer_id"]

    config = CosmosConfig.from_env()
    client = create_client(config)
    database = ensure_database(client, config.database)
    containers = ensure_runtime_containers(database, config)

    profile = containers["profiles"].read_item(item=customer_id, partition_key=customer_id)
    journeys = list(
        containers["journeys"].query_items(
            query="SELECT * FROM c WHERE c.customer_id = @customer_id",
            parameters=[{"name": "@customer_id", "value": customer_id}],
            enable_cross_partition_query=True,
        )
    )

    print(json.dumps({"profile": profile, "journeys": journeys}, indent=2))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Read back one scenario's seeded customer profile and journeys from the local Cosmos DB Emulator."
    )
    parser.add_argument("scenario", choices=list_scenarios())
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    inspect_scenario(args.scenario)


if __name__ == "__main__":
    main()
