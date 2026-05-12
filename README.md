# Dynamic Lead Generation Personalization Platform

## Overview

This repository contains the architecture, implementation guidance, and strategic design documents for a multi-vertical lead-generation personalization platform.

The platform is designed for service businesses such as:

- novated leasing
- health insurance
- broadband and utilities
- adjacent quote-driven or application-driven service categories

It is designed to:

- re-evaluate customer intent on every meaningful session
- personalize offers, content, and next-best actions dynamically
- optimize for qualified lead conversion rather than raw engagement alone
- enforce eligibility and suitability rules before promotion
- support future AI augmentation without losing explainability

The reference solution is designed around:

- .NET services
- Azure hosting and integration services
- Cosmos DB for operational profile storage
- Contentful for managed content and offer metadata
- Azure OpenAI for optional AI augmentation
- Azure AI Search for semantic retrieval

---

# Architecture Goals

The platform aims to:

- identify which service category a customer is most likely to convert in
- tailor journeys for research, comparison, quote, application, and renewal scenarios
- support multiple verticals through configuration instead of separate architectures
- optimize for qualified lead volume, lead quality, and downstream conversion
- keep ranking and compliance logic explicit and explainable
- separate content and offer management from decisioning logic

---

# High-Level Architecture

```text
Customer Session / Trigger
   ↓
Lead Profile Service
   ↓
Intent + Eligibility Evaluation
   ↓
CMS / Offer Catalog Query
   ↓
Ranking + Suitability Engine
   ↓
Next Best Action Selection
   ↓
Web / App / Assisted Sales Experience
```

---

# Documentation Structure

## Architecture

| Document | Description |
|---|---|
| [01-overview.md](./docs/architecture/01-overview.md) | Platform vision, business goals, and multi-vertical lead-generation framing |
| [02-customer-state-model.md](./docs/architecture/02-customer-state-model.md) | Lead profile, intent, eligibility, and lead-scoring model |
| [03-content-personalization-strategy.md](./docs/architecture/03-content-personalization-strategy.md) | Offer, content, and CTA selection strategy |
| [04-system-architecture.md](./docs/architecture/04-system-architecture.md) | Technical architecture, service boundaries, and decisioning flow |

---

## Services

| Document | Description |
|---|---|
| [05-contentful-integration.md](./docs/services/05-contentful-integration.md) | CMS and offer metadata design for multi-vertical lead generation |
| [06-customer-profile-service.md](./docs/services/06-customer-profile-service.md) | Lead profile service implementation guidance |
| [07-ranking-engine.md](./docs/services/07-ranking-engine.md) | Ranking, suitability, and qualified conversion scoring |

---

## AI

| Document | Description |
|---|---|
| [08-ai-and-rag-strategy.md](./docs/ai/08-ai-and-rag-strategy.md) | AI boundaries, RAG strategy, and assistive use cases |
| [09-vector-search-design.md](./docs/ai/09-vector-search-design.md) | Semantic retrieval for offers, guides, and lead-support journeys |

---

## Operations

| Document | Description |
|---|---|
| [10-feedback-and-analytics.md](./docs/operations/10-feedback-and-analytics.md) | Behavioral analytics, lead-quality measurement, and optimization loops |

---

## Delivery

| Document | Description |
|---|---|
| [11-roadmap.md](./docs/delivery/11-roadmap.md) | Phased rollout across deterministic, behavioral, and AI-assisted capabilities |

---

# Recommended Delivery Approach

## Phase 1 — Deterministic Lead Personalization

Initial implementation should focus on:

- unified lead profiles
- service metadata and offer taxonomy
- deterministic ranking with suitability guardrails
- personalized next-best actions for each vertical

This provides:

- explainability
- maintainability
- predictable compliance behavior
- faster rollout across service categories

---

## Phase 2 — Behavioral Optimization

Enhance personalization using:

- behavioral tracking
- intent refinement
- lead-quality analytics
- configurable ranking signals by vertical

---

## Phase 3 — AI-Augmented Experiences

Introduce:

- AI-assisted intent interpretation
- semantic retrieval
- conversational guidance
- personalized summaries and explanations
- RAG-based discovery for complex service journeys

AI should augment rather than replace deterministic decisioning.

---

# Architectural Principles

## Keep Content And Offers Separate From Decisioning

The CMS and offer catalog should remain responsible for managed assets and metadata only.

Personalization, eligibility, suitability, and ranking logic should live within dedicated backend services.

---

## Optimize For Qualified Conversion

The platform should optimize for:

- quote starts
- application progression
- callback requests
- qualified lead handoff
- downstream activation or policy conversion

Raw engagement alone is not enough.

---

## Treat Every Session As A Re-Evaluation Event

Customer intent changes over time.

Each meaningful session should:

- refresh customer state
- update intent and urgency signals
- re-rank offers and supporting content
- adjust the next best action to current conversion likelihood

---

# Recommended Technology Stack

| Area | Technology |
|---|---|
| Experience and decisioning services | .NET |
| Operational profile storage | Cosmos DB |
| CMS / offer metadata | Contentful |
| AI services | Azure OpenAI |
| Semantic search | Azure AI Search |
| APIs | GraphQL + REST |
| Hosting | Azure |

---

# Future Considerations

Potential future enhancements include:

- vertical-specific experimentation frameworks
- predictive lead quality models
- hybrid semantic + deterministic ranking
- provider-specific optimization strategies
- conversational quote-assistance journeys
- cross-sell and renewal propensity modeling

---

# Summary

This platform is designed as a multi-vertical lead-generation architecture that combines:

- deterministic decisioning
- behavioral optimization
- structured content and offer metadata
- incremental AI augmentation

The architecture prioritizes:

- lead quality
- explainability
- scalability across verticals
- compliance-aware promotion
- long-term extensibility
