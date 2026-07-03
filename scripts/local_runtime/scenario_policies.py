from __future__ import annotations

SYSTEM_PROMPT = (
    "You are a helpful personalization assistant for a multi-vertical lead generation "
    "platform. Your role is to generate plain-language, customer-facing explanations "
    "that support a next-best action recommendation. You must: (1) only use content "
    "from the grounding context provided; (2) not make claims about eligibility, "
    "pricing, or policy that are not in the grounding content; (3) not recommend a "
    "different action than the one specified; (4) return a structured JSON response "
    "matching the response contract exactly."
)

RESPONSE_CONTRACT = {
    "required_fields": ["summary", "key_points", "cta_support_text", "grounding_asset_ids"],
    "summary_max_words": 40,
    "key_points_max_count": 3,
    "cta_support_text_max_words": 25,
}

SCENARIO_POLICIES = {
    "01-primary-returning-multi-journey": {
        "selection": {
            "description": "Result of the active-journey selection step. Broadband is selected because current session signals are stronger than the older health comparison journey.",
            "selection_method": "deterministic_with_ai_support",
            "reason_summary": "Broadband move-home signals are more current than the older health comparison journey. Session entry point, campaign theme, current URL, and query text all point to a broadband moving-home intent.",
            "ai_confidence": 0.88,
            "ai_reason_summary": "Session context strongly suggests a move-home broadband intent. Health comparison is a likely secondary interest but is not the current driver.",
        },
        "retrieval": {
            "description": "Candidate retrieval query and results for the active broadband moving-home journey, plus limited secondary support for the health comparison journey.",
            "primary_candidates": [
                "action-bbd-address-check-001",
                "offer-bbd-fast-family-001",
                "guide-bbd-moving-home-001",
            ],
            "secondary_candidates": ["action-health-resume-compare-001"],
            "excluded": [
                {
                    "asset_id": "offer-bbd-fibre-premium-001",
                    "reason": "state_restricted_nsw_vic_qld — included for NSW but de-prioritised: stage mismatch (quote stage, customer is in research)",
                }
            ],
            "duration_ms": 18,
        },
        "ranking": {
            "description": "Ranking engine response. Address check ranks first because it is the essential serviceability step for a customer who has not yet confirmed what is available at their new address.",
            "ranked": [
                (
                    "action-bbd-address-check-001",
                    34,
                    [
                        "Active journey fit: broadband moving-home journey",
                        "Intent alignment: moving_home — serviceability check is the required first step",
                        "Serviceability not yet confirmed: address check must precede plan comparison",
                        "CTA alignment: check_eligibility",
                        "High urgency signal from active journey",
                        "Campaign alignment: move-home-broadband",
                        "Priority score: 1",
                    ],
                ),
                (
                    "offer-bbd-fast-family-001",
                    28,
                    [
                        "Active journey fit: broadband",
                        "Behavioral relevance: family household profile",
                        "Funnel stage match: research",
                        "Intent support: moving home customers benefit from seeing plan options early",
                    ],
                ),
                (
                    "guide-bbd-moving-home-001",
                    19,
                    [
                        "Active journey fit: broadband moving-home journey",
                        "Funnel stage match: research",
                        "Supports informed decision before address check",
                    ],
                ),
            ],
            "suppressed": [
                {
                    "content_id": "action-health-resume-compare-001",
                    "reason": "secondary_journey_not_primary — health comparison journey is retained as secondary prompt only, not in main ranked set",
                }
            ],
            "duration_ms": 11,
        },
        "prompt": {
            "description": "Context package assembled for the AI model. Task is to generate a plain-language CTA explanation for the selected next-best action.",
            "task_prompt": "Generate a short, helpful explanation for why this customer should check broadband availability at their new address. The explanation should feel personal and relevant to someone who is moving home. It should support the CTA without pressuring or overpromising.",
            "grounding_snippets": ["gs-bbd-address-check-001", "gs-bbd-moving-home-001"],
        },
        "final_response_description": "Final orchestrated response returned to the web channel. Contains the next-best action, supporting content, secondary journey prompt, and AI-assisted explanation.",
        "secondary_prompt": {
            "journey_id": "journey-health-301",
            "service_category": "health_insurance",
            "label": "Resume your health cover comparison",
            "deep_link": "/health-insurance/compare/resume",
            "content_id": "action-health-resume-compare-001",
        },
        "analytics_description": "Expected analytics events emitted by the platform during this session flow.",
        "analytics_extra_events": [
            {
                "event_type": "cta_clicked",
                "timestamp_suffix": 45,
                "placement": "end",
                "metadata_builder": "cta_clicked_destination",
                "note": "This event is expected to fire when the customer taps the CTA. Include in demo trace to show the full funnel.",
            }
        ],
        "expected_ai_output_file": "09-ai-expected-output.json",
    },
    "02-secondary-new-customer": {
        "selection": {
            "description": "Active-journey selection result. Only one journey exists. No ambiguity.",
            "selection_method": "deterministic_single_journey",
            "reason_summary": "Only one journey exists for this customer. Health insurance journey is the clear active path.",
            "ai_confidence": 0.95,
            "ai_reason_summary": "Single journey and clear health insurance discovery intent. No ambiguity.",
        },
        "retrieval": {
            "description": "Candidate retrieval for a new customer in health insurance discovery. Broad set covering discover and research stages.",
            "primary_candidates": [
                "action-health-compare-001",
                "guide-health-families-001",
                "offer-health-singles-001",
                "guide-health-switching-001",
            ],
            "secondary_candidates": [],
            "candidate_notes": {
                "guide-health-families-001": "Included but expected to be suppressed at ranking because household type is single, not family"
            },
            "excluded": [
                {
                    "asset_id": "offer-health-family-001",
                    "reason": "household_fit_mismatch — customer profile is single, family cover not retrieved",
                }
            ],
            "duration_ms": 14,
        },
        "ranking": {
            "description": "Ranking response. Comparison CTA ranks first as the most appropriate action for a new customer in discovery. Singles offer ranks second. Family guide is suppressed as household mismatch.",
            "ranked": [
                (
                    "action-health-compare-001",
                    29,
                    [
                        "Active journey fit: health insurance discovery journey",
                        "Funnel stage match: discover — comparison is the right next step",
                        "Intent alignment: researching_options",
                        "Priority score: 1",
                        "No prior comparison behavior — first-time customer benefits from guided comparison",
                    ],
                ),
                (
                    "offer-health-singles-001",
                    22,
                    [
                        "Household fit: customer profile is single — singles cover is appropriate",
                        "Service category match: health_insurance",
                        "Intent support: discovery-stage customers benefit from seeing a relevant offer",
                    ],
                ),
                (
                    "guide-health-switching-001",
                    11,
                    [
                        "Service category match",
                        "Useful supporting content for a customer in research mode",
                        "Lower priority as switching guide is less relevant for a first-time buyer",
                    ],
                ),
            ],
            "suppressed": [
                {
                    "content_id": "guide-health-families-001",
                    "reason": "household_fit_mismatch — family guide is not appropriate for a single-household customer",
                }
            ],
            "duration_ms": 9,
        },
        "prompt": {
            "description": "AI prompt input for a first-time customer discovering health insurance. Task is to generate a welcoming, non-pressuring comparison CTA explanation.",
            "task_prompt": "Generate a short, welcoming explanation for why a first-time visitor should compare health insurance options. The explanation should be helpful and informative without being pushy. It should help the customer feel confident taking the next step.",
            "grounding_snippets": ["gs-health-comparison-001", "gs-health-singles-cover-001"],
        },
        "final_response_description": "Final response for a first-time customer discovering health insurance. No secondary journey prompt.",
        "secondary_prompt": None,
        "analytics_description": "Analytics events for a first-time customer discovering health insurance. No prior journey events exist.",
        "analytics_extra_events": [],
        "expected_ai_output_file": "09-ai-expected-output.json",
    },
    "03-secondary-resume-quote": {
        "selection": {
            "description": "Active-journey selection result. Only one journey exists and it has a strong resume candidate signal.",
            "selection_method": "deterministic_single_journey",
            "reason_summary": "Single broadband journey with resume_candidate true and a high journey score. Customer returned directly to the broadband section after an abandoned quote 6 days ago.",
            "ai_confidence": 0.97,
            "ai_reason_summary": "Returning customer with a saved broadband quote. Direct return visit strongly suggests intent to complete. Resume flow is unambiguously the right path.",
        },
        "retrieval": {
            "description": "Candidate retrieval for a returning customer in quote_in_progress stage with resume_candidate true. Retrieval targets the quote funnel stage.",
            "primary_candidates": [
                "action-bbd-resume-quote-001",
                "offer-bbd-fast-family-001",
                "action-bbd-compare-plans-001",
            ],
            "secondary_candidates": [],
            "candidate_notes": {
                "action-bbd-resume-quote-001": "Resume CTA is the primary candidate given resume_candidate flag is true",
                "offer-bbd-fast-family-001": "Included as fallback comparison option if customer wants to reconsider plan",
                "action-bbd-compare-plans-001": "Included as secondary option in case customer wants to restart comparison",
            },
            "excluded": [
                {
                    "asset_id": "guide-bbd-moving-home-001",
                    "reason": "funnel_stage_mismatch — guide is for discover/research stages, customer is in quote stage",
                }
            ],
            "duration_ms": 12,
        },
        "ranking": {
            "description": "Ranking response. Resume CTA ranks first by a large margin due to resume_candidate flag, high journey score, and confirmed serviceability.",
            "ranked": [
                (
                    "action-bbd-resume-quote-001",
                    41,
                    [
                        "Active journey fit: broadband quote_in_progress journey",
                        "Resume candidate flag is true — quote was started and not completed",
                        "Intent alignment: ready_to_buy",
                        "Serviceability already confirmed — no need to restart from address check",
                        "High journey score (0.82) indicates strong purchase intent",
                        "Urgency is high",
                        "Priority score: 1",
                        "Resume bias policy applied: returning customer with saved quote receives maximum resume weight",
                    ],
                ),
                (
                    "offer-bbd-fast-family-001",
                    18,
                    [
                        "Service category match: broadband",
                        "Available as a fallback if customer wants to reconsider plan before resuming",
                        "Household fit: couple profile is compatible with fast family plan",
                    ],
                ),
                (
                    "action-bbd-compare-plans-001",
                    14,
                    [
                        "Service category match: broadband",
                        "Secondary fallback if customer wants to start comparison from scratch",
                    ],
                ),
            ],
            "suppressed": [],
            "duration_ms": 8,
        },
        "prompt": {
            "description": "AI prompt input for a returning customer resuming a broadband quote. Task is to generate a low-friction, reassuring re-engagement explanation.",
            "task_prompt": "Generate a short, reassuring explanation for a returning customer who started a broadband quote 6 days ago but did not finish. The explanation should make it easy and low-friction to pick up where they left off. It should acknowledge that their progress is saved without being pushy about completing.",
            "grounding_snippets": ["gs-bbd-resume-quote-001", "gs-bbd-fast-family-001"],
        },
        "final_response_description": "Final response for a returning customer resuming a saved broadband quote. Resume CTA is the next-best action. No secondary journey prompt.",
        "secondary_prompt": None,
        "analytics_description": "Analytics events for a returning customer resuming a saved broadband quote.",
        "analytics_extra_events": [
            {
                "event_type": "eligibility_checked",
                "timestamp_suffix": 0,
                "placement": "end",
                "metadata": {
                    "check_type": "serviceability",
                    "result": "confirmed",
                    "note": "Serviceability was confirmed in the prior session. Loaded from stored qualification state.",
                },
            }
        ],
        "expected_ai_output_file": "09-ai-expected-output.json",
    },
    "04-secondary-compliance-suppression": {
        "selection": {
            "description": "Active-journey selection result. Only one health insurance compare journey exists, so it is selected directly.",
            "selection_method": "deterministic_single_journey",
            "reason_summary": "Only one journey exists for this customer. Health insurance comparison journey is the clear active path.",
            "ai_confidence": 0.96,
            "ai_reason_summary": "Single active journey and strong compare-stage health insurance session signals. No ambiguity.",
        },
        "retrieval": {
            "description": "Candidate retrieval for a returning Tasmania customer comparing health insurance with active switching intent. Broad retrieval includes one bundle that will later be suppressed for compliance.",
            "primary_candidates": [
                "action-health-compare-001",
                "offer-health-hospital-extras-bundle-001",
                "guide-health-switching-001",
            ],
            "secondary_candidates": [],
            "candidate_notes": {
                "offer-health-hospital-extras-bundle-001": "Included by broad retrieval because intent and household fit are strong. Expected to be suppressed later because the bundle is state-restricted to NSW, VIC, and QLD."
            },
            "excluded": [
                {
                    "asset_id": "offer-health-family-001",
                    "reason": "household_fit_mismatch — customer profile is couple, not family",
                },
                {
                    "asset_id": "offer-health-singles-001",
                    "reason": "household_fit_mismatch — customer profile is couple, not single",
                },
                {
                    "asset_id": "action-health-resume-compare-001",
                    "reason": "resume_candidate_false — no saved comparison needs to be resumed",
                },
            ],
            "duration_ms": 16,
        },
        "ranking": {
            "description": "Ranking response. Compare CTA ranks first and the Provider K bundle is suppressed for compliance because it is not approved for Tasmania.",
            "ranked": [
                (
                    "action-health-compare-001",
                    31,
                    [
                        "Active journey fit: health insurance comparison journey",
                        "Funnel stage match: compare",
                        "Intent alignment: switching_provider",
                        "Comparison keeps the customer on a compliant path while they evaluate options",
                        "Priority score: 1",
                    ],
                ),
                (
                    "guide-health-switching-001",
                    18,
                    [
                        "Service category match: health_insurance",
                        "Supports compare-stage customers with portability and waiting-period guidance",
                        "Useful educational content before starting a quote",
                    ],
                ),
            ],
            "suppressed": [
                {
                    "content_id": "offer-health-hospital-extras-bundle-001",
                    "reason": "compliance_state_restriction — Provider K bundle carries state_restricted_nsw_vic_qld and cannot be promoted in TAS",
                }
            ],
            "duration_ms": 10,
        },
        "prompt": {
            "description": "AI prompt input for a returning customer in Tasmania comparing health insurance. Task is to explain the comparison CTA without mentioning suppressed non-compliant offers.",
            "task_prompt": "Generate a short, reassuring explanation for why this customer should compare health insurance options before starting a quote. The explanation should be helpful for someone in Tasmania who is actively considering switching providers. It should support the CTA without mentioning internal compliance rules or unavailable products.",
            "grounding_snippets": ["gs-health-comparison-001", "gs-health-switching-001"],
        },
        "final_response_description": "Final response for a returning customer comparing health insurance in Tasmania. Compliance suppression keeps the state-restricted bundle out of the promoted experience.",
        "secondary_prompt": None,
        "analytics_description": "Analytics events for a returning customer in Tasmania where a state-restricted bundle is suppressed before the final recommendation is served.",
        "analytics_extra_events": [
            {
                "event_type": "candidate_suppressed",
                "timestamp_suffix": 1,
                "placement": "after_active_journey",
                "metadata": {
                    "content_id": "offer-health-hospital-extras-bundle-001",
                    "provider": "Provider K",
                    "suppression_class": "compliance",
                    "suppression_reason": "state_restricted_nsw_vic_qld",
                    "customer_region": "TAS",
                },
            }
        ],
        "expected_ai_output_file": "09-ai-expected-output.json",
    },
}
