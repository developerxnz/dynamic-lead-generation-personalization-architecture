#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
from pathlib import Path

from local_runtime.activity_catalog import load_catalog
from local_runtime.orchestrator import (
    build_ai_prompt_input,
    build_analytics_events,
    build_final_response,
    build_ranking_request,
    load_runtime_inputs,
    persist_runtime_outputs,
    rank_candidates,
    retrieve_candidates,
    run_ai_explanation,
    select_active_journey,
    write_outputs,
)
from local_runtime.fixtures import list_scenarios


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run one mock-data scenario locally.")
    parser.add_argument("scenario", choices=list_scenarios())
    parser.add_argument(
        "--source",
        choices=["fixtures", "cosmos"],
        default="fixtures",
        help="Load profile/journeys from fixture files or the Cosmos emulator.",
    )
    parser.add_argument(
        "--ai-mode",
        choices=["expected", "ollama"],
        default="expected",
        help="Use checked-in expected AI output or call the local Ollama endpoint.",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Directory to write generated artifacts. Defaults to /tmp/leadgen-scenario-runs/<scenario>.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    profile, journeys, session = load_runtime_inputs(args.scenario, source=args.source)
    catalog = load_catalog()

    selection = select_active_journey(args.scenario, profile, journeys, session)
    retrieval = retrieve_candidates(args.scenario, profile, journeys, session, selection, catalog)
    ranking_request = build_ranking_request(
        args.scenario, profile, journeys, session, selection, retrieval, catalog
    )
    ranking_response = rank_candidates(args.scenario, catalog)
    prompt_input = build_ai_prompt_input(
        args.scenario, profile, journeys, ranking_request, ranking_response, catalog
    )
    ai_response, ai_record = run_ai_explanation(
        args.scenario,
        prompt_input,
        ai_mode=args.ai_mode,
        base_url=os.environ.get("OLLAMA_BASE_URL", "http://ollama:11434"),
        model=os.environ.get("MODEL") or os.environ.get("OLLAMA_MODEL", "llama3.1:8b"),
    )
    final_response = build_final_response(
        args.scenario, profile, session, ranking_response, ai_record, ai_response, catalog
    )
    analytics = build_analytics_events(
        args.scenario,
        profile,
        journeys,
        session,
        selection,
        ranking_response,
        final_response,
        ai_record,
        catalog,
    )

    outputs = {
        "04": selection,
        "05": retrieval,
        "06": ranking_request,
        "07": ranking_response,
        "08": prompt_input,
        "09": ai_record,
        "10": final_response,
        "11": analytics,
    }

    output_dir = (
        Path(args.output_dir)
        if args.output_dir
        else Path("/tmp/leadgen-scenario-runs") / args.scenario
    )
    write_outputs(outputs, output_dir)
    persist_runtime_outputs(args.scenario, profile, final_response, analytics)

    print(f"Ran scenario: {args.scenario}")
    print(f"  source: {args.source}")
    print(f"  ai_mode: {args.ai_mode}")
    print(f"  output_dir: {output_dir}")


if __name__ == "__main__":
    main()
