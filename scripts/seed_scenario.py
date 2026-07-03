#!/usr/bin/env python3
from __future__ import annotations

import argparse

from local_runtime.config import CosmosConfig
from local_runtime.cosmos_store import (
    create_client,
    ensure_database,
    ensure_runtime_containers,
    upsert_items,
)
from local_runtime.fixtures import list_scenarios, load_scenario_inputs


def build_profile_document(profile: dict, session: dict) -> dict:
    document = dict(profile)
    document["id"] = profile["customer_id"]
    document["source_session_id"] = session["session_id"]
    return document


def build_journey_documents(journeys_payload: dict, session: dict) -> list[dict]:
    scenario_name = journeys_payload["scenario"]
    customer_id = journeys_payload["customer_id"]
    documents = []

    for journey in journeys_payload["journeys"]:
        document = dict(journey)
        document["id"] = journey["journey_id"]
        document["scenario"] = scenario_name
        document["customer_id"] = customer_id
        document["source_session_id"] = session["session_id"]
        documents.append(document)

    return documents


def seed_scenario(scenario_name: str) -> None:
    profile, journeys, session = load_scenario_inputs(scenario_name)
    config = CosmosConfig.from_env()
    client = create_client(config)
    database = ensure_database(client, config.database)
    containers = ensure_runtime_containers(database, config)

    profile_count = upsert_items(
        containers["profiles"],
        [build_profile_document(profile, session)],
    )
    journey_count = upsert_items(
        containers["journeys"],
        build_journey_documents(journeys, session),
    )

    print(f"Seeded scenario: {scenario_name}")
    print(f"  customer_id: {profile['customer_id']}")
    print(f"  profiles upserted: {profile_count}")
    print(f"  journeys upserted: {journey_count}")
    print(f"  database: {config.database}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Seed a mock-data scenario into the local Cosmos DB Emulator."
    )
    parser.add_argument("scenario", choices=list_scenarios())
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    seed_scenario(args.scenario)


if __name__ == "__main__":
    main()
