# Vector Search Design

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Feedback and Analytics ->](../operations/10-feedback-and-analytics.md)

## Overview

Vector search enables semantic retrieval of offers and content based on meaning rather than exact keyword matching.

In this platform, vector search is used to support:

- semantic offer discovery
- AI-enhanced personalization
- conversational experiences
- hybrid retrieval alongside deterministic ranking

Vector search should augment deterministic decisioning, not replace it.

---

# Goals

The vector search layer should:

- improve relevance for natural-language questions
- support large cross-vertical content catalogs
- enhance RAG-based experiences
- enable similarity-based discovery
- complement metadata-based filtering
- remain optional in the core deterministic flow

---

# High-Level Architecture

```text
Managed Offers And Content
        ↓
Embedding Generation
        ↓
Vector Index
        ↓
Retrieval Layer
        ↓
Personalization Service
        ↓
Ranking Engine (Deterministic)
        ↓
Final Recommendation Set
```

---

# Core Concepts

## Embeddings

Embeddings are numerical representations of meaning.

They are generated from:

- titles
- body content
- metadata
- provider descriptions
- CTA context
- summaries

These embeddings allow semantic comparison between:

- user intent
- offers and guides
- free-text queries

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

The recommended approach is hybrid search, combining:

- keyword search
- vector similarity search
- metadata filtering

This ensures both:

- precision
- semantic relevance

---

# Embedding Strategy

## What To Embed

Each content item should generate embeddings from:

- title
- short description
- long-form body
- service category
- provider context
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
- content is materially modified
- metadata changes alter meaning

Avoid regeneration on every minor edit.

---

# Query Flow

## Semantic Retrieval Flow

```text
User Query Or Intent
        ↓
Generate Query Embedding
        ↓
Vector Search
        ↓
Retrieve Candidate Content
        ↓
Apply Metadata Filters
        ↓
Pass To Ranking Engine
```

---

## Personalization-Aware Retrieval

Vector search should incorporate:

- service category
- customer attributes
- intent signals
- funnel stage
- region or provider constraints

Example:

> "health cover for a young family with extras"

This is transformed into:

- embedding query
- metadata filters for household fit, category, and current stage

---

# Hybrid Retrieval Strategy

The recommended approach:

## Step 1: Filter

Apply deterministic filters:

- service category
- region
- provider availability
- compliance status

## Step 2: Vector Search

Apply semantic similarity on the filtered dataset.

## Step 3: Rerank

Use the deterministic ranking engine to finalize ordering.

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

- suitability
- conversion optimization
- business constraints
- final ordering

---

# Summary

Vector search improves how the platform discovers relevant content and offers.

The key principle is:

> vector search generates candidates; deterministic ranking decides outcomes

---

| <- Previous | Next -> |
|---|---|
| [AI and RAG Strategy](./08-ai-and-rag-strategy.md) | [Feedback and Analytics](../operations/10-feedback-and-analytics.md) |
