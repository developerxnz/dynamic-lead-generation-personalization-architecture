# Ranking Engine: Scoring Model and Policy

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: Ranking Engine](../07-ranking-engine.md) | [Next: Runtime and Contracts ->](./02-runtime-and-contracts.md)

## Overview

This document covers the ranking inputs, weighted model, and configurable policy controls used to order offers, content, and CTAs.

---

## Inputs To The Ranking Engine

### 1. Customer Profile And Active Journey

From the Customer Profile Service:

- household and employment attributes
- engagement level
- active journey stage
- active journey intent
- urgency and renewal window
- qualification evidence
- returning-customer signals

### 2. Activity Metadata

From metadata attached to existing activities:

- service_category
- conversion_goal
- cta_type
- funnel stage
- household fit
- metadata revision for traceability

### 3. Behavioral Signals

From the selected active journey:

- saved-quote/resume state
- serviceability and qualification state
- journey score
- urgency
- active versus secondary journey status

### 4. Context Signals

- session source
- device type
- referral partner
- time sensitivity
- campaign context

### 5. AI-Assisted Signals

AI can contribute:

- semantic relevance support
- inferred intent support
- explanation hints
- likely next-question signals

These inputs should improve ranking quality, but they should remain subordinate to deterministic policy controls.

---

## Scoring Model

### Example Weighted Model

`broadband-v1` and `health-v1` use a deterministic score policy. Each
candidate receives named contributions for active-journey fit, intent and CTA
alignment, funnel-stage fit, qualification, household fit, urgency, campaign
context, resume state, and policy priority.

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

### Example Weights

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

### Example Calculation

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

## Ranking Principles

### 1. Deterministic First

The ranking engine must:

- always return the same result for the same inputs
- avoid hidden randomness
- avoid opaque ML dependencies early on

### 2. Explainability

Every ranked item should be explainable.

Example:

> ranked high because it matched current service intent, passed eligibility checks, and aligned to a quote-ready stage

### 3. Configurable Logic

Scoring should be configurable via:

- configuration files
- database-driven rules
- feature flags

The local runtime selects policy by `ranking_policy_version`. Candidate metadata
remains descriptive; it must not become a container for hidden ranking policy.

### Example Config Shape

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

### 4. Separation Of Concerns

The ranking engine should not:

- retrieve content
- infer intent authoritatively
- own free-form AI interpretation
- own activity-source integration logic

It should only:

> rank a provided candidate set after deterministic constraints are applied

---

## Diversity And Business Rules

### Diversity Controls

To avoid repetitive results:

- limit repeated providers
- balance CTA types
- introduce category spread where cross-sell is appropriate

### Business Rules Examples

- prioritize high-value campaigns when relevance remains acceptable
- exclude expired saved-quote resume paths
- suppress state-restricted assets when compliance is enforced
- boost renewal or churn-save content during renewal windows
- allow carefully positioned secondary-journey prompts without displacing the active journey

---

## Summary

The scoring layer should stay deterministic, configurable, and explainable while still leaving room for AI-assisted relevance inputs under explicit policy guardrails.

---

| <- Previous | Next -> |
|---|---|
| [Ranking Engine](../07-ranking-engine.md) | [Runtime and Contracts](./02-runtime-and-contracts.md) |
