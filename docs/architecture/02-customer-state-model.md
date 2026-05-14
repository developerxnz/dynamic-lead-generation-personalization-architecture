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
  "identity": {
    "known_customer": true,
    "account_linked": false,
    "anonymous_ids": ["web-abc-123"]
  },
  "profile": {
    "household_type": "family",
    "employment_type": "full_time",
    "location": "NSW",
    "budget_range": "mid",
    "life_stage": "young_family"
  },
  "customer_summary": {
    "is_returning_customer": true,
    "last_meaningful_event_at": "2026-05-14T08:20:00Z",
    "repeat_sessions_30d": 3,
    "lead_score": 78
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

## Design Goals

The model should:

- separate durable customer profile from service-specific journey state
- support multiple concurrent journeys per customer
- make returning-customer behavior first-class
- preserve observed facts separately from inferred conclusions
- support AI-assisted interpretation without making AI the source of truth
- remain explainable to product, sales, operations, and compliance stakeholders

---

## Profile Vs Journey Responsibilities

| Model | What it should contain | What it should avoid |
|---|---|---|
| Customer profile | identity, household, employment, location, cross-journey summaries, global lead score | volatile per-service stage and intent detail |
| Journey state | service category, intent, stage, urgency, resume status, qualification state, journey-level score | unrelated customer-wide facts |

This separation keeps the data model cleaner and more scalable.

---

## What Belongs In The Customer Profile

The profile should contain durable or cross-journey information such as:

- identity and linkage keys
- household type
- employment type
- location
- budget range
- life stage
- cross-journey return behavior
- overall lead score or customer value signals

These fields are useful regardless of which service journey is currently most active.

---

## What Belongs In Journey State

Journey state should contain service-specific and time-sensitive information such as:

- service category
- current intent
- current stage
- urgency
- switching or renewal state
- qualification state
- quote or application progress
- journey-level behavior summary
- journey-level AI summary

This is the state that personalization and ranking should use most heavily.

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

AI should not replace:

- durable profile facts
- hard qualification evidence
- deterministic suppression rules
- authoritative compliance constraints

AI contributes **interpretation**. The profile and journey entities remain the system of record.

---

## Service-Specific Signal Examples

### Novated Leasing Journey

- employer eligibility checks
- EV tax-benefit calculator usage
- salary-packaging guide consumption
- budget and vehicle exploration

### Health Insurance Journey

- cover comparison behavior
- household composition updates
- hospital versus extras exploration
- repeat quote attempts near renewal timing

### Broadband Journey

- address availability checks
- speed-tier comparison
- move-home flows
- contract expiry or churn intent signals

---

## Scoring Approach

The model should support both:

- a **customer-level score** for broad lead value or overall opportunity
- a **journey-level score** for service-specific prioritization

Journey-level scoring is especially important when customers are active in more than one service path.

Deterministic scoring should remain the primary operational score initially.

AI can contribute:

- supporting relevance signals
- journey summaries
- multi-journey pattern detection

But authoritative scores should remain explainable and reproducible.

---

## Design Principles

- separate profile from journey state
- support multiple journeys per customer
- preserve observed facts separately from inferred state
- track return behavior at both customer and journey level
- let AI improve interpretation without obscuring control logic
- optimize the model for live decisions, not just storage completeness

---

## Summary

The customer state model should be structured around:

- a durable **customer profile**
- multiple **journey states**
- an explicit **active-journey selection** step for live decisioning

That gives the platform a cleaner way to support **parallel journeys**, stronger **returning-customer re-engagement**, and more accurate **qualified-conversion-focused personalization**.

---

| <- Previous | Next -> |
|---|---|
| [Multi-Vertical Lead Generation Platform Overview](./01-overview.md) | [Content Personalization Strategy](./03-content-personalization-strategy.md) |
