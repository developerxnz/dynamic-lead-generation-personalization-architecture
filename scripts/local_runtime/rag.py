from __future__ import annotations

import re
from copy import deepcopy

from local_runtime.fixtures import load_scenario_artifact

STOPWORDS = {
    "a",
    "an",
    "and",
    "are",
    "at",
    "be",
    "by",
    "for",
    "from",
    "how",
    "in",
    "is",
    "it",
    "of",
    "on",
    "or",
    "the",
    "this",
    "to",
    "up",
    "we",
    "with",
    "your",
}

STAGE_EQUIVALENTS = {
    "discover": {"discover", "research"},
    "research": {"discover", "research", "compare"},
    "compare": {"research", "compare", "quote"},
    "quote": {"compare", "quote"},
    "quote_in_progress": {"quote"},
}


def _tokenize(*parts: str | None) -> set[str]:
    tokens: set[str] = set()
    for part in parts:
        if not part:
            continue
        for token in re.findall(r"[a-z0-9]+", part.lower().replace("_", " ")):
            if len(token) > 1 and token not in STOPWORDS:
                tokens.add(token)
    return tokens


def _stage_matches(asset: dict, stage: str) -> bool:
    equivalents = STAGE_EQUIVALENTS.get(stage, {stage})
    return any(candidate in equivalents for candidate in asset.get("funnelStages", []))


def _region_matches(asset: dict, region: str | None) -> bool:
    regions = asset.get("region") or []
    return region is None or region in regions


def _household_matches(asset: dict, household_type: str | None) -> bool:
    if not household_type:
        return False
    fit = asset.get("serviceSpecific", {}).get("householdFit", [])
    return household_type in fit


def _query_text(session: dict, active_journey: dict, selected_action: dict) -> str:
    parts = [
        session.get("query_text"),
        session.get("current_url"),
        session.get("campaign_theme"),
        active_journey.get("intent"),
        active_journey.get("stage"),
        selected_action.get("cta_label"),
        selected_action.get("content_id"),
    ]
    return " ".join(part for part in parts if part)


def _snippet_tokens(snippet: dict, linked_assets: list[dict]) -> set[str]:
    fields: list[str | None] = [
        snippet.get("content"),
        " ".join(snippet.get("tags", [])),
    ]
    for asset in linked_assets:
        ai_fields = asset.get("aiSupportFields", {})
        fields.extend(
            [
                asset.get("retrievalSummary"),
                ai_fields.get("plainLanguageSummary"),
                ai_fields.get("approvedExplainerText"),
                " ".join(ai_fields.get("retrievalTags", [])),
                asset.get("subtype"),
                asset.get("conversionGoal"),
            ]
        )
    return _tokenize(*fields)


def _best_asset_id(
    linked_assets: list[dict],
    selected_action_id: str,
    ranked_asset_ids: list[str],
) -> str:
    if linked_assets[0]["assetId"] == selected_action_id:
        return linked_assets[0]["assetId"]
    if len(linked_assets) > 1:
        return linked_assets[0]["assetId"]
    for ranked_asset_id in ranked_asset_ids:
        for asset in linked_assets:
            if asset["assetId"] == ranked_asset_id:
                return asset["assetId"]
    return linked_assets[0]["assetId"]


def _selected_action_snippet_rank(entry: dict, selected_action_id: str) -> tuple[int, int, int, str]:
    linked_asset_ids = entry["snippet"].get("linkedAssets", [])
    exact_primary_link = 0 if linked_asset_ids and linked_asset_ids[0] == selected_action_id else 1
    exclusive_link = 0 if linked_asset_ids == [selected_action_id] else 1
    return (
        exact_primary_link,
        exclusive_link,
        -entry["score"],
        entry["snippet"]["snippetId"],
    )


def _score_snippet(
    snippet: dict,
    linked_assets: list[dict],
    query_tokens: set[str],
    selected_action_id: str,
    ranked_asset_ids: list[str],
    active_journey: dict,
    profile: dict,
    region: str | None,
) -> tuple[int, list[str]]:
    score = 0
    reasons: list[str] = []
    linked_asset_ids = [asset["assetId"] for asset in linked_assets]

    if selected_action_id in linked_asset_ids:
        score += 100
        reasons.append("Linked to selected action")

    for index, ranked_asset_id in enumerate(ranked_asset_ids[:3]):
        if ranked_asset_id in linked_asset_ids:
            weight = 36 - (index * 8)
            score += weight
            reasons.append(f"Linked to ranked candidate #{index + 1}")

    if any(_stage_matches(asset, active_journey["stage"]) for asset in linked_assets):
        score += 12
        reasons.append("Matches active journey stage")

    if any(_region_matches(asset, region) for asset in linked_assets):
        score += 8
        reasons.append("Available in session region")

    household_type = profile.get("profile", {}).get("household_type")
    if any(_household_matches(asset, household_type) for asset in linked_assets):
        score += 8
        reasons.append("Fits household type")

    overlap = query_tokens & _snippet_tokens(snippet, linked_assets)
    if overlap:
        overlap_score = min(len(overlap) * 5, 40)
        score += overlap_score
        reasons.append(f"Keyword overlap: {', '.join(sorted(overlap))}")

    return score, reasons


def _active_journey(journeys_payload: dict, active_selection: dict) -> dict:
    selected_journey_id = active_selection["selected_journey_id"]
    for journey in journeys_payload["journeys"]:
        if journey["journey_id"] == selected_journey_id:
            return journey
    raise KeyError(f"Selected journey not found: {selected_journey_id}")


def _selected_action(ranking_response: dict) -> dict:
    top_recommendation = ranking_response["ranked_recommendations"][0]
    return {
        "content_id": top_recommendation["content_id"],
        "cta_type": top_recommendation["cta"]["type"],
        "cta_label": top_recommendation["cta"]["label"],
        "cta_deep_link": top_recommendation["cta"]["deep_link"],
    }


def _build_grounding_context(
    profile: dict,
    session: dict,
    active_journey: dict,
    selected_action: dict,
    ranking_response: dict,
    catalog: dict,
    max_chunks: int = 2,
) -> tuple[list[dict], dict]:
    ranked_asset_ids = [
        recommendation["content_id"]
        for recommendation in ranking_response.get("ranked_recommendations", [])
    ]
    selected_action_id = selected_action["content_id"]
    query_tokens = _tokenize(_query_text(session, active_journey, selected_action))
    region = session.get("region")
    service_category = active_journey["service_category"]

    scored: list[dict] = []
    for snippet in catalog["snippets"].values():
        if snippet["serviceCategory"] != service_category:
            continue

        linked_assets = [
            catalog["assets"][asset_id]
            for asset_id in snippet.get("linkedAssets", [])
            if asset_id in catalog["assets"]
        ]
        if not linked_assets:
            continue

        if not any(_region_matches(asset, region) for asset in linked_assets):
            continue

        score, reasons = _score_snippet(
            snippet=snippet,
            linked_assets=linked_assets,
            query_tokens=query_tokens,
            selected_action_id=selected_action_id,
            ranked_asset_ids=ranked_asset_ids,
            active_journey=active_journey,
            profile=profile,
            region=region,
        )

        scored.append(
            {
                "snippet": snippet,
                "linked_assets": linked_assets,
                "score": score,
                "reasons": reasons,
                "asset_id": _best_asset_id(linked_assets, selected_action_id, ranked_asset_ids),
            }
        )

    selected_entries = [
        entry
        for entry in scored
        if entry["asset_id"] == selected_action_id
        or selected_action_id in entry["snippet"].get("linkedAssets", [])
    ]
    selected_entries.sort(
        key=lambda entry: _selected_action_snippet_rank(entry, selected_action_id)
    )

    chosen: list[dict] = []
    seen_snippet_ids: set[str] = set()

    if selected_entries:
        first = selected_entries[0]
        chosen.append(first)
        seen_snippet_ids.add(first["snippet"]["snippetId"])

    for entry in sorted(scored, key=lambda item: (-item["score"], item["snippet"]["snippetId"])):
        snippet_id = entry["snippet"]["snippetId"]
        if snippet_id in seen_snippet_ids:
            continue
        chosen.append(entry)
        seen_snippet_ids.add(snippet_id)
        if len(chosen) >= max_chunks:
            break

    grounding_context = [
        {
            "snippet_id": entry["snippet"]["snippetId"],
            "asset_id": entry["asset_id"],
            "content": entry["snippet"]["content"],
        }
        for entry in chosen[:max_chunks]
    ]

    retrieval_debug = {
        "query_text": _query_text(session, active_journey, selected_action),
        "query_tokens": sorted(query_tokens),
        "results": [
            {
                "snippet_id": entry["snippet"]["snippetId"],
                "asset_id": entry["asset_id"],
                "score": entry["score"],
                "metadata_revision": catalog["assets"][entry["asset_id"]]["metadataRevision"],
                "reasons": entry["reasons"],
            }
            for entry in chosen[:max_chunks]
        ],
    }
    return grounding_context, retrieval_debug


def build_rag_prompt_input(
    scenario_name: str,
    profile: dict,
    journeys_payload: dict,
    session: dict,
    active_selection: dict,
    ranking_response: dict,
    catalog: dict,
) -> dict:
    prompt_input = deepcopy(load_scenario_artifact(scenario_name, "08-ai-prompt-input.json"))
    active_journey = _active_journey(journeys_payload, active_selection)
    selected_action = _selected_action(ranking_response)
    grounding_context, retrieval_debug = _build_grounding_context(
        profile=profile,
        session=session,
        active_journey=active_journey,
        selected_action=selected_action,
        ranking_response=ranking_response,
        catalog=catalog,
    )

    prompt_input["journey_context"] = {
        "journey_id": active_journey["journey_id"],
        "service_category": active_journey["service_category"],
        "intent": active_journey["intent"],
        "stage": active_journey["stage"],
        "resume_candidate": active_journey["resume_candidate"],
        "qualification_state": {
            "coverage_region_match": active_journey["qualification_state"]["coverage_region_match"],
            "serviceability_confirmed": active_journey["qualification_state"][
                "serviceability_confirmed"
            ],
        },
        "behavior_summary": deepcopy(active_journey["behavior_summary"]),
        "journey_score": active_journey["decision_support"]["journey_score"],
        "last_meaningful_event_at": active_journey["last_meaningful_event_at"],
    }
    prompt_input["customer_context"] = {
        "household_type": profile["profile"]["household_type"],
        "location": profile["profile"]["location"],
        "is_returning_customer": profile["customer_summary"]["is_returning_customer"],
    }
    prompt_input["selected_action"] = selected_action
    prompt_input["grounding_context"] = grounding_context
    prompt_input["grounding_retrieval"] = retrieval_debug
    return prompt_input
