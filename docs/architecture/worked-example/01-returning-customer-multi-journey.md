# Worked Example: Returning Customer With Multiple Journeys

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Previous: Platform Overview <-](../01-overview.md) | [Next: Customer State Model ->](../02-customer-state-model.md)

## Overview

This document shows how the architecture works end to end using one realistic scenario.

The goal is to make the platform easier to explain to marketing, product, engineering, and analytics stakeholders by walking through:

1. the starting customer state
2. the current session context
3. active-journey selection
4. candidate retrieval
5. deterministic filtering and ranking
6. AI-assisted explanation
7. telemetry and downstream outcome measurement

---

## Why This Example Matters

Many architecture documents explain system components clearly but still leave readers asking:

- what actually happens in one session
- how multiple journeys are handled in practice
- where deterministic logic ends and AI support begins
- what data and telemetry move through the system

This walkthrough answers those questions in one place.

---

## Scenario

Assume a known customer returns to the platform with:

- an existing **health insurance** journey already in progress
- a new **broadband** session driven by move-home intent
- enough context for the system to decide which journey should lead the session now

The platform should:

- preserve the existing health journey
- recognize the new broadband intent
- choose the active journey for this session
- return the best next action for qualified conversion

---

## Starting Customer State

### Customer Profile

```json
{
  "customer_id": "cust-20481",
  "attributes": {
    "household_type": "family",
    "employment_type": "full_time",
    "location": "NSW",
    "move_window_days": 18
  }
}
```

### Journey Summaries

```json
[
  {
    "journey_id": "journey-health-301",
    "service_category": "health_insurance",
    "status": "active",
    "stage": "compare",
    "intent": "comparing_options",
    "resume_candidate": true,
    "journey_score": 0.68,
    "last_meaningful_event_at": "2026-05-14T11:20:00Z"
  },
  {
    "journey_id": "journey-broadband-118",
    "service_category": "broadband",
    "status": "active",
    "stage": "research",
    "intent": "moving_home",
    "resume_candidate": false,
    "journey_score": 0.74,
    "last_meaningful_event_at": "2026-05-16T19:10:00Z"
  }
]
```

This is already enough to show an important design choice:

- the customer has **one durable profile**
- the customer has **more than one live journey**
- the system still needs **one active journey** for the current session

---

## Current Session Input

The live request arrives with session context that strongly suggests broadband is the immediate need.

```json
{
  "customerId": "cust-20481",
  "sessionId": "sess-77821",
  "channel": "web",
  "entryPoint": "paid_search",
  "campaignTheme": "move-home-broadband",
  "currentUrl": "/broadband/moving-home",
  "region": "NSW",
  "queryText": "best internet for new house"
}
```

The orchestration layer now combines:

- customer profile
- journey summaries
- current session context

to decide what should lead the session.

---

## Step 1: Active-Journey Selection

The platform evaluates the candidate journeys using deterministic signals such as:

- recency of meaningful events
- session URL and campaign alignment
- journey score
- resume potential
- qualification confidence

### Example Active-Journey View

| Journey | Signals helping it | Signals limiting it | Decision effect |
|---|---|---|---|
| Broadband | latest meaningful event, broadband campaign source, moving-home page context, higher current journey score | journey is newer and less progressed | becomes **active journey** |
| Health insurance | resume candidate, prior comparison activity, still active | weaker session alignment, older recency | remains available as a secondary journey |

### Result

```json
{
  "active_journey_id": "journey-broadband-118",
  "active_service_category": "broadband",
  "decision_reason": "session context and recent behavior indicate move-home broadband intent is currently strongest"
}
```

The health journey is not deleted or ignored. It simply does not lead this session.

---

## Step 2: Broad Candidate Retrieval

The system then retrieves a broad candidate set from existing activities and their metadata.

This is deliberately broader than the final answer because retrieval should discover possibilities, while downstream logic decides what is safe and useful to show.

### Example Candidate Set

| Candidate ID | Asset type | Service category | Funnel stage | CTA type | Why retrieved |
|---|---|---|---|---|---|
| `bbd-guide-move-home` | guide | broadband | research | compare | matches move-home broadband research intent |
| `bbd-cta-address-check` | CTA module | broadband | compare | check_eligibility | fits move-home journey and address availability step |
| `bbd-offer-fast-family` | offer card | broadband | compare | compare | aligned to family household and speed-comparison behavior |
| `bbd-offer-budget-nbn` | offer card | broadband | compare | compare | still broadly relevant candidate |
| `health-resume-compare` | CTA module | health_insurance | compare | resume | secondary journey support candidate |
| `generic-utilities-hero` | banner | broadband | research | compare | broad category metadata match |

---

## Step 3: Deterministic Filtering And Suppression

Now the platform applies authoritative rules before ranking.

### Example Filters

| Candidate ID | Outcome | Reason |
|---|---|---|
| `bbd-guide-move-home` | keep | active journey match and useful for early comparison stage |
| `bbd-cta-address-check` | keep | strong fit for next actionable step in move-home broadband journey |
| `bbd-offer-fast-family` | keep | region supported and aligned to household and intent |
| `bbd-offer-budget-nbn` | suppress | low suitability because serviceability check not yet confirmed for this speed tier |
| `health-resume-compare` | keep as secondary | still valid, but not primary for this session |
| `generic-utilities-hero` | suppress | too generic compared with stronger journey-specific candidates |

This is where deterministic systems stay authoritative.

AI does **not** decide:

- whether the customer is eligible
- whether the offer can be shown in-region
- whether a suppressed asset should bypass policy

---

## Step 4: Ranking

The remaining candidates are ranked using deterministic scoring plus permitted AI support signals.

### Example Ranking Inputs

| Signal | Example impact in this scenario |
|---|---|
| Active-journey match | strongly favors broadband assets |
| Funnel-stage match | favors compare-stage assets and actionable next steps |
| Behavioral relevance | favors family-plan comparison content |
| CTA alignment | boosts address check because it moves the customer toward a qualified conversion path |
| Commercial priority | can reorder close candidates if business rules allow |
| AI-assisted relevance support | can help interpret query text and similarity, but does not override hard constraints |

### Example Ranked Output

| Rank | Candidate ID | Why it landed here |
|---|---|---|
| 1 | `bbd-cta-address-check` | best next action for move-home broadband qualification |
| 2 | `bbd-offer-fast-family` | strongest offer match after suitability checks |
| 3 | `bbd-guide-move-home` | useful supporting content for confidence and comparison |
| 4 | `health-resume-compare` | relevant secondary action, but not primary for this session |

This output is optimized for **qualified conversion**, not generic click volume.

---

## Step 5: Experience Response

The orchestrator returns a response that is ready for the web or app experience to render.

```json
{
  "customerId": "cust-20481",
  "sessionId": "sess-77821",
  "activeJourney": {
    "journeyId": "journey-broadband-118",
    "serviceCategory": "broadband",
    "stage": "research"
  },
  "nextBestAction": {
    "candidateId": "bbd-cta-address-check",
    "ctaType": "check_eligibility",
    "label": "Check broadband options at your new address",
    "deepLink": "/broadband/address-check"
  },
  "supportingContent": [
    {
      "candidateId": "bbd-offer-fast-family",
      "type": "offer_card"
    },
    {
      "candidateId": "bbd-guide-move-home",
      "type": "guide"
    }
  ],
  "secondaryJourneyPrompt": {
    "journeyId": "journey-health-301",
    "label": "Resume your health cover comparison"
  }
}
```

This shows a core platform behavior:

- one journey leads
- supporting content is aligned to that journey
- another valid journey can still be surfaced intentionally without confusing the session

---

## Step 6: AI's Role In This Example

AI contributes in bounded ways.

### AI Can Help With

- interpreting the free-text query `"best internet for new house"`
- generating a short customer-facing explanation
- summarizing why the selected CTA is relevant
- improving semantic retrieval of supporting broadband guides

### AI Does Not Decide

- the active-journey winner on its own
- eligibility or serviceability
- ranking overrides for suppressed items
- compliance or policy exceptions

### Example AI Explanation

> Because you're moving soon and recently explored broadband options, checking address availability is the fastest way to narrow to plans that can actually be connected at your new home.

That explanation improves clarity, but the authoritative decision came from deterministic systems.

---

## Step 7: Telemetry And Measurement

The session should emit structured telemetry through Segment.io so the team can analyze both decision quality and business outcomes in Mixpanel.

### Example Events

| Event | What it captures |
|---|---|
| `active_journey_selected` | broadband selected over health insurance, with decision context |
| `recommendation_served` | ranked candidates, policy version, metadata revision, and suppression reasons |
| `ai_explanation_shown` | explanation variant, prompt version, and model version where used |
| `cta_clicked` | customer clicked the address-check CTA |
| `eligibility_checked` | address or serviceability result returned |
| `provider_handoff_started` | customer progressed into a qualified provider path |

### Example Measurement Questions

- did active-journey selection lead to stronger CTA progression than resuming health by default
- did the address-check CTA produce better qualified outcomes than a generic compare CTA
- how often did the secondary health reminder still lead to later re-entry
- did AI explanation improve progression without increasing defect or fallback rates

---

## Why This Is Better Than A Generic Experience

Without this architecture, the customer would likely receive one of these weaker experiences:

- a generic broadband landing page with no customer memory
- a forced return to the old health journey despite clear current broadband intent
- a campaign-led page that ignores suitability and qualification state

With this architecture, the platform can:

- preserve prior context
- adapt to the current session
- choose one clear next action
- keep deterministic business controls intact
- measure whether the decision improved qualified conversion

---

## Questions This Example Should Help Answer

### Marketing And Growth

- how does the platform balance campaign context with customer context
- can it still surface a secondary journey without losing focus
- what events show whether the decision improved lead quality

### Product And Delivery

- how does active-journey selection actually change the experience
- where does the next-best action come from
- how do supporting content and secondary prompts fit together

### Engineering And Data

- which data is read in the synchronous path
- which rules are deterministic
- where AI participates without becoming authoritative
- which events are required for traceability

---

## Summary

This worked example demonstrates the intended runtime pattern:

1. load durable customer and journey summaries
2. choose the active journey for the session
3. retrieve broadly
4. filter deterministically
5. rank for qualified conversion
6. use AI only where it adds interpretation or explanation value
7. emit telemetry that makes the decision explainable and measurable

That is the core operating model of the platform.

---

| <- Previous | Next -> |
|---|---|
| [Platform Overview](../01-overview.md) | [Customer State Model](../02-customer-state-model.md) |
