# System Architecture

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Activity Metadata ->](../services/05-activity-metadata.md)

## Overview

This document defines the technical architecture for a multi-vertical lead-generation platform that personalizes offers and next-best actions across service categories.

The architecture assumes:

- one durable customer profile entity
- multiple concurrent journey states per customer
- an active-journey selection step during live decisioning

---

## Core Services

### Lead Profile Service

Responsibilities:

- maintain the durable customer profile
- aggregate behavioral and declared data
- maintain multiple journey states
- calculate journey-level scores
- expose profile, journey, intent, and qualification views

Suggested technology:

- .NET
- Cosmos DB

---

### Personalization Service

Responsibilities:

- determine active service interest
- retrieve candidate activities, offers, and content
- coordinate eligibility and suitability checks
- compose the recommendation response

Suggested architecture:

- CQRS-friendly service boundaries
- isolated domain services per decisioning concern

---

### Ranking And Suitability Engine

Responsibilities:

- score candidate offers and CTAs
- enforce deterministic business constraints
- prioritize the next best actions
- explain why items were promoted or suppressed

---

### Activity Metadata Adapter

Responsibilities:

- query existing activities and attached metadata
- normalize activity entities into domain models
- surface lifecycle, expiry, and metadata changes to downstream services

Suggested implementation:

- REST or GraphQL client over the existing activity source
- infrastructure adapter layer

---

### Analytics And Feedback Pipeline

Responsibilities:

- collect interaction and conversion events
- build projections for lead quality and provider performance
- feed optimization signals back into the decisioning stack

---

## Runtime Topology

At implementation time, the platform can be split into a small number of clear runtime components.

| Component | Role | Typical interaction pattern |
|---|---|---|
| Experience orchestration API | Entry point for web, app, and assisted-sales channels | synchronous request/response |
| Customer profile service | Reads and updates customer profile and journey state | synchronous reads, asynchronous writes |
| Activity metadata adapter | Retrieves and normalizes activities for decisioning | synchronous reads plus change-driven invalidation |
| Ranking engine | Scores and orders candidates | synchronous in-request execution |
| Analytics pipeline | Processes behavior and outcome events | asynchronous event processing |
| AI interpretation layer | Supports journey interpretation, retrieval expansion, and explanations | synchronous for lightweight use cases, asynchronous for heavier enrichment |

This keeps the live request path small while allowing enrichment and analytics to scale independently.

---

## Session Flow

The typical live request path is:

1. load customer profile and journey states
2. select the active journey
3. update intent and urgency signals
4. check eligibility and availability
5. retrieve candidate offers and content
6. rank and filter for suitability
7. return the personalized experience

For session-triggered personalization, the profile and journey reads should use the latest committed projections, or a bounded-staleness equivalent with an explicit freshness target, before candidate retrieval and ranking continue.

---

## Live Request Guardrails

- read the latest committed profile and journey projections, or a bounded-staleness equivalent with an explicit freshness target
- keep the synchronous path limited to the orchestrator, profile reads, retrieval, ranking, and lightweight AI-supported explanation
- move analytics, replay, and non-critical enrichment off the request thread
- prefer precomputed projections over repeated raw event scans

---

## Suggested Service Contracts

The docs do not need to prescribe final API shapes, but a concrete contract model helps make the architecture implementable and gives channels a clear entry point.

| Interaction | Suggested shape | Notes |
|---|---|---|
| Channel -> Orchestrator | `POST /personalization/resolve` | includes customer identity, session context, and optional query text |
| Orchestrator -> Profile service | `GET /customers/{id}` and `GET /customers/{id}/journeys` | read current profile and candidate journeys |
| Orchestrator -> Activity metadata adapter | `POST /activities/query` | request broad candidate set by active-journey context |
| Orchestrator -> Ranking engine | `POST /ranking/score` | pass profile, active journey, candidates, and context |
| Channel -> Event collection | `POST /events` | emit behavior, impression, and outcome events asynchronously |

Example orchestration request:

```json
{
  "customerId": "12345",
  "sessionId": "session-001",
  "channel": "web",
  "entryPoint": "paid_search",
  "currentUrl": "/health-insurance/compare",
  "queryText": "family cover with extras",
  "region": "NSW"
}
```

---

## Service Boundaries

### Profile Domain

Owns:

- customer identity linkage
- stable customer facts
- customer-level return behavior signals

### Journey Domain

Owns:

- service-specific journey states
- journey intent and stage projections
- journey qualification evidence
- resume and renewal indicators
- journey-level scores

### Decisioning Domain

Owns:

- active-journey selection
- candidate retrieval
- ranking
- suitability constraints
- campaign priority handling
- next-best-action assembly

### Activity Domain

Owns:

- customer-visible activity definitions
- provider and offer metadata
- disclosure references and CTA definitions
- lifecycle status of activities

### Analytics Domain

Owns:

- event history
- optimization projections
- funnel reporting
- experiment measurement

---

## Data And State Placement

Recommended state placement:

| State | Primary owner | Why |
|---|---|---|
| Durable customer facts | Profile domain | reused across all journeys |
| Service-specific journey state | Journey domain | changes faster and needs independent progression |
| Activities and metadata | Activity domain | display and governance state for candidate retrieval |
| Ranking configuration | Decisioning domain | operational control and explainability |
| Historical event stream | Analytics domain / event infrastructure | replay and measurement |

This keeps storage decisions aligned with service ownership.

---

## Architectural Principles

- keep business logic server-side
- keep rendering channels lightweight
- separate profile, journey, and decisioning concerns
- separate qualification from presentation
- make suitability and compliance rules explicit
- support asynchronous analytics processing
- enable vertical rollout through configuration, not cloned services

---

| <- Previous | Next -> |
|---|---|
| [Content Personalization Strategy](./03-content-personalization-strategy.md) | [Activity Metadata](../services/05-activity-metadata.md) |
