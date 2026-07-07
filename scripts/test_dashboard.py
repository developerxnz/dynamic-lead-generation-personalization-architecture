#!/usr/bin/env python3
from __future__ import annotations

import argparse
import io
import json
import os
import subprocess
import time
from contextlib import redirect_stdout
from datetime import datetime, timezone
from pathlib import Path

from local_runtime.fixtures import list_scenarios, load_scenario_artifact
from reset_scenario_state import reset_scenario
from seed_scenario import seed_scenario
from validate_scenarios import _load, _normalize_event_payload, _normalize_final_response


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate a local dashboard for deterministic scenario validation."
    )
    parser.add_argument("scenarios", nargs="*", choices=list_scenarios())
    parser.add_argument("--source", choices=["fixtures", "cosmos"], default="fixtures")
    parser.add_argument("--ai-mode", choices=["expected", "ollama"], default="expected")
    parser.add_argument(
        "--cosmos-clear",
        choices=["none", "before", "after", "both"],
        default="both",
        help="When --source cosmos is used, clear scenario state before and/or after each run.",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Directory to write the dashboard and scenario artifacts.",
    )
    return parser.parse_args()


def default_output_dir() -> Path:
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    return Path("/tmp/leadgen-test-dashboard") / timestamp


def compare_outputs(scenario: str, output_dir: Path, ai_mode: str) -> list[str]:
    actual = {
        "04": _load(output_dir / "04-active-journey-selection.json"),
        "05": _load(output_dir / "05-candidate-retrieval.json"),
        "06": _load(output_dir / "06-ranking-request.json"),
        "07": _load(output_dir / "07-ranking-response.json"),
        "08": _load(output_dir / "08-ai-prompt-input.json"),
        "10": _normalize_final_response(_load(output_dir / "10-final-response.json"), ai_mode),
        "11": _normalize_event_payload(_load(output_dir / "11-analytics-events.json"), ai_mode),
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

    return [name for name in expected if expected[name] != actual[name]]


def silently(callable_obj, *args) -> None:
    buffer = io.StringIO()
    with redirect_stdout(buffer):
        callable_obj(*args)


def format_command_failure(completed: subprocess.CompletedProcess[str]) -> str:
    sections = [f"run_scenario.py exited with code {completed.returncode}."]

    if completed.stdout.strip():
        sections.append(f"stdout:\n{completed.stdout.strip()}")

    if completed.stderr.strip():
        sections.append(f"stderr:\n{completed.stderr.strip()}")

    return "\n\n".join(sections)


def should_clear_before(cosmos_clear: str) -> bool:
    return cosmos_clear in {"before", "both"}


def should_clear_after(cosmos_clear: str) -> bool:
    return cosmos_clear in {"after", "both"}


def validate_scenario_with_report(
    scenario: str,
    source: str,
    ai_mode: str,
    cosmos_clear: str,
    output_dir: Path,
) -> dict:
    scenario_dir = output_dir / "scenarios" / scenario
    scenario_dir.mkdir(parents=True, exist_ok=True)

    started_at = datetime.now(timezone.utc)
    started = time.perf_counter()
    error = None
    status = "ERROR"
    mismatches: list[str] = []

    try:
        if source == "cosmos":
            if should_clear_before(cosmos_clear):
                silently(reset_scenario, scenario)
            silently(seed_scenario, scenario)

        completed = subprocess.run(
            [
                "python",
                "scripts/run_scenario.py",
                scenario,
                "--source",
                source,
                "--ai-mode",
                ai_mode,
                "--output-dir",
                str(scenario_dir),
            ],
            cwd="/workspace",
            check=False,
            capture_output=True,
            text=True,
            env=os.environ.copy(),
        )

        if completed.returncode != 0:
            error = format_command_failure(completed)
        else:
            mismatches = compare_outputs(scenario, scenario_dir, ai_mode)
            status = "PASS" if not mismatches else "FAIL"
            if completed.stdout.strip():
                (scenario_dir / "run.log").write_text(completed.stdout, encoding="utf-8")
            if completed.stderr.strip():
                (scenario_dir / "run.stderr.log").write_text(
                    completed.stderr, encoding="utf-8"
                )
    except Exception as exc:  # noqa: BLE001
        error = str(exc)
    finally:
        cleanup_error = None
        if source == "cosmos" and should_clear_after(cosmos_clear):
            try:
                silently(reset_scenario, scenario)
            except Exception as exc:  # noqa: BLE001
                cleanup_error = f"Cosmos cleanup failed: {exc}"

        duration_seconds = round(time.perf_counter() - started, 3)
        finished_at = datetime.now(timezone.utc)

        if cleanup_error:
            status = "ERROR"
            error = f"{error}\n\n{cleanup_error}" if error else cleanup_error
        elif error is None and status == "ERROR":
            error = "Unknown validation error."

    return {
        "scenario": scenario,
        "status": status,
        "mismatches": mismatches,
        "error": error,
        "duration_seconds": duration_seconds,
        "started_at": started_at.isoformat(),
        "finished_at": finished_at.isoformat(),
        "artifact_dir": str(scenario_dir),
        "artifact_index": f"scenarios/{scenario}/10-final-response.json",
    }


def build_markdown(report: dict) -> str:
    lines = [
        "# Leadgen Test Results",
        "",
        f"- **Source:** `{report['source']}`",
        f"- **AI mode:** `{report['ai_mode']}`",
        f"- **Generated:** `{report['generated_at']}`",
        f"- **Duration:** `{report['duration_seconds']:.3f}s`",
        "",
        "## Summary",
        "",
        f"- **Passed:** {report['passed']}",
        f"- **Failed:** {report['failed']}",
        f"- **Errored:** {report['errored']}",
        "",
        "## Scenario Results",
        "",
        "| Scenario | Status | Duration | Details | Artifact |",
        "|---|---|---:|---|---|",
    ]

    for item in report["results"]:
        detail = ", ".join(item["mismatches"]) if item["mismatches"] else "Matched expected artifacts."
        if item["error"]:
            detail = item["error"].replace("\n", "<br>")

        artifact = item["artifact_index"] if Path(report["output_dir"], item["artifact_index"]).exists() else "n/a"
        lines.append(
            f"| `{item['scenario']}` | **{item['status']}** | {item['duration_seconds']:.3f}s | {detail} | `{artifact}` |"
        )

    return "\n".join(lines) + "\n"


def main() -> None:
    args = parse_args()
    scenarios = args.scenarios or list_scenarios()
    output_dir = Path(args.output_dir) if args.output_dir else default_output_dir()
    output_dir.mkdir(parents=True, exist_ok=True)

    started = time.perf_counter()
    results = [
        validate_scenario_with_report(
            scenario=scenario,
            source=args.source,
            ai_mode=args.ai_mode,
            cosmos_clear=args.cosmos_clear,
            output_dir=output_dir,
        )
        for scenario in scenarios
    ]
    duration_seconds = round(time.perf_counter() - started, 3)

    passed = sum(item["status"] == "PASS" for item in results)
    failed = sum(item["status"] == "FAIL" for item in results)
    errored = sum(item["status"] == "ERROR" for item in results)

    report = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "source": args.source,
        "ai_mode": args.ai_mode,
        "cosmos_clear": args.cosmos_clear,
        "output_dir": str(output_dir),
        "scenarios": scenarios,
        "passed": passed,
        "failed": failed,
        "errored": errored,
        "duration_seconds": duration_seconds,
        "results": results,
    }

    (output_dir / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    (output_dir / "results.md").write_text(build_markdown(report), encoding="utf-8")

    print(f"Markdown report written to: {output_dir / 'results.md'}")
    print(f"JSON report written to: {output_dir / 'report.json'}")
    print(f"Summary: {passed} passed, {failed} failed, {errored} errored")

    raise SystemExit(0 if failed == 0 and errored == 0 else 1)


if __name__ == "__main__":
    main()
