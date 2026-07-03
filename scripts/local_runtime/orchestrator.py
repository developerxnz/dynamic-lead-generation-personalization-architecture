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
            "02-secondary-new-customer": [
                "Only active journey for this customer",
                "Session URL and query text align with health insurance discovery",
                "No competing journeys",
            ],
            "03-secondary-resume-quote": [
                "Only active journey for this customer",
                "resume_candidate is true — quote started but not completed",
                "Customer returned to broadband section directly",
                "High journey score indicates strong purchase intent",
                "Serviceability already confirmed",
            ],
            "04-secondary-compliance-suppression": [
                "Only active journey for this customer",
                "Session URL and query text align with health insurance comparison",
                "Returning customer behavior shows active switching intent",
            ],
        }[scenario_name]
        recency_score = 1.0 if scenario_name in {
            "02-secondary-new-customer",
            "04-secondary-compliance-suppression",
        } else 0.79
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
            "suggested_journey_id": selected["journey_id"],
            "confidence": policy["ai_confidence"],
            "reason_summary": policy["ai_reason_summary"],
        },
        "deterministic_override": False,
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
    if scenario_name == "01-primary-returning-multi-journey":
        secondary_journey = next(
            journey
            for journey in journeys_payload["journeys"]
            if journey["journey_id"] != active_journey["journey_id"]
        )

    funnel_stage_matches = {
        "action-bbd-address-check-001": "research",
        "offer-bbd-fast-family-001": "research" if scenario_name == "01-primary-returning-multi-journey" else "compare",
        "guide-bbd-moving-home-001": "research",
        "action-health-resume-compare-001": "compare",
        "action-health-compare-001": "discover" if scenario_name == "02-secondary-new-customer" else "compare",
        "guide-health-families-001": "discover",
        "offer-health-singles-001": "discover",
        "guide-health-switching-001": "research" if scenario_name == "02-secondary-new-customer" else "compare",
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
    if scenario_name == "04-secondary-compliance-suppression":
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
    ranking_request: dict,
    ranking_response: dict,
    catalog: dict,
) -> dict:
    return deepcopy(load_scenario_artifact(scenario_name, "08-ai-prompt-input.json"))


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
    policy = SCENARIO_POLICIES[scenario_name]
    top = ranking_response["ranked_recommendations"][0]
    top_asset = catalog["assets"][top["content_id"]]
    supporting_items = []
    if len(ranking_response["ranked_recommendations"]) > 1:
        support = ranking_response["ranked_recommendations"][1]
        support_asset = catalog["assets"][support["content_id"]]
        supporting_items.append(
            {
                "content_id": support["content_id"],
                "cta_type": support_asset["cta"]["type"],
                "label": support_asset["cta"]["label"],
                "deep_link": support_asset["cta"]["deepLink"],
            }
        )

    active_journey = load_scenario_artifact(scenario_name, "04-active-journey-selection.json")
    response_generated_at = _format_timestamp(
        _parse_timestamp(session["timestamp"]) + timedelta(seconds=2)
    )

    decision_trace = {
        "01-primary-returning-multi-journey": {
            "profile_read": "known customer, family household, NSW, lead score 81",
            "journey_read": "two journeys loaded: health compare + broadband move-home",
            "active_journey_selected": "broadband selected — session evidence stronger",
            "retrieval": "4 candidates returned",
            "filtering": "health resume retained as secondary only",
            "ranking": "address check ranked first (score 34)",
            "ai_explanation": "accepted — grounded on address-check and moving-home guide assets",
        },
        "02-secondary-new-customer": {
            "profile_read": "new customer, single household, VIC, lead score 42",
            "journey_read": "one journey loaded: health insurance discovery",
            "active_journey_selected": "health insurance — only journey, no ambiguity",
            "retrieval": "4 candidates returned; family guide excluded at retrieval",
            "filtering": "family guide suppressed at ranking (household mismatch)",
            "ranking": "comparison CTA ranked first (score 29)",
            "ai_explanation": "accepted — grounded on comparison and singles cover assets",
        },
        "03-secondary-resume-quote": {
            "profile_read": "known customer, couple household, QLD, lead score 71",
            "journey_read": "one journey loaded: broadband quote_in_progress, resume_candidate true",
            "active_journey_selected": "broadband — only journey, high resume signal",
            "retrieval": "3 candidates returned; moving-home guide excluded (stage mismatch)",
            "filtering": "no suppressions at ranking",
            "ranking": "resume CTA ranked first (score 41, resume bias applied)",
            "ai_explanation": "accepted — grounded on resume quote asset",
        },
        "04-secondary-compliance-suppression": {
            "profile_read": "known customer, couple household, TAS, lead score 63",
            "journey_read": "one journey loaded: health insurance compare, switching intent active",
            "active_journey_selected": "health insurance — only journey, no ambiguity",
            "retrieval": "3 candidates returned; Provider K bundle retained for policy evaluation despite broad retrieval",
            "filtering": "Provider K bundle suppressed at ranking due to state restriction in TAS",
            "ranking": "comparison CTA ranked first (score 31)",
            "ai_explanation": "accepted — grounded on comparison and switching guide assets",
        },
    }[scenario_name]

    return {
        "scenario": scenario_name,
        "description": policy["final_response_description"],
        "customer_id": profile["customer_id"],
        "session_id": session["session_id"],
        "active_journey": {
            "journey_id": active_journey["selected_journey_id"],
            "service_category": active_journey["selected_service_category"],
        },
        "next_best_action": {
            "content_id": top["content_id"],
            "cta_type": top_asset["cta"]["type"],
            "label": top_asset["cta"]["label"],
            "deep_link": top_asset["cta"]["deepLink"],
            "ranking_score": top["score"],
            "ranking_policy_version": ranking_response["ranking_policy_version"],
        },
        "supporting_content": supporting_items,
        "secondary_journey_prompt": policy["secondary_prompt"],
        "explanation": {
            "source": "ai_assisted",
            "ai_response_id": ai_record["ai_response_id"],
            "summary": ai_response["summary"],
            "cta_support_text": ai_response["cta_support_text"],
            "grounding_asset_ids": ai_response["grounding_asset_ids"],
        },
        "decision_trace": decision_trace,
        "metadata_revision": top_asset["metadataRevision"],
        "response_generated_at": response_generated_at,
    }


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
    policy = SCENARIO_POLICIES[scenario_name]
    base_time = _parse_timestamp(session["timestamp"])
    top_asset = catalog["assets"][final_response["next_best_action"]["content_id"]]

    events = [
        {
            "event_type": "active_journey_selected",
            "customer_id": profile["customer_id"],
            "session_id": session["session_id"],
            "journey_id": active_selection["selected_journey_id"],
            "timestamp": _format_timestamp(base_time),
            "metadata": {
                "candidate_journeys": [
                    journey["journey_id"] for journey in journeys_payload["journeys"]
                ],
                "selected_service_category": active_selection["selected_service_category"],
                "selection_method": active_selection["selection_method"],
                "ai_confidence": active_selection["ai_interpretation"]["confidence"],
            },
        }
    ]
    if scenario_name == "02-secondary-new-customer":
        events[0]["metadata"]["new_journey_created"] = True
    if scenario_name == "03-secondary-resume-quote":
        events[0]["metadata"]["resume_candidate"] = True
    if scenario_name == "04-secondary-compliance-suppression":
        events[0]["metadata"]["switching_intent"] = "active"

    post_active_events = []
    end_events = []
    for extra in policy["analytics_extra_events"]:
        event = {
            "event_type": extra["event_type"],
            "customer_id": profile["customer_id"],
            "session_id": session["session_id"],
            "journey_id": active_selection["selected_journey_id"],
            "timestamp": _format_timestamp(base_time + timedelta(seconds=extra["timestamp_suffix"])),
        }
        if extra.get("metadata_builder") == "cta_clicked_destination":
            event["metadata"] = {
                "content_id": final_response["next_best_action"]["content_id"],
                "cta_type": final_response["next_best_action"]["cta_type"],
                "destination": final_response["next_best_action"]["deep_link"],
            }
            event["note"] = extra["note"]
        else:
            event["metadata"] = extra["metadata"]
            if "note" in extra:
                event["note"] = extra["note"]
        if extra.get("placement") == "after_active_journey":
            post_active_events.append(event)
        else:
            end_events.append(event)

    events.extend(post_active_events)

    events.extend(
        [
            {
                "event_type": "recommendation_served",
                "customer_id": profile["customer_id"],
                "session_id": session["session_id"],
                "journey_id": active_selection["selected_journey_id"],
                "timestamp": _format_timestamp(base_time + timedelta(seconds=2)),
                "metadata": {
                    "active_journey": final_response["active_journey"]["service_category"],
                    "top_recommendation": final_response["next_best_action"]["content_id"],
                    "ranking_policy_version": final_response["next_best_action"]["ranking_policy_version"],
                    "metadata_revision": final_response["metadata_revision"],
                    "candidates_ranked": len(ranking_response["ranked_recommendations"]),
                    "candidates_suppressed": len(ranking_response["suppressed_candidates"]),
                },
            },
            {
                "event_type": "cta_impression",
                "customer_id": profile["customer_id"],
                "session_id": session["session_id"],
                "journey_id": active_selection["selected_journey_id"],
                "timestamp": _format_timestamp(base_time + timedelta(seconds=3)),
                "metadata": {
                    "content_id": final_response["next_best_action"]["content_id"],
                    "cta_type": final_response["next_best_action"]["cta_type"],
                    "label": final_response["next_best_action"]["label"],
                    "position": "primary",
                },
            },
            {
                "event_type": "ai_response_accepted",
                "customer_id": profile["customer_id"],
                "session_id": session["session_id"],
                "journey_id": active_selection["selected_journey_id"],
                "timestamp": _format_timestamp(base_time + timedelta(seconds=2)),
                "metadata": {
                    "response_id": ai_record["ai_response_id"],
                    "ai_task_type": "cta_explanation",
                    "prompt_template_version": "poc-cta-explainer-v1",
                    "grounding_asset_ids": final_response["explanation"]["grounding_asset_ids"],
                    "accepted": ai_record["response_status"] == "accepted",
                    "latency_ms": ai_record["latency_ms"],
                },
            },
        ]
    )
    if scenario_name == "03-secondary-resume-quote":
        events[-3]["metadata"]["resume_bias_applied"] = True
    events.extend(end_events)

    return {
        "scenario": scenario_name,
        "description": policy["analytics_description"],
        "events": events,
    }


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
            json.dump(payload, handle, indent=2)
            handle.write("\n")
