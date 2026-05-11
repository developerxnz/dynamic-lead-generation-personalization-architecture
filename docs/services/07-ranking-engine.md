# Ranking Engine

## Overview

The Ranking Engine is the core decisioning component of the personalization platform.

It determines which content is shown to a customer after candidate content has been retrieved.

Its purpose is to:

> maximise engagement and lead conversion probability in an explainable, deterministic way

---

# Goals

The ranking engine should:

- produce ordered content recommendations
- optimise for conversion likelihood
- remain deterministic and explainable
- support configurable business rules
- integrate with customer state and behavioural signals
- allow future experimentation and AI augmentation

---

# Core Responsibilities

The ranking engine is responsible for:

- scoring candidate content
- applying business rules
- ordering results
- explaining ranking decisions
- supporting configuration-based tuning
- ensuring consistency across sessions

---

# High-Level Flow

```text
Customer State
        ↓
Candidate Content Set
        ↓
Feature Extraction
        ↓
Score Calculation
        ↓
Ranking Aggregation
        ↓
Final Ordered Results
```

---

# Inputs to the Ranking Engine

## 1. Customer State

From the Customer Profile Service:

- persona (e.g. engineer, manager)
- seniority
- tech stack
- engagement level
- funnel stage
- lead score
- recent activity

---

## 2. Content Metadata

From Contentful:

- persona_fit
- funnel_stage
- topics
- conversion_goal
- CTA type
- experience level
- freshness
- priority

---

## 3. Behavioural Signals

From analytics systems:

- clicks
- impressions
- dwell time
- past conversions
- session history
- content interactions

---

## 4. Context Signals

- login time
- device type
- session entry point
- referral source
- campaign context

---

# Scoring Model

## Example Weighted Model

Each content item is scored using weighted signals:

```text
Total Score =
  Persona Match Score
+ Funnel Alignment Score
+ Topic Relevance Score
+ Behavioural Relevance Score
+ CTA Alignment Score
+ Freshness Score
+ Editorial Priority Score
```

---

## Example Weights

| Signal | Weight |
|---|---|
| Funnel alignment | 6 |
| Persona match | 5 |
| CTA relevance | 5 |
| Behavioural relevance | 4 |
| Topic overlap | 3 |
| Freshness | 2 |
| Editorial priority | 2 |

---

## Example Calculation

```text
Content A:
- Persona match: +5
- Funnel alignment: +6
- CTA relevance: +5
- Topic match: +3
- Freshness: +2

Total = 21
```

---

# Ranking Strategy

## Step 1: Normalisation

Standardise all inputs:

- scale scores consistently
- remove bias between sources
- normalise behavioural metrics

---

## Step 2: Feature Scoring

Convert raw inputs into:

- relevance scores
- engagement probability
- conversion likelihood indicators

---

## Step 3: Weighted Aggregation

Combine features into a single score per content item.

---

## Step 4: Sorting

Order content by:

```text
Highest score → Lowest score
```

Apply tie-breakers such as:

- freshness
- editorial priority
- diversity constraints

---

# Ranking Principles

## 1. Deterministic First

The ranking engine must:

- always return the same result for the same inputs
- avoid hidden randomness
- avoid opaque ML dependencies early on

---

## 2. Explainability

Every ranked item should be explainable:

Example:

> “Ranked high because it matches persona + funnel stage + recent engagement pattern”

---

## 3. Configurable Logic

Scoring should be configurable via:

- configuration files
- database-driven rules
- feature flags

Avoid hardcoding weights.

---

## 4. Separation of Concerns

Ranking engine should NOT:

- retrieve content
- infer intent
- perform AI reasoning
- apply CMS logic

It should ONLY:

> rank a provided candidate set

---

# Ranking Architecture

```text
Personalization Service
        ↓
Candidate Retrieval Service
        ↓
Ranking Engine
        ↓
Ranked Content List
        ↓
Frontend Delivery Layer
```

---

# Diversity and Business Rules

## Diversity Controls

To avoid repetitive results:

- limit repeated topics
- balance content types
- introduce category spread

---

## Business Rules Examples

- prioritise high-value campaigns
- exclude expired content
- boost promotional content during campaigns
- suppress low-performing content

---

# Explainability Model

Each ranked item should return:

```json
{
  "contentId": "abc123",
  "score": 21,
  "reasons": [
    "Persona match: engineer",
    "Funnel alignment: consideration",
    "CTA relevance: trial signup",
    "Topic match: CI/CD"
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

## Optimisation Strategies

- precompute behavioural aggregates
- cache customer profiles by profile version
- cache content metadata by content publish version
- avoid recomputing static scores

---

## Cache Boundaries and Invalidation

The ranking engine should cache inputs and derived features, not assume a single ranking result stays valid across materially different sessions.

Recommended rules:

- invalidate profile-dependent feature caches when the customer profile version changes
- invalidate content metadata caches on publish, unpublish, or scheduled content expiry events
- use short TTLs as a fallback when event-driven invalidation is delayed
- key reusable feature caches by the versions of the profile and content metadata they were derived from

This keeps ranking deterministic while reducing the risk of serving stale features after asynchronous profile or CMS updates.

---

# Experimentation Support

The ranking engine should support:

- A/B testing of weights
- feature toggles
- ranking strategy variants
- shadow mode evaluations

---

# Common Ranking Pitfalls

Avoid:

- overfitting to engagement metrics
- excessive complexity early on
- embedding AI directly into scoring logic
- tightly coupling CMS structure to ranking rules
- non-deterministic scoring behaviour

---

# Future Enhancements

Potential improvements include:

- machine learning ranking models
- reinforcement learning optimisation
- contextual bandits for ranking
- real-time weight adjustment
- personalised ranking functions per segment
- hybrid AI + deterministic scoring layers

---

# Summary

The Ranking Engine is the **decision layer** of the platform.

It is responsible for:

- selecting the best content
- applying business logic
- optimising for conversion outcomes
- ensuring explainability and consistency

The long-term vision is a hybrid system:

> deterministic ranking core + AI-assisted optimisation layer
