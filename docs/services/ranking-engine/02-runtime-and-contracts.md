# Ranking Engine: Runtime and Contracts

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: Ranking Engine](../07-ranking-engine.md) | [Previous: Scoring Model and Policy <-](./01-scoring-model-and-policy.md)

## Overview

This document covers the runtime contract, ranking steps, response model, and performance boundaries for the Ranking Engine.

---

## Suggested Request Contract

A concrete request shape helps make the ranking boundary implementable.

```json
{
  "customerProfile": {
    "customerId": "12345",
    "leadScore": 78,
    "location": "NSW"
  },
  "activeJourney": {
    "journeyId": "journey-health-001",
    "serviceCategory": "health_insurance",
    "intent": "comparing_providers",
    "stage": "quote_ready"
  },
  "context": {
    "channel": "web",
    "campaignSource": "paid_search"
  },
  "candidates": [
    {
      "contentId": "offer-123",
      "serviceCategory": "health_insurance",
      "ctaType": "get_quote",
      "provider": "Provider A",
      "priority": 2
    }
  ]
}
```

---

## Suggested Runtime Algorithm

```text
for each candidate:
  normalize raw inputs
  apply hard suppression and suitability checks
  compute active-journey fit
  compute intent, funnel, CTA, and behavioral features
  apply configurable weights
  record explanation reasons

apply diversity and slot rules
sort by total score
return ranked set + suppression reasons
```

This is intentionally simple. The platform can grow in sophistication later without losing explainability.

### Runtime Steps

#### Step 1: Normalization

Standardize all inputs:

- scale scores consistently
- remove bias between sources
- normalize behavioral metrics

#### Step 2: Suitability Screening

Before ranking, remove or suppress candidates that fail:

- region availability
- product or provider suitability
- compliance requirements
- hard eligibility rules

The suitability screen should be driven primarily by the active journey's qualification state.

#### Step 3: Feature Scoring

Convert raw inputs into:

- relevance scores
- qualification confidence
- conversion likelihood indicators
- active-journey fit

#### Step 4: Weighted Aggregation

Combine features into a single score per candidate.

#### Step 5: Sorting

Order content by:

```text
Highest score -> Lowest score
```

Apply tie-breakers such as:

- freshness
- campaign priority
- provider diversity
- secondary-journey suppression

---

## Ranking Architecture

```text
Personalization Service
        ↓
Candidate Retrieval Service
        ↓
Suitability Filters
        ↓
Ranking Engine
        ↓
Ranked Recommendations
        ↓
Channel Delivery Layer
```

---

## Explainability Model

Each ranked item should return:

```json
{
  "contentId": "offer-123",
  "score": 27,
  "reasons": [
    "Category match: health_insurance",
    "Active journey fit: health_insurance quote journey",
    "Intent alignment: comparing providers",
    "Eligibility fit: approved for quote flow",
    "CTA alignment: get_quote"
  ]
}
```

### Example Response Contract

```json
{
  "rankedRecommendations": [
    {
      "contentId": "offer-123",
      "score": 32,
      "reasons": [
        "Active journey fit: health_insurance quote journey",
        "Eligibility fit: approved for quote flow",
        "Intent alignment: comparing providers"
      ]
    }
  ],
  "suppressedCandidates": [
    {
      "contentId": "offer-999",
      "reason": "region_unavailable"
    }
  ]
}
```

Returning suppressed candidates is useful for traceability, debugging, and optimization analysis.

---

## Performance Considerations

### Requirements

The ranking engine must:

- execute in low latency (<100ms ideal)
- scale horizontally
- support caching of computed features
- avoid expensive external calls during ranking

### Optimization Strategies

- precompute behavioral aggregates
- cache customer profiles by profile version
- cache journey-state inputs by journey version
- cache content metadata by publish version
- avoid recomputing static scores

### Cache Boundaries And Invalidation

The ranking engine should cache inputs and derived features, not assume a single ranking result stays valid across materially different sessions.

Recommended rules:

- invalidate profile-dependent feature caches when the profile version changes
- invalidate journey-dependent feature caches when the active journey changes
- invalidate content-dependent caches when offers, disclosures, or eligibility references change
- shorten TTLs when urgency, renewal, or serviceability signals are volatile

---

## Summary

The runtime contract should keep ranking fast, traceable, and reusable across channels while making both promoted and suppressed outcomes inspectable.

---

| <- Previous | Next -> |
|---|---|
| [Scoring Model and Policy](./01-scoring-model-and-policy.md) | [AI and RAG Strategy](../../ai/08-ai-and-rag-strategy.md) |
