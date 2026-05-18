# Delivery Roadmap

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: POC Scope ->](./12-poc-scope.md)

## Overview

This roadmap outlines a phased delivery strategy for the lead-generation platform.

The goal is to:

- deliver value incrementally
- reduce implementation risk
- maintain architectural flexibility
- support rollout across multiple service categories
- build toward an AI-forward platform without losing control of core decisioning

---

## Delivery Principles

- start with durable platform foundations
- make journey selection and qualification explainable first
- add AI where it visibly improves customer and operator experience
- instrument every phase for lead-quality learning
- keep multi-vertical rollout configuration-driven

---

## Phase 1 - Decisioning Foundation

### Goals

- establish the customer profile and journey-state model
- implement service metadata and offer taxonomy
- build deterministic ranking with suitability rules
- deliver personalized next-best actions for a primary active journey

### Implementation Scope

Core capabilities:

- Cosmos DB customer profiles and journey states
- Contentful metadata strategy
- initial ranking engine
- candidate retrieval
- active-journey selection
- session personalization flow

### Recommended Deliverables

#### Profile And Journey Foundation

Implement:

- customer profile storage
- journey-state storage
- profile APIs
- journey APIs
- aggregation logic
- qualification signal foundations

#### Content And Offer Metadata Strategy

Enhance managed content models with:

- service-category metadata
- provider metadata
- funnel-stage metadata
- CTA metadata
- conversion-goal metadata
- compliance flags

#### Ranking Engine (Initial)

Implement deterministic ranking using:

- active-journey match
- intent alignment
- eligibility fit
- funnel alignment
- CTA likelihood
- freshness scoring

Ranking should remain:

- explainable
- configurable
- deterministic

#### Session Personalization Flow

Build the initial personalization pipeline:

```text
Customer Session
   ↓
Load Profile + Journey States
   ↓
Select Active Journey
   ↓
Retrieve Candidate Offers And Content
   ↓
Check Eligibility / Suitability
   ↓
Rank Candidates
   ↓
Return Personalized Results
```

#### Measurement Foundation

Implement the minimum measurement capability required to prove success:

- standardized Segment.io event schema
- customer and journey identifiers
- recommendation-served events
- active-journey-selected events
- initial Mixpanel dashboards for funnel and lead-quality views

### Expected Outcomes

Phase 1 should provide:

- explainable decisioning
- measurable lead uplift
- stable ranking behavior
- maintainable architecture foundations
- low operational complexity
- a credible baseline for future uplift measurement

---

## Phase 2 - Behavioral And Analytics Optimization

### Goals

- improve personalization quality
- introduce richer behavioral understanding
- optimize active-journey selection
- evolve qualification and intent modeling
- strengthen returning-customer re-entry

### Implementation Scope

Enhancements include:

- behavioral event tracking
- analytics pipelines
- customer-level and journey-level scoring refinement
- intent refinement
- configurable ranking weights
- experimentation support

### Recommended Deliverables

#### Behavioral Tracking

Track:

- impressions
- clicks
- quote starts
- quote completion
- callback requests
- repeated visits
- provider handoff outcomes
- journey resume events

#### Intent And Qualification Refinement

Introduce richer modeling using:

- behavioral aggregation
- session analysis
- engagement patterns
- eligibility evidence
- returning-customer behavior
- optional AI-assisted inference

#### Analytics Pipeline

Recommended architecture:

```text
Frontend / CRM Events
        ↓
Event Pipeline
        ↓
Analytics Processing
        ↓
Aggregated Projections
        ↓
Personalization Optimization
```

#### Success Measurement Model

Define and operationalize:

- north-star outcome metrics
- leading indicators
- guardrail metrics
- owner-aligned dashboards
- baseline versus post-change comparisons

#### Ranking Optimization

Enhance ranking using:

- engagement history
- qualification confidence
- content effectiveness
- configurable scoring by vertical
- stronger active-journey and resume signals

### Expected Outcomes

Phase 2 should provide:

- better lead quality
- more accurate journey handling
- improved conversion efficiency
- stronger optimization feedback loops
- faster detection of which changes are actually improving outcomes

---

## Phase 3 - AI-Forward Experiences

### Goals

- improve discovery and explanation
- reduce friction in complex journeys
- support natural-language guidance
- help resolve multi-journey ambiguity
- augment deterministic decisioning safely

### Implementation Scope

Enhancements include:

- Azure OpenAI integration
- vector search
- RAG experiences
- conversational guidance
- summary generation
- AI-assisted journey interpretation

### Recommended Deliverables

#### Semantic Retrieval

Implement:

- vector index for managed content
- metadata-aware retrieval
- semantic candidate generation
- journey-aware query expansion

#### Conversational Guidance

Support:

- quote-prep assistants
- service comparison help
- eligibility explanation
- next-step recommendations
- assisted-sales summaries

#### AI Explanation Layer

Add:

- "why this is relevant" explanations
- resume guidance for returning customers
- secondary-journey prompts where appropriate

#### AI Measurement Layer

Measure:

- AI explanation engagement
- AI-assisted journey selection performance
- grounded-response quality
- AI versus non-AI experience deltas

#### Multi-Vertical Expansion

Roll out new service lines primarily through configuration:

- metadata extensions
- ranking rule sets
- provider onboarding
- analytics segmentation
- AI grounding content

### Expected Outcomes

Phase 3 should provide:

- richer customer guidance
- improved discovery
- scalable multi-vertical expansion
- measurable AI-assisted conversion uplift
- clearer confidence in where AI adds value and where deterministic controls should stay dominant

---

## Phase 4 - Operating Model Maturity

### Goals

- make experimentation routine
- improve governance around AI and personalization
- turn platform signals into repeatable commercial optimization

### Implementation Scope

Enhancements include:

- mature experimentation workflows
- AI governance and review processes
- provider and channel performance optimization
- stronger operational dashboards
- mature success-governance reviews

### Expected Outcomes

Phase 4 should provide:

- better organizational confidence in the platform
- faster iteration loops
- clearer ownership across product, engineering, marketing, and operations
- a durable measurement operating model that makes optimization repeatable

---

## Summary

The recommended delivery sequence is:

1. decisioning foundation
2. behavioral and analytics optimization
3. AI-forward experiences
4. operating model maturity

This provides:

- lower implementation risk
- faster initial delivery
- maintainable architecture
- scalable vertical expansion
- measurable business value at every phase

---

| <- Previous | Next -> |
|---|---|
| [Feedback and Analytics](../operations/10-feedback-and-analytics.md) | [POC Scope](./12-poc-scope.md) |
