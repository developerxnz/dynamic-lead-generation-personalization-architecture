from __future__ import annotations

from pathlib import Path

from local_runtime.fixtures import load_json

SHARED_DIR = Path(__file__).resolve().parents[2] / "mock-data" / "shared"


def _load_assets(file_name: str) -> list[dict]:
    payload = load_json(SHARED_DIR / file_name)
    return payload["assets"]


def load_catalog() -> dict[str, dict]:
    assets = _load_assets("activities-health-insurance.json") + _load_assets(
        "activities-broadband.json"
    )
    snippets = load_json(SHARED_DIR / "grounding-snippets.json")["snippets"]

    return {
        "assets": {asset["assetId"]: asset for asset in assets},
        "snippets": {snippet["snippetId"]: snippet for snippet in snippets},
    }
