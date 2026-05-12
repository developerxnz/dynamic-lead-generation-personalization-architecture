# System Architecture

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Contentful Integration ->](../services/05-contentful-integration.md)

## Overview

This document defines the technical architecture for a multi-vertical lead-generation platform that personalizes offers and next-best actions across service categories.

---

## Core Services

### Lead Profile Service

Responsibilities:

- maintain unified lead state
- aggregate behavioral and declared data
- calculate lead scores
- expose profile, intent, and eligibility views

Suggested technology:

- .NET
- Cosmos DB

---

### Personalization Service

Responsibilities:

- determine active service interest
- retrieve candidate offers and content
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

### Contentful Adapter

Responsibilities:

- query managed content and offer metadata
- normalize CMS entities into domain models
- surface publish, expiry, and metadata changes to downstream services

Suggested implementation:

- GraphQL client
- infrastructure adapter layer

---

### Analytics And Feedback Pipeline

Responsibilities:

- collect interaction and conversion events
- build projections for lead quality and provider performance
- feed optimization signals back into the decisioning stack

---

## Session Flow

```text
Customer Session Starts
   ↓
Load Lead Profile
   ↓
Update Intent / Urgency Signals
   ↓
Check Eligibility And Availability
   ↓
Retrieve Candidate Offers And Content
   ↓
Rank And Filter For Suitability
   ↓
Return Personalized Experience
```

For session-triggered personalization, `Load Lead Profile` should use the latest committed lead projection, or a bounded-staleness equivalent with an explicit freshness target, before candidate retrieval and ranking continue.

---

## Service Boundaries

### Profile Domain

Owns:

- customer identity linkage
- service interests
- lead score
- intent and stage projections
- eligibility evidence

### Decisioning Domain

Owns:

- candidate retrieval
- ranking
- suitability constraints
- campaign priority handling
- next-best-action assembly

### Content Domain

Owns:

- managed copy
- provider and offer metadata
- disclosure content
- lifecycle status of content assets

### Analytics Domain

Owns:

- event history
- optimization projections
- funnel reporting
- experiment measurement

---

## Architectural Principles

- keep business logic server-side
- keep rendering channels lightweight
- separate qualification from presentation
- make suitability and compliance rules explicit
- support asynchronous analytics processing
- enable vertical rollout through configuration, not cloned services

---

| <- Previous | Next -> |
|---|---|
| [Content Personalization Strategy](./03-content-personalization-strategy.md) | [Contentful Integration](../services/05-contentful-integration.md) |
