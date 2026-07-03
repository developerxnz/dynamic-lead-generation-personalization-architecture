from __future__ import annotations

import os
from dataclasses import dataclass

DEFAULT_EMULATOR_KEY = (
    "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPM"
    "bIZnqyMsEcaGQy67XIw/Jw=="
)


@dataclass(frozen=True)
class CosmosConfig:
    endpoint: str
    key: str
    database: str
    profiles_container: str
    journeys_container: str
    events_container: str
    decision_traces_container: str

    @classmethod
    def from_env(cls) -> "CosmosConfig":
        return cls(
            endpoint=os.environ.get("COSMOS_ENDPOINT", "http://cosmosdb:8081"),
            key=os.environ.get("COSMOS_KEY", DEFAULT_EMULATOR_KEY),
            database=os.environ.get("COSMOS_DATABASE", "leadgen-local"),
            profiles_container=os.environ.get("COSMOS_PROFILES_CONTAINER", "profiles"),
            journeys_container=os.environ.get("COSMOS_JOURNEYS_CONTAINER", "journeys"),
            events_container=os.environ.get("COSMOS_EVENTS_CONTAINER", "events"),
            decision_traces_container=os.environ.get(
                "COSMOS_DECISION_TRACES_CONTAINER", "decision-traces"
            ),
        )
