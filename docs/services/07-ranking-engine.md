# Ranking Engine

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: AI and RAG Strategy ->](../ai/08-ai-and-rag-strategy.md)

## Overview

The Ranking Engine is the core decisioning component of the platform.

It determines which offers, content items, and CTAs are shown to a lead after candidate retrieval and suitability screening.

It assumes the customer may have multiple journey states, but ranking should operate against the **active journey selected for the current session**.

Its purpose is to:

> maximize qualified lead conversion in an explainable, deterministic way

---

# Goals

The ranking engine should:

- produce ordered recommendations
- optimize for qualified conversion likelihood
- remain deterministic and explainable
- support configurable business rules
- integrate with customer-profile, journey-state, and behavioral signals
- allow future experimentation and AI-assisted relevance support

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

## 1. Customer Profile And Active Journey

From the Customer Profile Service:

- household and employment attributes
- engagement level
- customer-level lead score
- active journey stage
- active journey intent
- urgency and renewal window
- qualification evidence
- returning-customer signals

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

## 5. AI-Assisted Signals

AI can contribute:

- semantic relevance support
- inferred intent support
- explanation hints
- likely next-question signals

These inputs should improve ranking quality, but they should remain subordinate to deterministic policy controls.

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

# Scoring Model

## Example Weighted Model

Each candidate is scored using weighted signals:

```text
Total Score =
  Category Match Score
+ Active Journey Fit Score
+ Intent Alignment Score
+ Eligibility Fit Score
+ Funnel Alignment Score
+ Behavioral Relevance Score
+ CTA Likelihood Score
+ AI Relevance Support Score
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
| Active journey fit | 5 |
| Behavioral relevance | 4 |
| Category match | 4 |
| AI relevance support | 3 |
| Commercial priority | 2 |
| Freshness | 2 |

---

## Example Calculation

```text
Candidate A:
- Eligibility fit: +7
- Active journey fit: +5
- Intent alignment: +6
- Funnel alignment: +5
- CTA likelihood: +5
- Category match: +4

Total = 32
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

The suitability screen should be driven primarily by the active journey's qualification state.

---

## Step 3: Feature Scoring

Convert raw inputs into:

- relevance scores
- qualification confidence
- conversion likelihood indicators
- active-journey fit

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
- secondary-journey suppression

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

## Example Config Shape

```json
{
  "vertical": "health_insurance",
  "weights": {
    "eligibilityFit": 7,
    "intentAlignment": 6,
    "funnelAlignment": 5,
    "ctaLikelihood": 5,
    "activeJourneyFit": 5,
    "behavioralRelevance": 4,
    "categoryMatch": 4,
    "aiRelevanceSupport": 3,
    "commercialPriority": 2,
    "freshness": 2
  },
  "diversityRules": {
    "maxSameProviderInTop3": 1
  }
}
```

This makes it easier to tune by vertical without changing code paths.

---

## 4. Separation Of Concerns

The ranking engine should not:

- retrieve content
- infer intent authoritatively
- own free-form AI interpretation
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
- allow carefully positioned secondary-journey prompts without displacing the active journey

---

# Explainability Model

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

---

## Example Response Contract

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
- cache customer profiles by profile version
- cache journey-state inputs by journey version
- cache content metadata by publish version
- avoid recomputing static scores

---

## Cache Boundaries And Invalidation

The ranking engine should cache inputs and derived features, not assume a single ranking result stays valid across materially different sessions.

Recommended rules:

- invalidate profile-dependent feature caches when the profile version changes
- invalidate journey-dependent feature caches when the active journey changes
- invalidate content-dependent caches when offers, disclosures, or eligibility references change
- shorten TTLs when urgency, renewal, or serviceability signals are volatile

---

# Summary

The Ranking Engine is the decision layer of the platform.

It is responsible for:

- selecting the best next actions
- applying business logic
- choosing results that best fit the active journey
- optimizing for qualified lead outcomes
- ensuring explainability and consistency

The long-term vision is a hybrid system:

> deterministic ranking core + AI-assisted optimization layer

---

| <- Previous | Next -> |
|---|---|
| [Customer Profile Service](./06-customer-profile-service.md) | [AI and RAG Strategy](../ai/08-ai-and-rag-strategy.md) |
