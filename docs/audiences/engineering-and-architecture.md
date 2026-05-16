# Engineering and Architecture Guide

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: Product and Delivery Guide <-](./product-and-delivery.md)

## Overview

This reading path is for **engineering, architecture, and data stakeholders** who need to understand:

- runtime boundaries and system responsibilities
- profile, journey, and event-processing models
- decisioning and ranking contracts
- analytics and AI integration points

It assumes the reader wants the deepest implementation detail in the repo.

---

## Start Here

1. [README](../../README.md) - top-level architecture and technology choices
2. [System Architecture](../architecture/04-system-architecture.md) - runtime topology and service boundaries
3. [Customer Profile Service](../services/06-customer-profile-service.md) - service overview and deep-dive map
4. [Ranking Engine](../services/07-ranking-engine.md) - decisioning overview and deep-dive map
5. [AI and RAG Strategy](../ai/08-ai-and-rag-strategy.md) - AI boundaries and retrieval use cases
6. [Vector Search Design](../ai/09-vector-search-design.md) - semantic retrieval design
7. [Feedback and Analytics](../operations/10-feedback-and-analytics.md) - telemetry architecture and measurement entry point

---

## Engineering Deep Dives

- [Customer Profile: State and Persistence](../services/customer-profile-service/01-state-and-persistence.md)
- [Customer Profile: Event Processing and APIs](../services/customer-profile-service/02-event-processing-and-apis.md)
- [Ranking: Scoring Model and Policy](../services/ranking-engine/01-scoring-model-and-policy.md)
- [Ranking: Runtime and Contracts](../services/ranking-engine/02-runtime-and-contracts.md)
- [Analytics: Event Model and Dashboards](../operations/feedback-and-analytics/02-event-model-and-dashboards.md)

---

## The Questions These Docs Answer

- Which services own customer facts, journey state, decisioning, and telemetry?
- How do runtime requests stay fast while analytics and enrichment run asynchronously?
- What contracts should exist between orchestration, profile, content, ranking, and analytics layers?
- How do engineering teams keep AI useful without making it authoritative for policy decisions?

---

## Summary

Engineering readers can stay in the technical architecture and service sections without losing the business context, while still having deeper sub-pages for contracts, state, and event-processing detail.

---

| <- Previous | Next -> |
|---|---|
| [Product and Delivery Guide](./product-and-delivery.md) | [Documentation Home](../../README.md#documentation-structure) |
