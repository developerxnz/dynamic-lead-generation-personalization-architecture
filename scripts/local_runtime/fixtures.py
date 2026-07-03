from __future__ import annotations

import json
from pathlib import Path

SCENARIOS_DIR = Path(__file__).resolve().parents[2] / "mock-data" / "scenarios"


def scenario_dir(scenario_name: str) -> Path:
    path = SCENARIOS_DIR / scenario_name
    if not path.is_dir():
        raise FileNotFoundError(f"Unknown scenario: {scenario_name}")
    return path


def load_json(path: Path) -> dict:
    with open(path) as handle:
        return json.load(handle)


def load_scenario_inputs(scenario_name: str) -> tuple[dict, dict, dict]:
    directory = scenario_dir(scenario_name)
    profile = load_json(directory / "01-customer-profile.json")
    journeys = load_json(directory / "02-journey-states.json")
    session = load_json(directory / "03-session-request.json")
    return profile, journeys, session


def load_scenario_artifact(scenario_name: str, file_name: str) -> dict:
    return load_json(scenario_dir(scenario_name) / file_name)


def list_scenarios() -> list[str]:
    return sorted(path.name for path in SCENARIOS_DIR.iterdir() if path.is_dir())
