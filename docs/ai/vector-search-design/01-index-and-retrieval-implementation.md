# Vector Search Design: Index and Retrieval Implementation

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: Vector Search Design](../09-vector-search-design.md) | [Next: Feedback and Analytics ->](../../operations/10-feedback-and-analytics.md)

## Overview

This document covers the implementation detail behind the vector layer: index structure, ingestion flow, query contracts, retrieval tuning, caching, and evaluation.

---

## Suggested Index Schema

The vector index should store enough structure to support both retrieval quality and downstream traceability.

Recommended fields:

| Field | Purpose |
|---|---|
| `assetId` | canonical content reference |
| `chunkId` | unique chunk-level identifier |
| `serviceCategory` | deterministic filtering |
| `provider` | provider-level filtering and reporting |
| `funnelStage` | stage-aware retrieval |
| `region` | regional eligibility filtering |
| `ctaType` | next-step alignment |
| `contentRevision` | traceability back to Contentful publish state |
| `embeddingModelVersion` | embedding refresh and experiment governance |
| `plainText` | keyword fallback and hybrid search |
| `vector` | semantic similarity search |

This keeps retrieval inspectable instead of turning the vector layer into a black box.

---

## Ingestion And Refresh Pipeline

Recommended ingestion flow:

```text
Contentful publish event
        ↓
Normalization and field extraction
        ↓
Chunking and summary preparation
        ↓
Embedding generation
        ↓
Vector index upsert
        ↓
Retrieval smoke checks and telemetry
```

Implementation guidance:

- separate normalization from embedding generation so retries do not re-fetch content unnecessarily
- upsert by `chunkId` so partial document refresh is possible
- refresh only changed chunks when the content revision changes
- emit index-health telemetry for failed embedding or indexing operations

---

## Suggested Query Contract

A concrete retrieval contract helps make the search boundary easier to implement and debug.

```json
{
  "queryText": "family cover with extras",
  "activeJourney": {
    "serviceCategory": "health_insurance",
    "stage": "quote_ready"
  },
  "filters": {
    "region": "NSW",
    "provider": null
  },
  "retrievalOptions": {
    "topK": 20,
    "keywordWeight": 0.35,
    "vectorWeight": 0.65
  }
}
```

Suggested response shape:

```json
{
  "results": [
    {
      "assetId": "offer-health-family-001",
      "chunkId": "offer-health-family-001#chunk-02",
      "vectorScore": 0.82,
      "keywordScore": 0.31,
      "serviceCategory": "health_insurance",
      "contentRevision": "offer-health-family-001@17"
    }
  ]
}
```

This provides enough traceability for downstream ranking and analytics without exposing raw index internals to every caller.

---

## Retrieval Tuning Strategy

Hybrid retrieval should be tuned explicitly rather than left to default engine behavior.

Recommended tuning levers:

- keyword versus vector weighting by use case
- `topK` size before deterministic reranking
- chunk size and overlap policy
- metadata filter strictness by channel and stage
- recall thresholds for secondary-journey retrieval

Typical pattern:

- use tighter filters and smaller `topK` for quote-ready journeys
- use broader recall for exploratory, conversational, or research journeys
- keep secondary-journey retrieval gated behind explicit product rules

---

## Caching Guidance

Recommended cache boundaries:

- cache query embeddings briefly for repeated same-session requests
- cache retrieval results only when journey, region, and content revision inputs have not changed
- avoid long-lived caches for volatile quote-ready or urgency-sensitive journeys

This reduces repeated embedding and search cost without allowing stale retrieval context to dominate live decisions.

---

## Evaluation And Quality Checks

The vector layer should be measured using:

- recall of known-good assets for representative queries
- proportion of retrieved assets later suppressed by deterministic rules
- grounding usefulness in downstream AI responses
- retrieval latency by query type
- retrieval quality by vertical, journey stage, and returning-customer scenario

If too many retrieved items are later suppressed, the issue is often poor metadata filtering or loose chunk/index design rather than ranking alone.

---

## Summary

The implementation goal is a retrieval layer that is observable, tunable, and safe enough to support AI-assisted experiences without weakening deterministic controls.

---

| <- Previous | Next -> |
|---|---|
| [Vector Search Design](../09-vector-search-design.md) | [Feedback and Analytics](../../operations/10-feedback-and-analytics.md) |
