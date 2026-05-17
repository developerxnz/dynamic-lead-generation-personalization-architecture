# POC Scope

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: Delivery Roadmap <-](./11-roadmap.md) | [Next: Ownership And Operating Model ->](./13-ownership-and-operating-model.md)

## Overview

This document defines the recommended **proof of concept** for the platform.

Its purpose is to turn the architecture into a concrete first implementation that can be:

- demonstrated live
- evaluated by product, marketing, and engineering stakeholders
- measured using a small but credible telemetry foundation
- used to prove that the AI-forward architecture is practical without making the first release too broad

---

## POC Objective

The POC should prove that the platform can:

1. maintain a durable customer profile over time
2. support multiple concurrent journey states for the same customer
3. choose the right active journey for the current session
4. retrieve and rank eligible offers, content, and CTAs using deterministic controls
5. use AI to improve interpretation or explanation without replacing authoritative business logic
6. show a visible next-best action and measure whether it influences progress toward qualified conversion

---

## Demo Audience

The POC should be understandable to three core audiences.

| Audience | What they should see |
|---|---|
| Marketing and growth | more relevant journeys, clearer next actions, and better visibility into qualified lead outcomes |
| Product and delivery | explicit journey handling, visible decision logic, and a realistic phased path to broader rollout |
| Engineering and data | clear service boundaries, durable state, traceable ranking, and measurable event flow |

---

## Recommended POC Scenario Set

The POC should use **one primary scenario** that is fully demonstrated end to end, plus **one or two secondary scenarios** that show the architecture generalizes cleanly.

This gives the demo enough breadth to feel credible without turning the POC into a mini-product.

### Primary Scenario

The primary scenario should be a **returning-customer, multi-journey flow** because it demonstrates more of the architecture than a simple first-visit path.

For a concrete documentation walkthrough of this style of scenario, see [Worked Example: Returning Customer With Multiple Journeys](../architecture/worked-example/01-returning-customer-multi-journey.md).

For the delivery-focused version showing example payloads, decision trace, and analytics events, see [POC Demo Flow](./14-poc-demo-flow.md).

Recommended scenario:

1. a known customer has an existing journey in one category
2. the same customer returns with signals that indicate a second possible journey
3. the platform loads the durable profile and both journey states
4. the platform selects the active journey for the session
5. the platform retrieves candidate offers and supporting content
6. deterministic qualification and ranking narrow the result set
7. AI provides a useful interpretation or explanation layer
8. the experience returns a clear next-best action and emits measurement events

Example:

- an existing health-insurance journey is already in progress
- the same customer returns from a broadband-related campaign or page
- the platform must decide whether to resume health, lead with broadband, or support both without losing clarity

This primary scenario proves:

- durable state
- active-journey selection
- multi-journey handling
- deterministic controls
- AI-assisted support
- measurable decisioning

### Secondary Supporting Scenarios

The supporting scenarios should be narrower and faster to explain.

Recommended secondary scenarios:

1. **first-time customer, single clear journey**
   - show that the architecture also works when there is no journey ambiguity
   - demonstrate a clean first-visit path from session to next-best action
2. **returning customer resuming an interrupted quote or application**
   - show that prior state can be resumed with lower friction
   - demonstrate how returning-customer recovery works without requiring a second concurrent journey to lead the session

These supporting scenarios help prove the design is reusable rather than overfit to one complex story.

---

## What Must Be Shown Live

The POC should show the following capabilities in a live or interactive demo:

### 1. Durable Customer Profile

Show that the same customer can be recognized or resolved and that prior state is available to the decisioning flow.

### 2. Multiple Journey States

Show at least two journeys associated with the same customer, with different stages or intent signals.

### 3. Active-Journey Selection

Show how the platform chooses which journey should lead the session and why.

### 4. Deterministic Qualification And Ranking

Show:

- candidate retrieval
- eligibility or suitability filtering
- deterministic ranking inputs
- promoted and suppressed outcomes

### 5. Visible Next-Best Action

Show the final response containing:

- the selected journey context
- recommended content or offer
- supporting CTA
- explanation of why it is relevant

### 6. Telemetry And Outcome Trace

Show that the experience emits:

- active journey selected
- recommendation served
- next action clicked or progressed

This is the minimum needed to make the POC measurable rather than just visually impressive.

---

## What Can Be Described Rather Than Fully Built

To keep the POC focused, the following can be described, stubbed, or represented through documented assumptions:

- full multi-vertical rollout beyond the chosen scenario
- advanced experimentation workflows
- deep historical analytics projections
- full conversational experiences
- production-hardening concerns such as exhaustive scaling, resilience, and governance workflows
- mature provider optimization loops across many partners

The POC should prove the architecture works, not attempt to finish the full platform.

---

## Recommended Functional Scope

Keep the POC intentionally narrow.

### In Scope

- one primary returning-customer, multi-journey scenario
- one or two supporting scenarios that reuse the same platform model
- two concurrent journeys for the same customer in the primary scenario
- one active-journey decision
- one candidate retrieval path
- one deterministic ranking configuration
- one AI-assisted interpretation or explanation capability
- one visible next-best-action experience
- core Segment.io telemetry for the demo flow

### Out Of Scope

- broad multi-channel rollout
- full provider network coverage
- complex experimentation orchestration
- autonomous AI decisioning
- deep operational governance tooling

---

## Success Criteria

The POC should be considered successful if it can demonstrate all of the following:

### Product Success

- stakeholders can see that multi-journey handling is practical and understandable
- the next-best-action result feels meaningfully better than a generic experience
- the flow clearly shows how returning-customer re-entry works
- the supporting scenarios make it clear the design works beyond one narrow demo

### Engineering Success

- the demo uses stable customer and journey identifiers
- decisions are traceable from profile to active journey to ranked outcome
- deterministic controls remain authoritative over qualification and ranking

### Measurement Success

- Segment.io events are emitted for the key decision points
- the served recommendation can be tied back to journey and ranking context
- the team can show at least a simple funnel or decision trace in Mixpanel or a comparable view

### Executive Confidence

- the POC makes the architecture feel credible, not theoretical
- the audience can see what should be built next and why

---

## Minimum Data And Telemetry Needed

The POC should carry a minimal but consistent data shape:

- customer ID
- session ID
- journey ID
- service category
- active journey selected
- recommendation ID
- ranking policy version
- content revision where applicable

Minimum events:

- `active_journey_selected`
- `recommendation_served`
- `cta_clicked` or `quote_started`
- optional `ai_explanation_shown`

---

## Suggested Demo Narrative

The presentation should be easy to follow:

1. start with the primary returning-customer, multi-journey scenario
2. show the known customer and existing journey state
3. introduce new session context that creates journey ambiguity
4. show the platform selecting the active journey
5. show the candidate set being filtered and ranked
6. show the personalized response, explanation, and telemetry
7. briefly step through the supporting scenarios to show the same architecture also handles simpler first-visit and resume flows

This sequence keeps the POC aligned to both technical credibility and business clarity.

---

## What This POC Sets Up Next

If successful, the POC creates a clean bridge into the broader roadmap:

- stronger behavioral scoring
- richer returning-customer re-entry logic
- broader vertical rollout
- better dashboard coverage
- deeper AI guidance and retrieval

It should therefore be treated as the first credible slice of the platform, not a throwaway prototype.

---

## Summary

The best POC for this platform uses **one primary returning-customer, multi-journey demonstration** plus **supporting first-visit and resume scenarios**.

That gives all major stakeholders enough evidence to believe the architecture is both valuable, reusable, and buildable.

---

| <- Previous | Next -> |
|---|---|
| [Delivery Roadmap](./11-roadmap.md) | [Ownership And Operating Model](./13-ownership-and-operating-model.md) |
