# AI, Edge Cases, And Performance

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: Content Personalization Strategy](../03-content-personalization-strategy.md) | [Previous: Metadata and Rules <-](./02-metadata-and-rules.md) | [Next: Illustrative Examples ->](./04-illustrative-examples.md)

## Overview

This document covers the bounded role of AI in personalization, how the platform should handle ambiguous or conflicting cases, and the runtime constraints that keep personalization responsive.

---

## AI Usage In Personalization

AI should help with:

- active-journey selection support
- intent inference
- content summarization
- explanation generation
- query expansion

AI must not be used for:

- hard ranking authority
- lead scoring authority
- business rule enforcement
- compliance overrides

---

## Edge Cases And Conflict Handling

The platform should be explicit about what happens when customer signals, campaign context, eligibility, and ranking pressure do not all point in the same direction.

This matters because many of the hardest personalization questions are really conflict-resolution questions.

### 1. Campaign Intent Conflicts With Customer History

Example:

- a customer arrives from a broadband campaign
- the customer also has a nearly complete health-insurance quote journey

Recommended behavior:

- use campaign context as an input, not an override
- evaluate recency, resume potential, current on-site behavior, and journey score
- choose one active journey for the session
- allow the non-leading journey to appear only as intentional secondary support

The platform should not blindly force the campaign category to lead if stronger customer-state evidence points elsewhere.

### 2. Multiple Journeys Are Simultaneously Plausible

Example:

- the customer has active broadband and health journeys
- both have recent activity
- current session behavior is mixed or weak

Recommended behavior:

- prefer the journey with the strongest combination of recency, qualification confidence, and next-step readiness
- keep the decision trace explicit so the result can be reviewed later
- surface the secondary journey only if it does not confuse the primary flow

When evidence is genuinely close, the system should still return one active journey rather than produce an incoherent mixed session.

### 3. A High-Priority Offer Is Not Suitable

Example:

- a provider or campaign has strong commercial priority
- the customer fails region, serviceability, or suitability checks

Recommended behavior:

- fail the candidate at deterministic filtering
- record the suppression reason
- do not allow commercial weighting to revive an ineligible or unsuitable result

Commercial priorities may reorder valid candidates, but they should not bypass protected constraints.

### 4. The Best Content Exists, But The Next Step Cannot Be Completed

Example:

- the content asset is relevant
- the CTA deep link does not match the active journey, channel, or region

Recommended behavior:

- treat CTA validity as part of suitability
- suppress or demote assets whose next step cannot land correctly
- prefer assets that move the customer into a valid, traceable flow

The platform should avoid showing a strong-looking recommendation that leads to a broken or generic destination.

### 5. Journey State Is Stale Or Incomplete

Example:

- the customer profile is known
- the most recent journey projection may lag the current session

Recommended behavior:

- use the latest committed projection in the live request path
- combine it with current session evidence
- tolerate bounded staleness rather than replaying raw history during the request
- emit telemetry that makes the final decision inspectable

The system should stay fast and explainable instead of trying to recompute everything synchronously.

### 6. AI Suggests A Different Interpretation Than Deterministic Signals

Example:

- AI suggests the customer should move into a different journey
- deterministic resume or qualification evidence points to another path

Recommended behavior:

- treat the AI result as decision support, not authority
- keep deterministic rules authoritative for protected decisions
- log both the AI suggestion and final selected outcome where useful

AI can help disambiguate intent, but it should not overrule deterministic evidence on its own.

### 7. No Strong Candidate Survives Filtering

Example:

- broad retrieval succeeds
- most or all candidates are suppressed by region, compliance, timing, or suitability constraints

Recommended behavior:

- return the best safe fallback set available
- prefer guidance, eligibility-check, callback, or resume actions over empty promotional slots
- avoid inventing recommendations that were not actually valid

The correct fallback is usually a safer next step, not a weaker version of the same invalid recommendation.

### 8. Secondary-Journey Support Starts To Pollute The Primary Path

Example:

- the system has valid cross-sell or secondary-journey items
- showing too many of them makes the current session harder to understand

Recommended behavior:

- cap the number and prominence of secondary-journey prompts
- keep the primary journey's next-best action dominant
- treat secondary support as optional and clearly labeled

Cross-journey support should expand opportunity without weakening session clarity.

### Summary Rule

When signals conflict, the platform should prefer:

1. deterministic safety and suitability
2. one clear active journey
3. the next step most likely to improve qualified conversion
4. explicit traceability for why the decision was made

That ordering keeps the system explainable even in ambiguous cases.

---

## Performance Considerations

### Latency Target

Personalization should be designed for:

- fast session experiences
- cached candidate retrieval where possible
- precomputed profile and journey state

### Optimization Strategies

- cache activity metadata with change-aware invalidation
- precompute intent and engagement signals
- avoid heavy runtime calculations in ranking
- reuse candidate sets only when profile, journey, and content versions remain valid

---

## Summary

AI should make personalization more helpful, not less governable.

Edge-case handling and performance discipline are what keep the personalization layer trustworthy under real session ambiguity.

---

| <- Previous | Next -> |
|---|---|
| [Metadata and Rules](./02-metadata-and-rules.md) | [Illustrative Examples](./04-illustrative-examples.md) |
