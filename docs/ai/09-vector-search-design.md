# Vector Search Design

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Feedback and Analytics ->](../operations/10-feedback-and-analytics.md)

## Overview

Vector search enables semantic retrieval of offers and content based on meaning rather than exact keyword matching.

In this platform, managed assets are embedded into a vector index and queried through hybrid retrieval. Vector search should expand and improve candidate discovery, but it should not become the authority on final outcomes.

---

## How To Read This Section

Use this overview page for the retrieval boundary, hybrid query pattern, and multi-journey guidance. For implementation detail, use:

| Detail page | Best for | Covers |
|---|---|---|
| [Index and Retrieval Implementation](./vector-search-design/01-index-and-retrieval-implementation.md) | engineering + data | index schema, ingestion pipeline, query contracts, tuning, caching, and retrieval evaluation |

---

## High-Level Architecture

At a high level, managed offers and content are embedded into a vector index, queried through the retrieval layer, and then passed into personalization and ranking before the final recommendation set is assembled.

---

## Query Pattern

The recommended pattern is:

1. apply deterministic filters such as service category, region, provider availability, and compliance status
2. run vector similarity across the filtered set
3. pass the candidate set to deterministic ranking for final ordering

### Personalization-Aware Retrieval

Vector retrieval should incorporate:

- active journey category
- customer attributes
- intent signals
- funnel stage
- region or provider constraints
- secondary-journey hints where useful

Example:

> "health cover for a young family with extras"

This becomes an embedding query plus metadata filters for household fit, category, and current stage.

---

## Multi-Journey Retrieval

When a customer has multiple journey states, vector search should help retrieval without collapsing those journeys together blindly.

Recommended pattern:

- retrieve primarily against the active journey
- allow secondary-journey retrieval only where explicitly supported
- keep candidate attribution clear so downstream ranking knows why an item entered the set

This makes cross-sell and secondary-journey prompts possible without polluting the primary recommendation set.

---

## Ranking Vs Vector Search Responsibilities

### Vector Search

Responsible for:

- semantic relevance
- similarity matching
- contextual retrieval
- candidate generation

### Ranking Engine

Responsible for:

- suitability
- conversion optimization
- business constraints
- final ordering

The split should remain:

> vector search discovers candidates; ranking decides what is safe and best to show

---

| <- Previous | Next -> |
|---|---|
| [AI and RAG Strategy](./08-ai-and-rag-strategy.md) | [Feedback and Analytics](../operations/10-feedback-and-analytics.md) |
