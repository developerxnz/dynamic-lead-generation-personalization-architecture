# Customer State Model

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Content Personalization Strategy ->](./03-content-personalization-strategy.md)

## Overview

Customer state should be modeled as **two related concepts**:

1. a durable **customer profile**
2. one or more **journey states**

That separation is important for this platform because a customer can legitimately be active in multiple journeys at once. For example:

- comparing health cover while also exploring broadband for an upcoming move
- researching novated leasing while returning to resume a partially completed insurance quote
- holding long-lived background interest in one category while showing high current intent in another

The system therefore needs to support:

- a stable customer profile shared across journeys
- multiple concurrent journey states
- an explicit view of which journey is most relevant **right now**

---

## Why This Shape Works Better

If everything is stored inside a single flat state model, the platform struggles to represent parallel intent cleanly.

If everything is modeled as a deeply nested per-product tree, the profile becomes hard to reason about operationally.

The better pattern is:

- keep the **profile** as its own entity
- keep **journey state** as a repeatable structure
- let live decisioning choose an **active journey**

This gives the platform enough flexibility for multi-journey behavior without losing clarity in the current session.

---

## Recommended Model

### Customer Profile Entity

The customer profile should contain durable facts and cross-journey signals.

```json
{
  "customer_id": "cust-12345",
  "attributes": {
    "household_type": "family",
    "employment_type": "full_time",
    "location": "NSW",
    "budget_range": "mid",
    "life_stage": "young_family"
  }
}
```

The profile entity should remain relatively stable and should not try to hold the full live state of every journey.

---

### Journey State Entity

Each journey state represents a service-specific path the customer is currently, or recently, engaged in.

```json
{
  "journey_id": "journey-health-001",
  "customer_id": "cust-12345",
  "service_category": "health_insurance",
  "status": "active",
  "intent": "comparing_options",
  "stage": "quote_ready",
  "urgency": "high",
  "switching_intent": "active",
  "renewal_window_days": 21,
  "resume_candidate": true,
  "qualification_state": {
    "coverage_region_match": true,
    "serviceability_confirmed": true,
    "hard_exclusions": [],
    "suppression_flags": []
  },
  "behavior_summary": {
    "recent_quote_started": true,
    "provider_comparisons_7d": 4
  },
  "decision_support": {
    "journey_score": 0.76,
    "ai_journey_summary": "Returning visitor likely ready to compare family cover and resume quote flow"
  },
  "last_meaningful_event_at": "2026-05-14T08:20:00Z"
}
```

This structure allows the platform to keep separate state for multiple concurrent journeys without overloading the core customer profile.

---

## What A Journey Summary Actually Is

A **journey summary** is not the same thing as the full stored journey document or raw event history.

It is the compact, decision-ready view of a journey that downstream systems should usually read during live decisioning.

In plain language:

- the **full journey state** is everything the platform knows about that journey
- the **journey summary** is the smaller subset that is most useful for choosing what to do next

This matters because orchestration, ranking, and AI prompt assembly should not need to scan full event history on every request.

### What A Journey Summary Should Usually Contain

- journey ID
- service category
- current intent
- current stage
- resume indicator
- qualification or suitability status
- recent behavior summary
- journey score
- last meaningful event time
- short human-readable reason or summary where useful

### What A Journey Summary Should Usually Avoid

- raw event-by-event history
- large free-form notes
- full content payloads
- low-level processing metadata that is only useful for rebuilds or repair

### Example Journey Summary

```json
{
  "journey_id": "journey-health-001",
  "service_category": "health_insurance",
  "intent": "comparing_options",
  "stage": "quote_ready",
  "resume_candidate": true,
  "qualification_state": {
    "coverage_region_match": true,
    "serviceability_confirmed": true,
    "hard_exclusions": []
  },
  "behavior_summary": {
    "recent_quote_started": true,
    "provider_comparisons_7d": 4
  },
  "journey_score": 0.76,
  "last_meaningful_event_at": "2026-05-14T08:20:00Z",
  "ai_journey_summary": "Returning visitor likely ready to compare family cover and resume quote flow"
}
```

This is the form that active-journey selection, ranking, and AI support layers should prefer when making live decisions.

---

## Active Journey Selection

Even when multiple journeys exist, the platform still needs to decide:

> which journey should drive the current session

That should be handled through an active-journey decision layer, not by assuming the customer only has one journey.

Recommended active-journey factors:

- recency of meaningful events
- current session behavior
- channel or campaign context
- journey score or intent strength
- qualification confidence
- whether the customer is resuming a previously interrupted path

The platform can therefore maintain multiple journeys while still keeping session decisioning clear.

---

## Profile Vs Journey Responsibilities

| Model | What it should contain | What it should avoid |
|---|---|---|
| Customer profile | durable attributes, household, employment, location | volatile per-service stage and intent detail |
| Journey state | service category, intent, stage, urgency, resume status, qualification state, journey-level score | unrelated customer-wide facts |

This separation keeps the data model cleaner and more scalable.

---

## Returning-Customer Logic

Returning customers should not be treated as blank sessions.

The model should capture both:

- **customer-level return behavior**, such as repeat visits and time since last meaningful interaction
- **journey-level return behavior**, such as whether a specific quote or application should be resumed

Recommended examples:

| Situation | Recommended behavior |
|---|---|
| Customer returns to same active journey | Resume with reduced friction and stronger conversion support |
| Customer returns but shifts category | Select a different active journey while preserving prior journey state |
| Customer has multiple active journeys | Use current session evidence to choose the best active journey for presentation |
| Renewal window creates re-entry | Elevate the affected journey without discarding other active ones |

---

## AI's Role In The State Model

AI should help interpret messy evidence, especially when customers move between journeys or provide incomplete information.

Appropriate AI uses:

- summarizing journey history into readable explanations
- improving confidence in intent classification
- interpreting free-text or conversational inputs
- helping identify which journey is likely most relevant now

In practice, AI should usually read a **journey summary** rather than the raw underlying event stream.

That keeps prompts smaller, makes reasoning easier to inspect, and reduces the risk that prompt assembly becomes a hidden replay engine.

AI should not replace:

- durable profile facts
- hard qualification evidence
- deterministic suppression rules
- authoritative compliance constraints

AI contributes **interpretation**. The profile and journey entities remain the system of record.

### Runtime Boundary

The local runtime builds a bounded summary for each active or recent journey before
calling AI. The interpretation response may contain only:

- `suggested_journey_id` from the supplied candidate set
- confidence from `0.0` to `1.0`
- a short reason summary

The deterministic selector evaluates current URL, query text, campaign context,
journey score, recency, and resume state before it chooses the active journey.
It can accept an AI suggestion, but it records an explicit override whenever
stronger session evidence selects a different journey. Invalid, timed-out, or
unavailable AI interpretation leaves the deterministic path in control.

AI-generated summaries and interpretations are decision-support records. They
must be stored with their model and prompt metadata separately from durable
profile facts and journey projections.

---

## Scoring Approach

The model should support a **journey-level score** for service-specific prioritization.

Journey-level scoring matters most when customers are active in more than one service path. AI can contribute supporting relevance signals or summaries, but authoritative scores should remain deterministic, explainable, and reproducible.

---

| <- Previous | Next -> |
|---|---|
| [Multi-Vertical Lead Generation Platform Overview](./01-overview.md) | [Content Personalization Strategy](./03-content-personalization-strategy.md) |
