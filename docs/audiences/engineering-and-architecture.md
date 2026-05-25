# Engineering and Architecture Guide

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: Product and Delivery Guide <-](./product-and-delivery.md)

## Overview

This reading path is for **engineering, architecture, and data stakeholders** who need to understand:

- runtime boundaries and system responsibilities
- profile, journey, and event-processing models
- decisioning and ranking contracts
- analytics and AI integration points

It is intended to give engineering readers a concise explanation of how the platform should work end to end before they drop into the deeper service and contract pages.

---

## Why This Matters For Engineering

This platform is not just a collection of APIs. It is a coordinated decisioning system with strict boundaries between:

- customer state
- journey state
- activity metadata
- deterministic decision logic
- AI-assisted interpretation
- telemetry and optimization

If those boundaries are unclear, teams usually end up with:

- business rules leaking into content queries
- raw event history being used in live request paths
- front-end channels owning too much orchestration logic
- AI behavior creeping into authoritative ranking or policy decisions
- telemetry added too late to explain outcomes properly

The goal of this architecture is to keep the system **fast, explainable, replayable, and safe to evolve**.

---

## What Engineering Should Take Away

Engineering readers should understand six core ideas.

### 1. The Live Request Path Should Stay Small

The synchronous request path should be limited to the steps required to resolve the current session:

1. read customer profile and journey projections
2. select the active journey
3. retrieve a broad candidate set
4. apply deterministic qualification and suitability constraints
5. rank the remaining candidates
6. optionally add lightweight AI-supported explanation text
7. return the assembled experience payload

That path should avoid:

- replaying raw events
- large fan-out orchestration
- heavy AI enrichment
- analytics processing
- complex cross-service mutation chains

The architecture works best when live reads use **precomputed projections** and asynchronous systems do the heavier work elsewhere.

**Associated docs:** [System Architecture](../architecture/04-system-architecture.md), [Ranking: Runtime and Contracts](../services/ranking-engine/02-runtime-and-contracts.md)

### 2. Customer State And Journey State Are Separate On Purpose

Engineering should treat profile and journey state as different concerns:

- **customer profile** = durable, cross-journey facts
- **journey state** = service-specific, time-sensitive progression

This separation is what makes:

- multiple concurrent journeys
- returning-customer re-entry
- active-journey selection
- replayable state rebuilds

practical to implement.

It also prevents the core profile object from becoming an unreadable mixture of stable facts and volatile per-service state.

**Associated docs:** [Customer State Model](../architecture/02-customer-state-model.md), [Customer Profile: State and Persistence](../services/customer-profile-service/01-state-and-persistence.md)

### 3. Deterministic Systems Remain Authoritative

Engineering teams should assume the following stay deterministic and explainable:

- eligibility checks
- suitability checks
- suppression rules
- ranking logic
- business and campaign constraints
- compliance enforcement

AI may support interpretation, retrieval, explanation, and summarization, but it should not become the authority for protected decisions.

This is the core design choice that keeps the platform testable and auditable.

**Associated docs:** [AI and RAG Strategy](../ai/08-ai-and-rag-strategy.md), [Ranking: Scoring Model and Policy](../services/ranking-engine/01-scoring-model-and-policy.md)

### 4. Activity Metadata Describes Options, Not Decision Logic

The activity side of the system should own:

- offers
- provider and campaign copy
- disclosures
- CTA definitions
- metadata that helps retrieval and slot composition

It should not own:

- ranking policy
- suitability enforcement
- active-journey logic
- AI prompt behavior

This keeps activity configuration flexible without making operational logic hard to govern.

**Associated docs:** [Activity Metadata](../services/05-activity-metadata.md), [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md)

### 5. Async Pipelines Are Not Optional

The architecture depends on asynchronous processing for:

- event ingestion
- state projection updates
- telemetry processing
- analytics projections
- embedding refresh
- heavier AI enrichment

If these workloads are pulled into the synchronous request path, latency and operational complexity will climb quickly.

The expected pattern is:

- **fast synchronous reads**
- **bounded in-request computation**
- **asynchronous projection and optimization loops**

**Associated docs:** [System Architecture](../architecture/04-system-architecture.md), [Feedback and Analytics](../operations/10-feedback-and-analytics.md)

### 6. Traceability Is A First-Class Technical Requirement

Engineering should treat traceability as part of the runtime contract, not an analytics afterthought.

The system should be able to explain:

- which journey led the session
- which candidates were retrieved
- which candidates were suppressed and why
- which ranking version produced the result
- which activity metadata revision was shown
- whether AI contributed and which prompt/model version was used

Without this, teams will not be able to debug, govern, or optimize the platform safely.

**Associated docs:** [Feedback and Analytics](../operations/feedback-and-analytics/02-event-model-and-dashboards.md), [AI Runtime and Implementation](../ai/ai-and-rag-strategy/01-runtime-and-implementation.md)

---

## The Runtime Model Engineering Should Assume

### Synchronous Path

The synchronous path should usually involve:

- channel request into the orchestration API
- profile and journey projection reads
- active-journey selection
- activity-backed candidate retrieval
- deterministic filtering and ranking
- lightweight explanation assembly
- response payload back to the caller

This path should remain:

- latency-bounded
- deterministic where decisions matter
- independent of heavy analytical workloads

### Asynchronous Path

The asynchronous path should usually involve:

- event ingestion
- projection rebuilding
- telemetry aggregation
- experiment readouts
- embedding generation or refresh
- heavier summarization and enrichment

This path should be replayable, monitorable, and safe to run independently from live request handling.

---

## Service Boundaries Engineering Should Keep Clean

### Profile Domain

Owns:

- identity linkage
- durable customer facts
- customer-level summaries
- cross-journey customer signals

### Journey Domain

Owns:

- service-specific journey states
- stage, intent, urgency, and resume projections
- qualification evidence
- journey-level summaries and scores

### Decisioning Domain

Owns:

- active-journey selection
- candidate retrieval coordination
- ranking and suppression
- next-best-action assembly

### Content Domain

Owns:

- managed copy
- offer and provider metadata
- CTA definitions and deep-link metadata
- disclosures and content lifecycle state

### Analytics Domain

Owns:

- historical event stream
- reporting projections
- experiment measurement
- optimization readouts

These boundaries are important because they keep storage, contracts, ownership, and failure behavior easier to reason about.

---

## Failure And Fallback Behavior

Engineering readers should assume the system needs safe fallbacks in several places.

### If AI Fails

The platform should:

- preserve the deterministic next-best-action result
- fall back to deterministic or pre-authored explanation text
- emit telemetry showing the AI bypass or rejection

### If Retrieval Returns Weak Candidates

The platform should:

- prefer broad retrieval plus deterministic narrowing
- return the best safe eligible set available
- avoid inventing unsupported recommendations

### If Events Arrive Late

The platform should:

- rely on the latest committed projections in live reads
- support bounded staleness rather than replay on request
- make freshness expectations explicit

### If Activity Metadata Changes Midstream

The platform should:

- version activity metadata revisions
- invalidate affected normalized assets
- preserve traceability for what was actually served

These are architecture decisions as much as operational decisions.

---

## What Engineering Should Build For Early

Before advanced optimization, engineering should make sure the platform already has:

- clean service ownership
- stable request and response contracts
- replayable event handling
- deterministic ranking outputs
- explicit deep-link and disclosure handling
- decision-trace telemetry
- safe AI fallback behavior

These are the foundations that make later AI-forward expansion feasible instead of brittle.

---

## Key Engineering Questions To Resolve Early

Before implementation moves too far, engineering should align on:

### 1. What is the exact live request contract?

Define:

- request payload shape
- required identity and session fields
- response structure
- explanation and CTA payload expectations

### 2. Where are the projection boundaries?

Make explicit:

- what is precomputed
- what is read live
- what is recomputed asynchronously

### 3. How is active-journey selection implemented?

Decide:

- which service owns the decision
- which signals are required
- how conflicts are resolved
- how decision traces are emitted

### 4. Where can AI participate?

Be explicit about:

- interpretation support
- retrieval expansion
- explanation generation
- fallback conditions

### 5. What must always be versioned?

At minimum:

- ranking configuration
- metadata revision
- prompt template version
- model version where applicable
- experiment assignment

---

## Recommended Reading Path

Use this order if you want the clearest engineering narrative:

1. [README](../../README.md) - top-level architecture, core definitions, and platform flow
2. [System Architecture](../architecture/04-system-architecture.md) - service boundaries and runtime topology
3. [Customer Profile Service](../services/06-customer-profile-service.md) - service ownership and projection responsibilities
4. [Ranking Engine](../services/07-ranking-engine.md) - decisioning boundaries and deep-dive map
5. [AI and RAG Strategy](../ai/08-ai-and-rag-strategy.md) - AI boundaries and safe usage
6. [Vector Search Design](../ai/09-vector-search-design.md) - semantic retrieval behavior
7. [Feedback and Analytics](../operations/10-feedback-and-analytics.md) - telemetry architecture and measurement entry point

Then go deeper with:

- [Customer Profile: State and Persistence](../services/customer-profile-service/01-state-and-persistence.md)
- [Customer Profile: Event Processing and APIs](../services/customer-profile-service/02-event-processing-and-apis.md)
- [Ranking: Scoring Model and Policy](../services/ranking-engine/01-scoring-model-and-policy.md)
- [Ranking: Runtime and Contracts](../services/ranking-engine/02-runtime-and-contracts.md)
- [AI Runtime and Implementation](../ai/ai-and-rag-strategy/01-runtime-and-implementation.md)
- [Analytics: Event Model and Dashboards](../operations/feedback-and-analytics/02-event-model-and-dashboards.md)

---

## The Questions These Docs Answer

- Which services own customer facts, journey state, decisioning, and telemetry?
- How do runtime requests stay fast while analytics and enrichment run asynchronously?
- What contracts should exist between orchestration, profile, content, ranking, and analytics layers?
- How do engineering teams keep AI useful without making it authoritative for policy decisions?
- What needs to be versioned, replayable, and traceable from the start?

---

## Summary

Engineering readers should come away seeing the platform as a small number of clear domains coordinated by a bounded live request path and supported by asynchronous state, analytics, and AI support layers.

The key engineering responsibilities are:

- keep service boundaries clean
- keep deterministic decisions authoritative
- keep the synchronous path small
- keep telemetry and traceability first-class
- keep AI useful without letting it become the policy engine

---

| <- Previous | Next -> |
|---|---|
| [Product and Delivery Guide](./product-and-delivery.md) | [Documentation Home](../../README.md#documentation-structure) |
