# Content Personalization Strategy

## Overview

This document defines how content is selected, ranked, and optimised for each customer in the personalization platform.

The goal is not simply to recommend content.

The goal is:

> deliver the *right content, to the right customer, at the right moment* to maximise engagement and lead conversion.

---

# Goals

The content personalization layer should:

- match content to customer intent
- optimise for conversion outcomes (lead generation)
- support dynamic real-time decisions on login
- combine metadata, behaviour, and context signals
- remain explainable and testable
- separate content selection from content storage (Contentful)

---

# Core Personalization Model

Personalization is based on three inputs:

## 1. Customer State

Provided by the Customer Profile Service:

- persona (role, seniority, industry)
- engagement level
- funnel stage
- intent signals
- lead score
- behavioural history

---

## 2. Content Metadata

Provided by Contentful:

- persona_fit
- funnel_stage
- topics
- conversion_goal
- CTA type
- experience level
- freshness
- priority

---

## 3. Context Signals

Real-time signals such as:

- login time
- device type
- session origin
- campaign source
- current session behaviour

---

# Personalization Flow

```text
Customer Login
        ↓
Load Customer State
        ↓
Retrieve Candidate Content
        ↓
Apply Relevance Filtering
        ↓
Rank Content Items
        ↓
Select Top Content
        ↓
Render in Application
```

---

# Candidate Selection Strategy

## Principle: Broad First, Narrow Later

The system should first retrieve a **broad candidate set**, then narrow through ranking.

Avoid overly restrictive filtering in Contentful queries.

---

## Filtering Dimensions

Initial filtering should include:

- persona alignment
- funnel stage compatibility
- topic relevance

Avoid hard exclusions unless required (e.g. expired content).

---

# Ranking Strategy Overview

Ranking determines final ordering of content.

It is handled by the Ranking Engine (see `/services/07-ranking-engine.md`), but relies on this strategy.

---

## Primary Ranking Signals

| Signal | Purpose |
|---|---|
| persona match | Align content to user type |
| funnel alignment | Match buying stage |
| behavioural relevance | Reflect user activity |
| CTA alignment | Improve conversion likelihood |
| topic overlap | Ensure relevance |
| freshness | Ensure recency |
| editorial priority | Business control |

---

## Conversion Optimisation Focus

Ranking is optimised for:

- CTA engagement
- trial signups
- demo requests
- product exploration
- onboarding completion

---

# Content Metadata Strategy

## Required Metadata Model

All Contentful content should include:

| Field | Purpose |
|---|---|
| persona_fit | Target audience |
| funnel_stage | Awareness / Consideration / Decision |
| topics | Subject relevance |
| conversion_goal | Intended outcome |
| CTA type | Action type |
| experience_level | Skill targeting |
| freshness | Recency relevance |
| priority | Editorial control |

---

## Example Content Entry

```json
{
  "title": "Improve CI/CD pipelines with Azure DevOps",
  "persona_fit": ["engineer", "devops"],
  "funnel_stage": "consideration",
  "topics": ["ci-cd", "azure", ".net"],
  "conversion_goal": "start_trial",
  "cta_type": "trial_signup",
  "experience_level": "senior",
  "freshness": "high",
  "priority": 3
}
```

---

# Personalization Dimensions

## 1. Persona Matching

Match content based on:

- role (engineer, manager, architect)
- seniority (junior, mid, senior)
- industry

---

## 2. Funnel Stage Matching

Align content to customer journey:

- Awareness → educational content
- Consideration → comparison content
- Decision → conversion-focused content

---

## 3. Behavioural Alignment

Use observed behaviour:

- previously viewed topics
- clicked content types
- time spent on categories

---

## 4. Intent Alignment

Infer intent such as:

- learning
- evaluating
- comparing
- ready to convert

---

# Personalization Rules

## Rule 1: Relevance First

Content must always pass basic relevance thresholds before ranking.

---

## Rule 2: Conversion Bias

When multiple items are similar in relevance:

> prefer content with stronger conversion signals

---

## Rule 3: Diversity

Avoid showing:

- repeated topics
- same CTA types
- overly similar content items

---

## Rule 4: Freshness Awareness

Prefer:

- recently published content
- actively maintained campaigns
- time-sensitive offers

---

# AI Usage in Personalization

AI is used ONLY for:

- intent inference
- content summarisation
- content explanation
- query expansion

AI must NOT be used for:

- ranking decisions
- lead scoring
- business rule enforcement

---

# RAG Integration (Optional Layer)

RAG can enhance personalization by:

- enriching content explanations
- providing contextual answers
- improving content discovery

Example:

```text
User asks: "How do I improve deployment speed?"

→ Retrieve relevant CI/CD content
→ Inject customer context
→ Generate explanation + recommendations
```

---

# Performance Considerations

## Latency Target

Personalization should be designed for:

- fast login experiences (<200ms target for decisioning layer)
- cached candidate retrieval where possible
- precomputed customer state

---

## Optimization Strategies

- cache Contentful metadata with publish-aware invalidation
- precompute engagement signals
- avoid runtime heavy computations in ranking
- reuse candidate sets across sessions only when the underlying profile and content versions remain valid

---

## Cache Usage Rules

Caching in the personalization layer should preserve freshness for login-time decisioning:

- invalidate Contentful metadata caches on publish, unpublish, or expiry events
- reuse candidate sets only when the customer's profile version has not changed since the set was produced
- prefer short TTLs for candidate-set caches because intent and funnel state can change quickly
- do not serve a cached candidate set for a login flow if a fresher customer profile projection is available

These rules keep latency low without letting stale profile or content state distort personalization outcomes.

---

# Observability

Track:

- content impression rates
- click-through rates
- conversion rates
- ranking effectiveness
- personalization uplift
- funnel progression impact

---

# Common Pitfalls

Avoid:

- over-filtering candidates early
- embedding business logic into CMS queries
- using AI as a decision engine
- ignoring behavioural signals
- static personalization logic

---

# Future Enhancements

Potential improvements:

- real-time adaptive personalization
- reinforcement learning-based ranking tuning
- dynamic content generation
- personalized content sequences (journeys)
- predictive content recommendations
- cross-session intent continuity

---

# Summary

The Content Personalization Strategy defines how content becomes relevant to each customer.

It connects:

- customer state
- content metadata
- behavioural signals
- ranking logic

to deliver:

> dynamic, conversion-optimised content experiences at scale
