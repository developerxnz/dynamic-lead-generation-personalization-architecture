# Ranking Engine

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: AI and RAG Strategy ->](../ai/08-ai-and-rag-strategy.md)

## Overview

The Ranking Engine is the core decisioning component of the platform.

It determines which offers, content items, and CTAs are shown to a lead after candidate retrieval and suitability screening.

Its purpose is to:

> maximize qualified lead conversion in an explainable, deterministic way

---

# Goals

The ranking engine should:

- produce ordered recommendations
- optimize for qualified conversion likelihood
- remain deterministic and explainable
- support configurable business rules
- integrate with lead state and behavioral signals
- allow future experimentation and AI augmentation

---

# Core Responsibilities

The ranking engine is responsible for:

- scoring candidate assets
- applying business and suitability rules
- ordering results
- explaining ranking decisions
- supporting configuration-based tuning
- ensuring consistency across similar sessions

---

# Inputs To The Ranking Engine

## 1. Lead State

From the Customer Profile Service:

- service interests
- household and employment attributes
- engagement level
- funnel stage
- lead score
- urgency and renewal window
- eligibility evidence

---

## 2. Content And Offer Metadata

From Contentful or the offer catalog:

- service_category
- subtype
- provider
- region
- conversion_goal
- cta_type
- compliance_flags
- freshness
- priority

---

## 3. Behavioral Signals

From analytics systems:

- clicks
- impressions
- quote starts
- quote completion
- callback requests
- content interactions
- provider handoff outcomes

---

## 4. Context Signals

- session source
- device type
- referral partner
- time sensitivity
- campaign context

---

# Scoring Model

## Example Weighted Model

Each candidate is scored using weighted signals:

```text
Total Score =
  Category Match Score
+ Intent Alignment Score
+ Eligibility Fit Score
+ Funnel Alignment Score
+ Behavioral Relevance Score
+ CTA Likelihood Score
+ Commercial Priority Score
+ Freshness Score
```

---

## Example Weights

| Signal | Weight |
|---|---|
| Eligibility fit | 7 |
| Intent alignment | 6 |
| Funnel alignment | 5 |
| CTA likelihood | 5 |
| Behavioral relevance | 4 |
| Category match | 4 |
| Commercial priority | 2 |
| Freshness | 2 |

---

## Example Calculation

```text
Candidate A:
- Eligibility fit: +7
- Intent alignment: +6
- Funnel alignment: +5
- CTA likelihood: +5
- Category match: +4

Total = 27
```

---

# Ranking Strategy

## Step 1: Normalization

Standardize all inputs:

- scale scores consistently
- remove bias between sources
- normalize behavioral metrics

---

## Step 2: Suitability Screening

Before ranking, remove or suppress candidates that fail:

- region availability
- product or provider suitability
- compliance requirements
- hard eligibility rules

---

## Step 3: Feature Scoring

Convert raw inputs into:

- relevance scores
- qualification confidence
- conversion likelihood indicators

---

## Step 4: Weighted Aggregation

Combine features into a single score per candidate.

---

## Step 5: Sorting

Order content by:

```text
Highest score -> Lowest score
```

Apply tie-breakers such as:

- freshness
- campaign priority
- provider diversity

---

# Ranking Principles

## 1. Deterministic First

The ranking engine must:

- always return the same result for the same inputs
- avoid hidden randomness
- avoid opaque ML dependencies early on

---

## 2. Explainability

Every ranked item should be explainable.

Example:

> ranked high because it matched current service intent, passed eligibility checks, and aligned to a quote-ready stage

---

## 3. Configurable Logic

Scoring should be configurable via:

- configuration files
- database-driven rules
- feature flags

Avoid hardcoding weights.

---

## 4. Separation Of Concerns

The ranking engine should not:

- retrieve content
- infer intent authoritatively
- perform AI reasoning
- own CMS logic

It should only:

> rank a provided candidate set after deterministic constraints are applied

---

# Ranking Architecture

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

# Diversity And Business Rules

## Diversity Controls

To avoid repetitive results:

- limit repeated providers
- balance CTA types
- introduce category spread where cross-sell is appropriate

---

## Business Rules Examples

- prioritize high-value campaigns when relevance remains acceptable
- exclude expired or withdrawn offers
- suppress providers with temporary operational issues
- boost renewal or churn-save content during renewal windows

---

# Explainability Model

Each ranked item should return:

```json
{
  "contentId": "offer-123",
  "score": 27,
  "reasons": [
    "Category match: health_insurance",
    "Intent alignment: comparing providers",
    "Eligibility fit: approved for quote flow",
    "CTA alignment: get_quote"
  ]
}
```

---

# Performance Considerations

## Requirements

The ranking engine must:

- execute in low latency (<100ms ideal)
- scale horizontally
- support caching of computed features
- avoid expensive external calls during ranking

---

## Optimization Strategies

- precompute behavioral aggregates
- cache lead profiles by profile version
- cache content metadata by publish version
- avoid recomputing static scores

---

## Cache Boundaries And Invalidation

The ranking engine should cache inputs and derived features, not assume a single ranking result stays valid across materially different sessions.

Recommended rules:

- invalidate profile-dependent feature caches when the profile version changes
- invalidate content-dependent caches when offers, disclosures, or eligibility references change
- shorten TTLs when urgency, renewal, or serviceability signals are volatile

---

# Summary

The Ranking Engine is the decision layer of the platform.

It is responsible for:

- selecting the best next actions
- applying business logic
- optimizing for qualified lead outcomes
- ensuring explainability and consistency

The long-term vision is a hybrid system:

> deterministic ranking core + AI-assisted optimization layer

---

| <- Previous | Next -> |
|---|---|
| [Customer Profile Service](./06-customer-profile-service.md) | [AI and RAG Strategy](../ai/08-ai-and-rag-strategy.md) |
