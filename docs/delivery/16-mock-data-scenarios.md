# Mock Data Scenarios

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: POC Story And Presentation Narrative <-](./15-poc-story.md)

## Overview

This document describes the four mock data scenarios used to validate the platform's decisioning flow against the AI model.

The full data set lives in the [`mock-data/`](../../mock-data/) folder at the repository root. This document explains each scenario, the data shape used, and how the files relate to the broader architecture.

---

## Purpose

The mock data is designed to support **full end-to-end trace validation** across the four POC scenarios defined in [POC Scope](./12-poc-scope.md).

Each scenario provides:

- starting customer state (profile and journey states)
- a session request that triggers the decisioning flow
- expected intermediate outputs at each step (active-journey selection, retrieval, ranking)
- a complete AI prompt input package
- an expected AI model output with validation criteria
- the expected final experience response
- the expected analytics events

This means each scenario can be used to:

- test individual service outputs against expected values
- validate AI model responses against a deterministic target
- demonstrate the full decision trace in a POC setting

---

## Scenarios

### Scenario 01 — Primary: Returning Customer With Multiple Journeys

**Folder:** [`mock-data/scenarios/01-primary-returning-multi-journey/`](../../mock-data/scenarios/01-primary-returning-multi-journey/)

**Customer:** `cust-20481` — returning customer, family household, NSW

**Journey states:**
- `journey-health-301` — health insurance, comparing options, resume candidate, score 0.68
- `journey-broadband-118` — broadband, moving home, research stage, score 0.74

**Session:** web, paid search, campaign theme `move-home-broadband`, entry URL `/broadband/moving-home`

**Active journey selected:** broadband — current session signals are stronger than the older health comparison journey

**Next best action:** `action-bbd-address-check-001` — check broadband availability at new address

**Secondary journey prompt:** health comparison resume retained as a secondary prompt

**Why this scenario matters:** This is the richest test case. It exercises active-journey selection from two competing journeys, deterministic ranking where the primary candidate is a serviceability check rather than an offer, AI explanation grounded on move-home context, and a secondary-journey prompt in the final response.

---

### Scenario 02 — Secondary A: First-Time Customer, Single Journey

**Folder:** [`mock-data/scenarios/02-secondary-new-customer/`](../../mock-data/scenarios/02-secondary-new-customer/)

**Customer:** `cust-99001` — new customer, single household, VIC

**Journey states:**
- `journey-health-812` — health insurance, researching options, discover stage, score 0.41

**Session:** web, organic search, entry URL `/health-insurance/plans`, query text `health insurance options for singles`

**Active journey selected:** health insurance — only journey, no ambiguity

**Next best action:** `action-health-compare-001` — compare health cover options

**Suppressed at ranking:** `guide-health-families-001` — household mismatch (single, not family)

**Why this scenario matters:** This validates that the architecture works cleanly for a simple first-visit path. It shows that the household profile suppresses irrelevant content at ranking, and that a new customer journey can be created and acted on within a single session.

---

### Scenario 03 — Secondary B: Returning Customer Resuming An Interrupted Quote

**Folder:** [`mock-data/scenarios/03-secondary-resume-quote/`](../../mock-data/scenarios/03-secondary-resume-quote/)

**Customer:** `cust-55312` — returning customer, couple household, QLD

**Journey states:**
- `journey-broadband-441` — broadband, ready to buy, quote started 6 days ago, 60% complete, resume candidate true, score 0.82

**Session:** web, direct return visit, entry URL `/broadband`

**Active journey selected:** broadband — only journey, high resume signal

**Next best action:** `action-bbd-resume-quote-001` — pick up where you left off (resume bias applied)

**Why this scenario matters:** This validates that resume logic works correctly. The ranking engine should apply resume bias to promote the resume CTA well above any other candidate. The AI explanation should be low-friction and reassuring, not generic. The eligibility check from the prior session should be preserved in the qualification state.

---

### Scenario 04 — Secondary C: Compliance Suppression Of A State-Restricted Offer

**Folder:** [`mock-data/scenarios/04-secondary-compliance-suppression/`](../../mock-data/scenarios/04-secondary-compliance-suppression/)

**Customer:** `cust-66140` — returning customer, couple household, TAS

**Journey states:**
- `journey-health-944` — health insurance, switching provider, compare stage, score 0.67

**Session:** web, organic search, entry URL `/health-insurance/compare`, query text `compare health insurance tas`

**Active journey selected:** health insurance — only journey, no ambiguity

**Next best action:** `action-health-compare-001` — compare health cover options

**Suppressed at ranking:** `offer-health-hospital-extras-bundle-001` — compliance state restriction (`state_restricted_nsw_vic_qld`)

**Why this scenario matters:** This is the clearest proof that deterministic compliance controls remain authoritative. The bundle looks relevant on intent and household fit, but it still cannot be promoted in Tasmania. The final experience therefore leads with compliant comparison rather than a non-compliant quote path.

---

## Data Shape Overview

All mock data follows the schemas defined in the architecture documentation.

| Data type | Schema reference |
|---|---|
| Customer profile | [Customer State Model](../architecture/02-customer-state-model.md) |
| Journey states | [Customer State Model](../architecture/02-customer-state-model.md) |
| Activity assets | [Activity Metadata](../services/05-activity-metadata.md) |
| Ranking request and response | [Ranking Engine: Runtime and Contracts](../services/ranking-engine/02-runtime-and-contracts.md) |
| AI prompt input and output | [AI and RAG Strategy: Runtime and Implementation](../ai/ai-and-rag-strategy/01-runtime-and-implementation.md) |

---

## Per-File Reference

Each scenario folder contains 11 files in processing order:

| File | What it contains |
|---|---|
| `01-customer-profile.json` | Durable customer profile entity |
| `02-journey-states.json` | Journey state entities for this customer |
| `03-session-request.json` | Incoming channel request to the orchestrator |
| `04-active-journey-selection.json` | Active-journey selection result and reasoning |
| `05-candidate-retrieval.json` | Retrieval query and the candidate set returned |
| `06-ranking-request.json` | Full ranking engine request |
| `07-ranking-response.json` | Ranking engine response with scores, reasons, and suppressions |
| `08-ai-prompt-input.json` | Assembled AI prompt context package |
| `09-ai-expected-output.json` | Expected AI response with validation criteria |
| `10-final-response.json` | Final orchestrated response returned to the channel |
| `11-analytics-events.json` | Expected telemetry events emitted during this flow |

---

## Shared Activity Assets

Both verticals share a common set of normalized activity assets in `mock-data/shared/`.

These assets are referenced by ID across all scenarios and represent the platform's available candidate pool for retrieval and ranking.

See [`mock-data/README.md`](../../mock-data/README.md) for the full asset list and usage guidance.

---

## AI Validation

The `09-ai-expected-output.json` file in each scenario defines the deterministic target for the AI model response. It includes:

- `response` — expected structured output matching the response contract defined in `08-ai-prompt-input.json`
- `validation` — field-level checks covering required fields, grounding coverage, claim safety, and length bounds
- `response_status` — `accepted` or `rejected`

All four expected outputs have `response_status: accepted`. This means:

- required fields are present
- grounding asset IDs are cited
- no unsupported claims are introduced
- summary and CTA text are within configured length bounds

---

| <- Previous |
|---|
| [POC Story And Presentation Narrative](./15-poc-story.md) |
