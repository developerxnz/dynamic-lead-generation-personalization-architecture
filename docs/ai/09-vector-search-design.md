# Vector Search Design

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Feedback and Analytics ->](../operations/10-feedback-and-analytics.md)

## Overview

Vector search enables semantic retrieval of content based on meaning rather than exact keyword matching.

In this platform, vector search is used to support:

- semantic content discovery
- AI-enhanced personalization
- conversational experiences (RAG)
- hybrid search alongside deterministic ranking

Vector search should **augment** deterministic ranking, not replace it.

---

# Goals

The vector search layer should:

- improve content relevance through semantic understanding
- support natural language queries
- enhance RAG-based experiences
- enable similarity-based content discovery
- complement metadata-based filtering
- remain optional in the core personalization flow

---

# High-Level Architecture

```text
Contentful Content
        ↓
Embedding Generation (Azure OpenAI)
        ↓
Vector Index (Azure AI Search)
        ↓
Retrieval Layer
        ↓
Personalization Service
        ↓
Ranking Engine (Deterministic)
        ↓
Final Content Selection
```

---

# Core Concepts

## Embeddings

Embeddings are numerical representations of content meaning.

They are generated from:

- content body
- title
- metadata
- topics
- CTA context
- summaries

These embeddings allow semantic comparison between:

- user intent
- content items
- queries

---

## Vector Index

A vector index stores:

- embeddings
- metadata fields
- searchable text fields

Recommended platform:

- Azure AI Search

---

## Hybrid Search

The recommended approach is **hybrid search**, combining:

- keyword search (BM25)
- vector similarity search
- metadata filtering

This ensures both:

- precision (keywords + filters)
- relevance (semantic matching)

---

# Embedding Strategy

## What to Embed

Each content item should generate embeddings from:

- title
- description
- full content body
- topics
- persona tags
- funnel stage
- CTA description

---

## Chunking Strategy

For long-form content:

- split into semantic chunks
- generate embeddings per chunk
- store chunk-level references
- reassemble results at query time

---

## Update Strategy

Embeddings should be updated when:

- content is published
- content is significantly modified
- metadata changes affect meaning

Avoid embedding regeneration on every minor edit.

---

# Query Flow

## Semantic Retrieval Flow

```text
User Query or Intent
        ↓
Generate Query Embedding
        ↓
Vector Search (Azure AI Search)
        ↓
Retrieve Candidate Content
        ↓
Apply Metadata Filters
        ↓
Pass to Ranking Engine
```

---

## Personalization-Aware Retrieval

Vector search should incorporate:

- customer attributes
- intent signals
- funnel stage
- engagement history

Example:

> “Senior .NET engineer improving deployment speed”

This is transformed into:

- embedding query
- metadata filters (persona, topic, experience level)

---

# Hybrid Retrieval Strategy

The recommended approach:

## Step 1: Filter

Apply deterministic filters:

- persona match
- funnel stage alignment
- experience level
- topic constraints

## Step 2: Vector Search

Apply semantic similarity on filtered dataset.

## Step 3: Rerank

Use deterministic ranking engine to finalize ordering.

---

# Ranking vs Vector Search Responsibilities

## Vector Search

Responsible for:

- semantic relevance
- similarity matching
- contextual retrieval
- candidate generation

---

## Ranking Engine

Responsible for:

- business logic
- conversion optimization
- scoring
- explainability
- final ordering

---

# Integration with Personalization System

```text
Customer State
        ↓
Intent Inference
        ↓
Vector Query Generation
        ↓
Vector Retrieval (Azure AI Search)
        ↓
Candidate Content Set
        ↓
Deterministic Ranking Engine
        ↓
Personalized Output
```

---

# RAG Integration

Vector search is a core component of RAG workflows.

## RAG Flow

```text
User Question
        ↓
Vector Retrieval
        ↓
Context Injection
        ↓
LLM Response Generation (Azure OpenAI)
```

---

## Use Cases for RAG

- onboarding assistance
- product explanations
- dynamic help content
- conversational discovery
- contextual recommendations

---

## RAG Boundaries

RAG should NOT be used for:

- lead scoring
- deterministic ranking
- conversion decisioning
- business rules enforcement

---

# Performance Considerations

## Latency Optimization

- precompute embeddings
- cache frequent queries
- limit vector search scope with filters
- avoid full index scans

---

## Index Design

Recommended structure:

| Field | Purpose |
|---|---|
| embedding vector | semantic search |
| content_id | lookup reference |
| persona tags | filtering |
| funnel stage | filtering |
| topics | filtering |
| raw content | fallback retrieval |

---

# Observability

Monitor:

- query latency
- retrieval accuracy
- vector match relevance
- ranking impact
- conversion outcomes

---

# Limitations

Vector search should NOT be relied on for:

- strict filtering logic
- deterministic business decisions
- compliance-critical rules
- pricing or contractual logic

---

# Future Enhancements

Potential improvements:

- personalized embeddings per user segment
- multi-vector representations per content
- semantic reranking models
- cross-session memory retrieval
- adaptive embedding tuning
- multimodal embeddings (text + image)

---

# Summary

Vector search introduces semantic understanding into the personalization platform.

When combined with:

- deterministic ranking
- customer state modeling
- behavioral analytics

it enables a powerful hybrid system that supports:

- scalable personalization
- AI-enhanced experiences
- intelligent content discovery
- conversational interfaces

The key principle is:

> Vector search generates candidates. Ranking decides outcomes.

---

| <- Previous | Next -> |
|---|---|
| [AI and RAG Strategy](./08-ai-and-rag-strategy.md) | [Feedback and Analytics](../operations/10-feedback-and-analytics.md) |
