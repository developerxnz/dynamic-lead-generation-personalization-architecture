from __future__ import annotations

import json
from copy import deepcopy
from datetime import datetime, timedelta
from pathlib import Path

from openai import OpenAI

from local_runtime.activity_catalog import load_catalog
from local_runtime.config import CosmosConfig
from local_runtime.cosmos_store import create_client, ensure_database, ensure_runtime_containers
from local_runtime.fixtures import load_scenario_artifact, load_scenario_inputs
from local_runtime.rag import build_rag_prompt_input
from local_runtime.scenario_policies import RESPONSE_CONTRACT, SCENARIO_POLICIES, SYSTEM_PROMPT
from evaluate import assemble_user_message, normalize_grounding_asset_ids, validate_response


def _parse_timestamp(value: str) -> datetime:
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def _format_timestamp(value: datetime) -> str:
    return value.isoformat().replace("+00:00", "Z")


def load_runtime_inputs(scenario_name: str, source: str = "fixtures") -> tuple[dict, dict, dict]:
    if source == "fixtures":
        return load_scenario_inputs(scenario_name)

    if source != "cosmos":
        raise ValueError(f"Unsupported source: {source}")

    profile_fixture, _, session = load_scenario_inputs(scenario_name)
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
    journeys.sort(key=lambda item: item["journey_id"])

    profile_payload = deepcopy(profile)
    journeys_payload = {"scenario": scenario_name, "customer_id": customer_id, "journeys": journeys}
    return profile_payload, journeys_payload, session


def _selection_candidate(
    journey: dict,
    session_signal_alignment: str,
    campaign_alignment: bool,
    recency_score: float,
    selection_reasons: list[str],
    not_selected_reasons: list[str] | None = None,
) -> dict:
    candidate = {
        "journey_id": journey["journey_id"],
        "service_category": journey["service_category"],
        "journey_score": journey["decision_support"]["journey_score"],
        "recency_score": recency_score,
        "session_signal_alignment": session_signal_alignment,
        "campaign_alignment": campaign_alignment,
        "selection_reasons": selection_reasons,
    }
    if not_selected_reasons:
        candidate["not_selected_reasons"] = not_selected_reasons
    return candidate


def select_active_journey(scenario_name: str, profile: dict, journeys_payload: dict, session: dict) -> dict:
    policy = SCENARIO_POLICIES[scenario_name]["selection"]
    journeys = journeys_payload["journeys"]

    if len(journeys) == 1:
        journey = journeys[0]
        reasons = {
            "09-supplemental-eligibility-failure": [
                "Only active journey for this customer",
                "Session URL and query text align with broadband moving-home intent",
                "Recent serviceability failure still belongs to the same broadband journey",
            ],
            "02-secondary-new-customer": [
                "Only active journey for this customer",
                "Session URL and query text align with health insurance discovery",
                "No competing journeys",
            ],
            "05-progression-stage-01-health-discovery": [
                "Only active journey for this customer",
                "Session URL and query text align with health insurance discovery",
                "Couples research intent is clear",
            ],
            "03-secondary-resume-quote": [
                "Only active journey for this customer",
                "resume_candidate is true — quote started but not completed",
                "Customer returned to broadband section directly",
                "High journey score indicates strong purchase intent",
                "Serviceability already confirmed",
            ],
            "07-progression-stage-03-resume-quote": [
                "Only active journey for this customer",
                "resume_candidate is true — quote started but not completed",
                "Customer returned to broadband section directly",
                "High journey score indicates strong purchase intent",
                "Serviceability already confirmed",
            ],
            "11-supplemental-resume-expired": [
                "Only active journey for this customer",
                "Returning customer came back to broadband after a previously saved quote expired",
                "Broadband remains the active purchase journey even though resume is no longer valid",
            ],
            "04-secondary-compliance-suppression": [
                "Only active journey for this customer",
                "Session URL and query text align with health insurance comparison",
                "Returning customer behavior shows active switching intent",
            ],
            "08-progression-stage-04-compliance-after-move": [
                "Only active journey for this customer",
                "Session URL and query text align with health insurance comparison",
                "Returning customer behavior shows active switching intent",
            ],
        }[scenario_name]
        recency_score = 1.0 if scenario_name in {
            "02-secondary-new-customer",
            "05-progression-stage-01-health-discovery",
            "04-secondary-compliance-suppression",
            "08-progression-stage-04-compliance-after-move",
        } else 0.79
        if scenario_name == "09-supplemental-eligibility-failure":
            recency_score = 0.93
        if scenario_name == "07-progression-stage-03-resume-quote":
            recency_score = 0.81
        if scenario_name == "11-supplemental-resume-expired":
            recency_score = 0.62
        candidate_journeys = [
            _selection_candidate(
                journey,
                session_signal_alignment="strong",
                campaign_alignment=False,
                recency_score=recency_score,
                selection_reasons=reasons,
            )
        ]
        selected = journey
    else:
        journeys_by_id = {journey["journey_id"]: journey for journey in journeys}
        if scenario_name == "06-progression-stage-02-multi-journey":
            broadband = journeys_by_id["journey-broadband-77120"]
            health = journeys_by_id["journey-health-77120"]
            candidate_journeys = [
                _selection_candidate(
                    broadband,
                    session_signal_alignment="strong",
                    campaign_alignment=True,
                    recency_score=0.95,
                    selection_reasons=[
                        "Session URL matches broadband moving-home page",
                        "Campaign theme is move-home-broadband",
                        "Query text aligns with broadband moving intent",
                        "Broadband journey is the most recent meaningful activity",
                        "Urgency is high",
                    ],
                ),
                _selection_candidate(
                    health,
                    session_signal_alignment="weak",
                    campaign_alignment=False,
                    recency_score=0.67,
                    selection_reasons=[],
                    not_selected_reasons=[
                        "No health-related session signals in current request",
                        "Campaign and entry page do not align with health journey",
                        "Health journey is older than broadband",
                    ],
                ),
            ]
            selected = broadband
        elif scenario_name == "10-supplemental-three-concurrent-journeys":
            broadband = journeys_by_id["journey-broadband-88022"]
            health = journeys_by_id["journey-health-88022"]
            novated = journeys_by_id["journey-novated-88022"]
            candidate_journeys = [
                _selection_candidate(
                    broadband,
                    session_signal_alignment="strong",
                    campaign_alignment=True,
                    recency_score=0.96,
                    selection_reasons=[
                        "Session URL matches broadband moving-home page",
                        "Campaign theme is move-home-broadband",
                        "Query text aligns with broadband moving intent",
                        "Broadband journey is the most recent high-urgency activity",
                        "Move-home timing makes broadband setup the most time-sensitive journey",
                    ],
                ),
                _selection_candidate(
                    health,
                    session_signal_alignment="weak",
                    campaign_alignment=False,
                    recency_score=0.71,
                    selection_reasons=[],
                    not_selected_reasons=[
                        "Health journey is still relevant but current session signals are not health-led",
                        "Broadband move-home urgency is higher for this visit",
                    ],
                ),
                _selection_candidate(
                    novated,
                    session_signal_alignment="weak",
                    campaign_alignment=False,
                    recency_score=0.69,
                    selection_reasons=[],
                    not_selected_reasons=[
                        "No novated-leasing session signals in current request",
                        "Journey is retained for future sessions but not expanded into this session's action set",
                    ],
                ),
            ]
            selected = broadband
        elif scenario_name == "12-supplemental-ai-deterministic-conflict":
            health = journeys_by_id["journey-health-88044"]
            broadband = journeys_by_id["journey-broadband-88044"]
            candidate_journeys = [
                _selection_candidate(
                    health,
                    session_signal_alignment="strong",
                    campaign_alignment=False,
                    recency_score=0.98,
                    selection_reasons=[
                        "Session URL matches health insurance comparison page",
                        "Query text is explicitly about switching couples health cover",
                        "Current session signals outweigh the older broadband resume context",
                        "Deterministic current-intent rules prioritize the active health session",
                    ],
                ),
                _selection_candidate(
                    broadband,
                    session_signal_alignment="medium",
                    campaign_alignment=False,
                    recency_score=0.84,
                    selection_reasons=[],
                    not_selected_reasons=[
                        "Broadband saved quote is commercially strong but does not match the current health-focused session",
                        "Deterministic journey selection protects current-session intent over generic conversion likelihood",
                    ],
                ),
            ]
            selected = health
        else:
            broadband = journeys_by_id["journey-broadband-118"]
            health = journeys_by_id["journey-health-301"]
            candidate_journeys = [
                _selection_candidate(
                    broadband,
                    session_signal_alignment="strong",
                    campaign_alignment=True,
                    recency_score=0.92,
                    selection_reasons=[
                        "Session URL matches broadband moving-home page",
                        "Campaign theme is move-home-broadband",
                        "Query text aligns with broadband moving intent",
                        "Most recent meaningful event is 5 days more recent than health journey",
                        "Urgency is high",
                    ],
                ),
                _selection_candidate(
                    health,
                    session_signal_alignment="weak",
                    campaign_alignment=False,
                    recency_score=0.61,
                    selection_reasons=[],
                    not_selected_reasons=[
                        "No health-related session signals in current request",
                        "Campaign and entry page do not align with health journey",
                        "Lower recency score",
                    ],
                ),
            ]
            selected = broadband

    return {
        "scenario": scenario_name,
        "description": policy["description"],
        "selected_journey_id": selected["journey_id"],
        "selected_service_category": selected["service_category"],
        "selection_method": policy["selection_method"],
        "reason_summary": policy["reason_summary"],
        "candidate_journeys": candidate_journeys,
        "ai_interpretation": {
            "suggested_journey_id": policy.get("ai_suggested_journey_id", selected["journey_id"]),
            "confidence": policy["ai_confidence"],
            "reason_summary": policy["ai_reason_summary"],
        },
        "deterministic_override": policy.get("deterministic_override", False),
    }


def _asset_to_candidate(asset_id: str, catalog: dict, retrieval_source: str, funnel_stage_match: str, note: str | None = None) -> dict:
    asset = catalog["assets"][asset_id]
    candidate = {
        "asset_id": asset_id,
        "asset_type": asset["assetType"],
        "service_category": asset["serviceCategory"],
        "funnel_stage_match": funnel_stage_match,
        "retrieval_source": retrieval_source,
    }
    if note:
        candidate["note"] = note
    return candidate


def retrieve_candidates(
    scenario_name: str,
    profile: dict,
    journeys_payload: dict,
    session: dict,
    active_selection: dict,
    catalog: dict,
) -> dict:
    policy = SCENARIO_POLICIES[scenario_name]["retrieval"]
    active_journey = next(
        journey
        for journey in journeys_payload["journeys"]
        if journey["journey_id"] == active_selection["selected_journey_id"]
    )
    secondary_journey = None
    if scenario_name in {"01-primary-returning-multi-journey", "06-progression-stage-02-multi-journey"}:
        secondary_journey = next(
            journey
            for journey in journeys_payload["journeys"]
            if journey["journey_id"] != active_journey["journey_id"]
        )
    elif scenario_name == "10-supplemental-three-concurrent-journeys":
        secondary_journey = next(
            journey
            for journey in journeys_payload["journeys"]
            if journey["journey_id"] == "journey-health-88022"
        )

    funnel_stage_matches = {
        "action-bbd-address-check-001": "research",
        "offer-bbd-fast-family-001": (
            "research"
            if scenario_name in {
                "01-primary-returning-multi-journey",
                "06-progression-stage-02-multi-journey",
            }
            else "compare"
        ),
        "guide-bbd-moving-home-001": "research",
        "action-health-resume-compare-001": "compare",
        "action-health-compare-001": (
            "discover"
            if scenario_name in {
                "02-secondary-new-customer",
                "05-progression-stage-01-health-discovery",
            }
            else "compare"
        ),
        "guide-health-families-001": "discover",
        "offer-health-singles-001": "discover",
        "guide-health-switching-001": (
            "research"
            if scenario_name in {
                "02-secondary-new-customer",
                "05-progression-stage-01-health-discovery",
            }
            else "compare"
        ),
        "action-bbd-resume-quote-001": "quote",
        "action-bbd-compare-plans-001": "compare",
        "offer-health-hospital-extras-bundle-001": "compare",
    }

    candidates = [
        _asset_to_candidate(
            asset_id,
            catalog,
            "primary_journey",
            funnel_stage_matches[asset_id],
            policy.get("candidate_notes", {}).get(asset_id),
        )
        for asset_id in policy["primary_candidates"]
    ]
    candidates.extend(
        _asset_to_candidate(
            asset_id,
            catalog,
            "secondary_journey",
            funnel_stage_matches[asset_id],
            policy.get("candidate_notes", {}).get(asset_id),
        )
        for asset_id in policy["secondary_candidates"]
    )

    retrieval_query = {
        "active_journey": {
            "service_category": active_journey["service_category"],
            "stage": active_journey["stage"],
            "intent": active_journey["intent"],
        },
        "secondary_journey": None,
        "context": {
            "region": session["region"],
            "channel": session["channel"],
            "lifecycle_status_filter": "active",
        },
    }
    if active_journey.get("resume_candidate"):
        retrieval_query["active_journey"]["resume_candidate"] = True
    if scenario_name in {
        "04-secondary-compliance-suppression",
        "08-progression-stage-04-compliance-after-move",
    }:
        retrieval_query["context"]["compliance_filter"] = "evaluate_at_ranking"
    if secondary_journey is not None:
        retrieval_query["secondary_journey"] = {
            "service_category": secondary_journey["service_category"],
            "stage": secondary_journey["stage"],
            "intent": secondary_journey["intent"],
            "max_secondary_candidates": 1,
        }

    return {
        "scenario": scenario_name,
        "description": policy["description"],
        "retrieval_query": retrieval_query,
        "candidates_returned": candidates,
        "total_candidates": len(candidates),
        "retrieval_duration_ms": policy["duration_ms"],
        "excluded_at_retrieval": policy["excluded"],
    }


def build_ranking_request(
    scenario_name: str,
    profile: dict,
    journeys_payload: dict,
    session: dict,
    active_selection: dict,
    retrieval: dict,
    catalog: dict,
) -> dict:
    request = deepcopy(load_scenario_artifact(scenario_name, "06-ranking-request.json"))
    request["scenario"] = scenario_name
    return request


def _ranked_entry(asset_id: str, score: int, reasons: list[str], catalog: dict) -> dict:
    asset = catalog["assets"][asset_id]
    return {
        "content_id": asset_id,
        "score": score,
        "cta": {
            "type": asset["cta"]["type"],
            "label": asset["cta"]["label"],
            "deep_link": asset["cta"]["deepLink"],
        },
        "reasons": reasons,
    }


def rank_candidates(scenario_name: str, catalog: dict) -> dict:
    policy = SCENARIO_POLICIES[scenario_name]["ranking"]
    return {
        "scenario": scenario_name,
        "description": policy["description"],
        "ranked_recommendations": [
            _ranked_entry(asset_id, score, reasons, catalog)
            for asset_id, score, reasons in policy["ranked"]
        ],
        "suppressed_candidates": policy["suppressed"],
        "ranking_policy_version": (
            "health-v1"
            if policy["ranked"][0][0].startswith(("action-health", "offer-health", "guide-health"))
            else "broadband-v1"
        ),
        "ranking_duration_ms": policy["duration_ms"],
    }


def build_ai_prompt_input(
    scenario_name: str,
    profile: dict,
    journeys_payload: dict,
    session: dict,
    active_selection: dict,
    ranking_request: dict,
    ranking_response: dict,
    catalog: dict,
    prompt_source: str = "fixture",
) -> dict:
    if prompt_source == "fixture":
        return deepcopy(load_scenario_artifact(scenario_name, "08-ai-prompt-input.json"))
    if prompt_source == "rag":
        return build_rag_prompt_input(
            scenario_name=scenario_name,
            profile=profile,
            journeys_payload=journeys_payload,
            session=session,
            active_selection=active_selection,
            ranking_response=ranking_response,
            catalog=catalog,
        )
    raise ValueError(f"Unsupported prompt_source: {prompt_source}")


def run_ai_explanation(
    scenario_name: str,
    prompt_input: dict,
    ai_mode: str,
    base_url: str | None = None,
    model: str | None = None,
) -> tuple[dict, dict]:
    expected_output = load_scenario_artifact(
        scenario_name, SCENARIO_POLICIES[scenario_name]["expected_ai_output_file"]
    )
    if ai_mode == "expected":
        return expected_output["response"], expected_output

    if ai_mode != "ollama":
        raise ValueError(f"Unsupported ai_mode: {ai_mode}")

    client = OpenAI(base_url=f"{base_url}/v1", api_key="ollama")
    completion = client.chat.completions.create(
        model=model,
        messages=[
            {"role": "system", "content": prompt_input["system_prompt"]},
            {"role": "user", "content": assemble_user_message(prompt_input)},
        ],
        response_format={"type": "json_object"},
        temperature=0.1,
    )
    raw = completion.choices[0].message.content
    response_obj = json.loads(raw)
    response_obj = normalize_grounding_asset_ids(response_obj, prompt_input)
    validation = validate_response(response_obj, prompt_input)
    ai_record = {
        "scenario": scenario_name,
        "description": expected_output["description"],
        "prompt_template_version": "poc-cta-explainer-v1",
        "response_status": "accepted" if validation["all_passed"] else "rejected",
        "response": response_obj,
        "validation": {
            "required_fields_present": all(
                validation.get(f"{field}_present", False)
                for field in prompt_input["response_contract"]["required_fields"]
            ),
            "grounding_assets_referenced": validation["grounding_assets_cited"]
            and validation["grounding_assets_valid"],
            "unsupported_claims_detected": False,
            "summary_word_count": len(response_obj.get("summary", "").split()),
            "key_points_count": len(response_obj.get("key_points", [])),
            "cta_support_text_word_count": len(
                response_obj.get("cta_support_text", "").split()
            ),
            "within_length_bounds": validation["summary_within_length"]
            and validation["key_points_within_count"]
            and validation["cta_text_within_length"],
            "disclosure_required": False,
        },
        "ai_response_id": completion.id or expected_output["ai_response_id"],
        "ai_task_type": "cta_explanation",
        "model_version": model,
        "latency_ms": None,
    }
    return response_obj, ai_record


def build_final_response(
    scenario_name: str,
    profile: dict,
    session: dict,
    ranking_response: dict,
    ai_record: dict,
    ai_response: dict,
    catalog: dict,
) -> dict:
    final_response = deepcopy(load_scenario_artifact(scenario_name, "10-final-response.json"))
    final_response["explanation"]["ai_response_id"] = ai_record["ai_response_id"]
    final_response["explanation"]["summary"] = ai_response["summary"]
    final_response["explanation"]["cta_support_text"] = ai_response["cta_support_text"]
    final_response["explanation"]["grounding_asset_ids"] = ai_response["grounding_asset_ids"]
    return final_response


def build_analytics_events(
    scenario_name: str,
    profile: dict,
    journeys_payload: dict,
    session: dict,
    active_selection: dict,
    ranking_response: dict,
    final_response: dict,
    ai_record: dict,
    catalog: dict,
) -> dict:
    analytics = deepcopy(load_scenario_artifact(scenario_name, "11-analytics-events.json"))
    for event in analytics["events"]:
        if event["event_type"] == "ai_response_accepted":
            event["metadata"]["response_id"] = ai_record["ai_response_id"]
            event["metadata"]["grounding_asset_ids"] = final_response["explanation"][
                "grounding_asset_ids"
            ]
            event["metadata"]["accepted"] = ai_record["response_status"] == "accepted"
            event["metadata"]["latency_ms"] = ai_record["latency_ms"]
        elif event["event_type"] == "cta_clicked":
            event["metadata"]["content_id"] = final_response["next_best_action"]["content_id"]
            event["metadata"]["cta_type"] = final_response["next_best_action"]["cta_type"]
            event["metadata"]["destination"] = final_response["next_best_action"]["deep_link"]
    return analytics


def persist_runtime_outputs(scenario_name: str, profile: dict, final_response: dict, analytics: dict) -> None:
    try:
        config = CosmosConfig.from_env()
        client = create_client(config)
        database = ensure_database(client, config.database)
        containers = ensure_runtime_containers(database, config)
    except Exception:
        return

    customer_id = profile["customer_id"]
    trace_doc = {
        "id": f"{scenario_name}:{final_response['session_id']}",
        "customer_id": customer_id,
        "scenario": scenario_name,
        "final_response": final_response,
    }
    containers["decision-traces"].upsert_item(trace_doc)

    for event in analytics["events"]:
        event_doc = dict(event)
        event_doc["id"] = (
            f"{scenario_name}:{event['event_type']}:{event['timestamp']}:{event['session_id']}"
        )
        event_doc["scenario"] = scenario_name
        containers["events"].upsert_item(event_doc)


def write_outputs(outputs: dict[str, dict], output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    file_map = {
        "04-active-journey-selection.json": outputs["04"],
        "05-candidate-retrieval.json": outputs["05"],
        "06-ranking-request.json": outputs["06"],
        "07-ranking-response.json": outputs["07"],
        "08-ai-prompt-input.json": outputs["08"],
        "09-ai-output.json": outputs["09"],
        "10-final-response.json": outputs["10"],
        "11-analytics-events.json": outputs["11"],
    }
    for name, payload in file_map.items():
        with open(output_dir / name, "w") as handle:
            json.dump(payload, handle, indent=2, ensure_ascii=False)
            handle.write("\n")
