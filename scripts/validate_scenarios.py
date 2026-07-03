#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import subprocess
import tempfile
from pathlib import Path

from local_runtime.fixtures import list_scenarios, load_scenario_artifact


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Replay and validate mock-data scenarios locally.")
    parser.add_argument("scenarios", nargs="*", choices=list_scenarios())
    parser.add_argument("--source", choices=["fixtures", "cosmos"], default="fixtures")
    parser.add_argument("--ai-mode", choices=["expected", "ollama"], default="expected")
    return parser.parse_args()


def _load(path: Path) -> dict:
    with open(path) as handle:
        return json.load(handle)


def _normalize_event_payload(payload: dict, ai_mode: str) -> dict:
    normalized = json.loads(json.dumps(payload))
    if ai_mode == "ollama":
        for event in normalized.get("events", []):
            if event["event_type"] == "ai_response_accepted":
                event["metadata"]["response_id"] = "__dynamic__"
                event["metadata"]["latency_ms"] = "__dynamic__"
    return normalized


def _normalize_final_response(payload: dict, ai_mode: str) -> dict:
    normalized = json.loads(json.dumps(payload))
    if ai_mode == "ollama":
        normalized["explanation"]["ai_response_id"] = "__dynamic__"
        normalized["explanation"]["summary"] = "__dynamic__"
        normalized["explanation"]["cta_support_text"] = "__dynamic__"
    return normalized


def validate_scenario(scenario: str, source: str, ai_mode: str) -> tuple[bool, list[str]]:
    with tempfile.TemporaryDirectory() as temp_dir:
        subprocess.run(
            [
                "python",
                "scripts/run_scenario.py",
                scenario,
                "--source",
                source,
                "--ai-mode",
                ai_mode,
                "--output-dir",
                temp_dir,
            ],
            cwd="/workspace",
            check=True,
            env=os.environ.copy(),
        )

        actual = {
            "04": _load(Path(temp_dir) / "04-active-journey-selection.json"),
            "05": _load(Path(temp_dir) / "05-candidate-retrieval.json"),
            "06": _load(Path(temp_dir) / "06-ranking-request.json"),
            "07": _load(Path(temp_dir) / "07-ranking-response.json"),
            "08": _load(Path(temp_dir) / "08-ai-prompt-input.json"),
            "10": _normalize_final_response(
                _load(Path(temp_dir) / "10-final-response.json"), ai_mode
            ),
            "11": _normalize_event_payload(
                _load(Path(temp_dir) / "11-analytics-events.json"), ai_mode
            ),
        }

        expected = {
            "04": load_scenario_artifact(scenario, "04-active-journey-selection.json"),
            "05": load_scenario_artifact(scenario, "05-candidate-retrieval.json"),
            "06": load_scenario_artifact(scenario, "06-ranking-request.json"),
            "07": load_scenario_artifact(scenario, "07-ranking-response.json"),
            "08": load_scenario_artifact(scenario, "08-ai-prompt-input.json"),
            "10": _normalize_final_response(
                load_scenario_artifact(scenario, "10-final-response.json"), ai_mode
            ),
            "11": _normalize_event_payload(
                load_scenario_artifact(scenario, "11-analytics-events.json"), ai_mode
            ),
        }

        mismatches = [name for name in expected if expected[name] != actual[name]]
        return (len(mismatches) == 0, mismatches)


def main() -> None:
    args = parse_args()
    scenarios = args.scenarios or list_scenarios()
    passed = 0
    total = len(scenarios)

    for scenario in scenarios:
        ok, mismatches = validate_scenario(scenario, args.source, args.ai_mode)
        if ok:
            print(f"PASS {scenario}")
            passed += 1
        else:
            print(f"FAIL {scenario} mismatches: {', '.join(mismatches)}")

    print(f"{passed}/{total} scenarios passed")
    raise SystemExit(0 if passed == total else 1)


if __name__ == "__main__":
    main()
