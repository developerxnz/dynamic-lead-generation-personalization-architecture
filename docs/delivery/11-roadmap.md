# Delivery Roadmap

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [End of guide ->](../../README.md#documentation-structure)

## Overview

This roadmap outlines the recommended phased delivery strategy for the lead-generation platform.

The goal is to:

- deliver value incrementally
- reduce implementation risk
- maintain architectural flexibility
- support rollout across multiple service categories

---

# Phase 1 - Deterministic Lead Personalization

## Goals

- establish unified lead profiles
- implement service metadata and offer taxonomy
- build deterministic ranking with suitability rules
- deliver personalized next-best actions

---

## Implementation Scope

Core capabilities:

- Cosmos DB lead profiles
- Contentful metadata strategy
- basic ranking engine
- candidate retrieval
- session personalization flow
- lead scoring

---

## Recommended Deliverables

### Lead Profile Foundation

Implement:

- lead state storage
- profile APIs
- profile aggregation logic
- engagement and qualification signal foundations

Suggested technologies:

- .NET
- Cosmos DB

---

### Content And Offer Metadata Strategy

Enhance managed content models with:

- service-category metadata
- provider metadata
- funnel-stage metadata
- CTA metadata
- conversion-goal metadata
- compliance flags

---

### Ranking Engine (Initial)

Implement deterministic ranking using:

- category alignment
- intent alignment
- eligibility fit
- funnel alignment
- CTA likelihood
- freshness scoring

Ranking should remain:

- explainable
- configurable
- deterministic

---

### Session Personalization Flow

Build the initial personalization pipeline:

```text
Customer Session
   ↓
Load Lead State
   ↓
Retrieve Candidate Offers And Content
   ↓
Check Eligibility / Suitability
   ↓
Rank Candidates
   ↓
Return Personalized Results
```

---

## Expected Outcomes

Phase 1 should provide:

- explainable decisioning
- measurable lead uplift
- stable ranking behavior
- maintainable architecture foundations
- low operational complexity

---

# Phase 2 - Behavioral Optimization

## Goals

- improve personalization quality
- introduce richer behavioral understanding
- optimize ranking effectiveness
- evolve lead qualification and intent modeling

---

## Implementation Scope

Enhancements include:

- behavioral event tracking
- analytics pipelines
- qualification scoring
- intent refinement
- configurable ranking weights
- experimentation support

---

## Recommended Deliverables

### Behavioral Tracking

Track:

- impressions
- clicks
- quote starts
- quote completion
- callback requests
- repeated visits
- provider handoff outcomes

---

### Intent And Qualification Refinement

Introduce richer modeling using:

- behavioral aggregation
- session analysis
- engagement patterns
- eligibility evidence
- optional AI-assisted inference

Example intents:

- researching
- comparing
- quote-ready
- application-ready
- renewal-switching

---

### Analytics Pipeline

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

---

### Ranking Optimization

Enhance ranking using:

- engagement history
- qualification confidence
- content effectiveness
- configurable scoring by vertical

---

## Expected Outcomes

Phase 2 should provide:

- better lead quality
- more accurate intent handling
- improved conversion efficiency
- stronger optimization feedback loops

---

# Phase 3 - AI-Augmented Experiences

## Goals

- improve discovery and explanation
- reduce friction in complex journeys
- support natural-language guidance
- augment deterministic decisioning safely

---

## Implementation Scope

Enhancements include:

- Azure OpenAI integration
- vector search
- RAG experiences
- conversational guidance
- summary generation

---

## Recommended Deliverables

### Semantic Retrieval

Implement:

- vector index for managed content
- metadata-aware retrieval
- semantic candidate generation

---

### Conversational Guidance

Support:

- quote-prep assistants
- service comparison help
- eligibility explanation
- next-step recommendations

---

### Multi-Vertical Expansion

Roll out new service lines primarily through configuration:

- metadata extensions
- ranking rule sets
- provider onboarding
- analytics segmentation

Examples:

- novated leasing
- health insurance
- broadband
- future service categories

---

## Expected Outcomes

Phase 3 should provide:

- richer customer guidance
- improved discovery
- scalable multi-vertical expansion
- measurable AI-assisted conversion uplift

---

# Summary

The recommended delivery sequence is:

1. deterministic personalization
2. behavioral optimization
3. AI-augmented experiences

This provides:

- lower implementation risk
- faster initial delivery
- maintainable architecture
- scalable vertical expansion
- measurable business value at every phase

---

| <- Previous | Next -> |
|---|---|
| [Feedback and Analytics](../operations/10-feedback-and-analytics.md) | [Documentation Home](../../README.md#documentation-structure) |
