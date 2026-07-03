#!/usr/bin/env python3
"""
Evaluate all four POC scenarios against a local Ollama model.

For each scenario this script:
  1. Reads 08-ai-prompt-input.json and assembles the OpenAI messages
  2. Sends the request to Ollama
  3. Parses the response JSON
  4. Validates it against the contract in 08-ai-prompt-input.json
  5. Compares it to the expected output in 09-ai-expected-output.json
  6. Prints a pass/fail report

Usage (inside devcontainer):
    python scripts/evaluate.py

    # Use a different model:
    MODEL=phi4:14b python scripts/evaluate.py

    # Point at a different Ollama endpoint:
    OLLAMA_BASE_URL=http://localhost:11434 python scripts/evaluate.py
"""

import json
import os
import sys
from pathlib import Path

from openai import OpenAI

SCENARIOS_DIR = Path(__file__).parent.parent / "mock-data" / "scenarios"
OLLAMA_BASE_URL = os.environ.get("OLLAMA_BASE_URL", "http://ollama:11434")
MODEL = os.environ.get("MODEL") or os.environ.get("OLLAMA_MODEL", "llama3.1:8b")


def list_scenarios() -> list[str]:
    return sorted(path.name for path in SCENARIOS_DIR.iterdir() if path.is_dir())


def load_json(path: Path) -> dict:
    with open(path) as f:
        return json.load(f)


def format_grounding_context(prompt_input: dict) -> str:
    lines = [
        "Use only the following grounded assets.",
        "If you cite grounding_asset_ids, copy the asset_id values exactly as written.",
        "Do not return snippet_id values.",
        "",
    ]

    for item in prompt_input["grounding_context"]:
        lines.extend(
            [
                f"- asset_id: {item['asset_id']}",
                f"  snippet_id: {item['snippet_id']}",
                f"  content: {item['content']}",
            ]
        )

    return "\n".join(lines)


def assemble_user_message(prompt_input: dict) -> str:
    parts = [
        prompt_input["task_prompt"],
        "",
        "## Journey context",
        json.dumps(prompt_input["journey_context"], indent=2),
        "",
        "## Customer context",
        json.dumps(prompt_input["customer_context"], indent=2),
        "",
        "## Selected action",
        json.dumps(prompt_input["selected_action"], indent=2),
        "",
        "## Grounding context",
        format_grounding_context(prompt_input),
        "",
        "## Response contract",
        "You must return a JSON object with exactly these fields:",
        json.dumps(prompt_input["response_contract"]["required_fields"], indent=2),
        f"- summary: max {prompt_input['response_contract']['summary_max_words']} words",
        f"- key_points: max {prompt_input['response_contract']['key_points_max_count']} items",
        f"- cta_support_text: max {prompt_input['response_contract']['cta_support_text_max_words']} words",
        "- grounding_asset_ids: list of asset IDs from the grounding context you used",
        "",
        "Return only the JSON object. No explanation or prose outside the JSON.",
    ]
    return "\n".join(parts)


def normalize_grounding_asset_ids(response_obj: dict, prompt_input: dict) -> dict:
    normalized = dict(response_obj)
    grounding_ids = response_obj.get("grounding_asset_ids")
    if not isinstance(grounding_ids, list):
        return normalized

    snippet_to_asset = {
        item["snippet_id"]: item["asset_id"] for item in prompt_input.get("grounding_context", [])
    }
    seen = set()
    normalized_ids = []

    for grounding_id in grounding_ids:
        canonical_id = snippet_to_asset.get(grounding_id, grounding_id)
        if canonical_id not in seen:
            seen.add(canonical_id)
            normalized_ids.append(canonical_id)

    normalized["grounding_asset_ids"] = normalized_ids
    return normalized


def validate_response(response_obj: dict, prompt_input: dict) -> dict:
    contract = prompt_input["response_contract"]
    required = contract["required_fields"]
    results = {}

    for field in required:
        results[f"{field}_present"] = field in response_obj

    summary = response_obj.get("summary", "")
    key_points = response_obj.get("key_points", [])
    cta_text = response_obj.get("cta_support_text", "")
    grounding_ids = response_obj.get("grounding_asset_ids", [])
    grounding_context_ids = [g["asset_id"] for g in prompt_input.get("grounding_context", [])]

    results["summary_within_length"] = len(summary.split()) <= contract["summary_max_words"]
    results["key_points_within_count"] = len(key_points) <= contract["key_points_max_count"]
    results["cta_text_within_length"] = len(cta_text.split()) <= contract["cta_support_text_max_words"]
    results["grounding_assets_cited"] = len(grounding_ids) > 0
    results["grounding_assets_valid"] = all(g in grounding_context_ids for g in grounding_ids)

    results["all_passed"] = all(v for v in results.values())
    return results


def run_scenario(scenario: str, client: OpenAI) -> dict:
    scenario_dir = SCENARIOS_DIR / scenario
    prompt_input = load_json(scenario_dir / "08-ai-prompt-input.json")
    expected_output = load_json(scenario_dir / "09-ai-expected-output.json")

    messages = [
        {"role": "system", "content": prompt_input["system_prompt"]},
        {"role": "user", "content": assemble_user_message(prompt_input)},
    ]

    print(f"  Sending to {MODEL}...", end=" ", flush=True)
    completion = client.chat.completions.create(
        model=MODEL,
        messages=messages,
        response_format={"type": "json_object"},
        temperature=0.1,
    )

    raw = completion.choices[0].message.content
    print(f"done ({completion.usage.completion_tokens} tokens, {int(completion.usage.total_tokens)} total)")

    try:
        response_obj = json.loads(raw)
    except json.JSONDecodeError as e:
        return {
            "scenario": scenario,
            "status": "ERROR",
            "error": f"Response was not valid JSON: {e}",
            "raw_response": raw[:200],
        }

    response_obj = normalize_grounding_asset_ids(response_obj, prompt_input)
    validation = validate_response(response_obj, prompt_input)

    return {
        "scenario": scenario,
        "status": "PASS" if validation["all_passed"] else "FAIL",
        "validation": validation,
        "response": response_obj,
        "expected_summary": expected_output["response"]["summary"],
        "actual_summary": response_obj.get("summary", ""),
        "latency_ms": None,
    }


def print_report(results: list[dict]) -> None:
    print("\n" + "=" * 60)
    print("EVALUATION REPORT")
    print("=" * 60)

    for r in results:
        status_icon = "✓" if r["status"] == "PASS" else "✗" if r["status"] == "FAIL" else "!"
        print(f"\n{status_icon} {r['scenario']}  [{r['status']}]")

        if r["status"] == "ERROR":
            print(f"  Error: {r['error']}")
            continue

        print(f"  Expected summary: {r['expected_summary']}")
        print(f"  Actual summary:   {r['actual_summary']}")
        print()

        for check, passed in r["validation"].items():
            if check == "all_passed":
                continue
            icon = "✓" if passed else "✗"
            print(f"  {icon}  {check}")

    passed = sum(1 for r in results if r["status"] == "PASS")
    total = len(results)
    print(f"\n{'-' * 60}")
    print(f"Result: {passed}/{total} scenarios passed")
    print("=" * 60 + "\n")


def main() -> None:
    print(f"Ollama endpoint: {OLLAMA_BASE_URL}")
    print(f"Model:           {MODEL}")
    print()

    client = OpenAI(
        base_url=f"{OLLAMA_BASE_URL}/v1",
        api_key="ollama",
    )

    # Check connectivity
    try:
        client.models.list()
    except Exception as e:
        print(f"ERROR: Cannot reach Ollama at {OLLAMA_BASE_URL}")
        print(f"  {e}")
        print()
        print("Make sure Ollama is running and the model is pulled:")
        print(f"  ./scripts/pull-model.sh {MODEL}")
        sys.exit(1)

    results = []
    for scenario in list_scenarios():
        print(f"Running: {scenario}")
        result = run_scenario(scenario, client)
        results.append(result)

    print_report(results)

    failed = [r for r in results if r["status"] != "PASS"]
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
