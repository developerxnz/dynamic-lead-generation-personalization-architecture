# Content Personalization Strategy

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: System Architecture ->](./04-system-architecture.md)

## Overview

This document defines how offers, educational content, tools, and CTAs are selected for each lead in a multi-vertical service platform.

The goal is not simply to recommend content.

The goal is:

> deliver the *right offer, guidance, and next action* to the right lead at the right moment to maximize qualified conversion.

---

# Goals

The personalization layer should:

- match experiences to customer intent and service category
- optimize for qualified lead outcomes such as quotes, applications, and callbacks
- account for eligibility, suitability, and regional availability
- combine metadata, behavior, and session context
- remain explainable and testable
- separate content selection from content storage

---

# Core Personalization Model

Personalization is based on four inputs:

## 1. Customer State

Provided by the Customer Profile Service:

- service interests
- profile and household attributes
- eligibility signals
- engagement level
- funnel stage
- lead score
- behavioral history

---

## 2. Content And Offer Metadata

Provided by the CMS or offer catalog:

- service_category
- subtype
- provider
- region
- eligibility rules reference
- conversion_goal
- cta_type
- compliance_flags
- freshness
- priority

---

## 3. Context Signals

Real-time signals such as:

- session origin
- device type
- campaign source
- referral partner
- current session behavior
- assisted-sales vs self-serve journey type

---

## 4. Business Constraints

Deterministic rules such as:

- region restrictions
- serviceability checks
- provider suppression lists
- compliance guardrails
- campaign windows

---

# Personalization Flow

```text
Customer Session
        ↓
Load Lead State
        ↓
Retrieve Candidate Offers And Content
        ↓
Apply Eligibility / Suitability Filters
        ↓
Rank Remaining Candidates
        ↓
Select Next Best Actions
        ↓
Render In Journey
```

---

# Candidate Selection Strategy

## Principle: Broad First, Narrow Later

The system should first retrieve a broad candidate set, then narrow through deterministic filtering and ranking.

Avoid overly restrictive CMS queries unless needed for hard constraints such as:

- expired offers
- unsupported regions
- missing compliance approval

---

## Filtering Dimensions

Initial filtering should include:

- service category alignment
- funnel stage compatibility
- region and provider availability
- basic eligibility and suitability checks

---

# Ranking Strategy Overview

Ranking determines final ordering of offers, content, and CTAs.

It is handled by the Ranking Engine, but relies on this strategy.

---

## Primary Ranking Signals

| Signal | Purpose |
|---|---|
| category match | Align to the most relevant service line |
| intent alignment | Match the current customer need |
| eligibility fit | Prefer actions the lead can actually complete |
| funnel alignment | Match the current decision stage |
| behavioral relevance | Reflect recent actions and repeat interests |
| cta alignment | Improve quote, callback, or application likelihood |
| provider / campaign priority | Support commercial objectives explicitly |
| freshness | Ensure current offers and guidance |

---

## Qualified Conversion Focus

Ranking is optimized for:

- quote starts
- quote completion
- application starts
- callback requests
- provider handoff success
- downstream qualified lead outcomes

---

# Content Metadata Strategy

## Required Metadata Model

All managed assets should include universal metadata:

| Field | Purpose |
|---|---|
| service_category | Lead vertical such as novated leasing, health insurance, or broadband |
| subtype | More specific classification inside a vertical |
| provider | Provider or partner association |
| region | Geographic availability |
| funnel_stage | Research / Compare / Quote / Apply / Renew |
| conversion_goal | Intended business outcome |
| cta_type | Quote, callback, compare, check-eligibility, apply |
| compliance_flags | Approval and disclosure requirements |
| freshness | Recency and validity relevance |
| priority | Explicit business control |

---

## Service-Specific Extensions

Verticals can extend the metadata model.

Examples:

- **Novated leasing:** vehicle type, employer requirement, tax-benefit angle
- **Health insurance:** cover tier, household fit, extras focus
- **Broadband:** speed tier, technology availability, contract type

---

## Example Content Entry

```json
{
  "title": "Compare family health cover with extras",
  "service_category": "health_insurance",
  "subtype": "family_cover",
  "provider": "Provider A",
  "region": ["NSW", "VIC"],
  "funnel_stage": "compare",
  "conversion_goal": "start_quote",
  "cta_type": "get_quote",
  "compliance_flags": ["approved_health_copy"],
  "freshness": "high",
  "priority": 3
}
```

---

# Personalization Dimensions

## 1. Service Category Matching

Match assets based on:

- current service interest
- adjacent service cross-sell potential
- provider affinity

---

## 2. Funnel Stage Matching

Align content to customer journey:

- Research -> educational guides and calculators
- Compare -> provider comparisons and suitability explainers
- Quote -> quote-start and eligibility CTAs
- Apply -> conversion-focused support and reassurance
- Renew / Switch -> urgency-driven switching messages

---

## 3. Behavioral Alignment

Use observed behavior:

- viewed providers or plans
- repeated category visits
- quote or form abandonment
- calculator usage
- return frequency

---

## 4. Intent Alignment

Infer intent such as:

- exploring options
- comparing providers
- checking eligibility
- ready for quote
- ready to apply
- likely to switch

---

# Personalization Rules

## Rule 1: Relevance First

Candidates must pass basic relevance thresholds before ranking.

---

## Rule 2: Suitability Before Promotion

Do not promote offers or CTAs that fail deterministic suitability, eligibility, or compliance constraints.

---

## Rule 3: Qualified Conversion Bias

When multiple items are similarly relevant:

> prefer the item most likely to produce a qualified lead outcome

---

## Rule 4: Diversity

Avoid showing:

- repeated providers
- duplicate CTA types
- overly similar assets in the same slot set

---

## Rule 5: Freshness Awareness

Prefer:

- current approved offers
- active campaigns
- recently updated explainer content

---

# AI Usage In Personalization

AI is used only for:

- intent inference
- content summarization
- explanation generation
- query expansion

AI must not be used for:

- ranking decisions
- lead scoring authority
- business rule enforcement
- compliance overrides

---

# RAG Integration (Optional Layer)

RAG can enhance personalization by:

- enriching offer explanations
- answering service-specific questions
- improving discovery across complex service offer sets

Example:

```text
User asks: "What broadband option suits a family working from home?"

→ Retrieve relevant broadband guides, speed explainers, and provider offers
→ Inject customer context such as household type and location
→ Generate grounded explanation + recommended next action
```

---

# Performance Considerations

## Latency Target

Personalization should be designed for:

- fast session experiences (<200ms target for the decisioning layer)
- cached candidate retrieval where possible
- precomputed customer state

---

## Optimization Strategies

- cache CMS metadata with publish-aware invalidation
- precompute intent and engagement signals
- avoid heavy runtime calculations in ranking
- reuse candidate sets only when profile and content versions remain valid

---

## Cache Usage Rules

Caching should preserve freshness for lead decisioning:

- invalidate metadata caches on publish, unpublish, expiry, or compliance changes
- reuse candidate sets only when the customer profile version has not changed
- prefer short TTLs where intent and eligibility change quickly
- do not serve a cached candidate set if a fresher profile or serviceability result is available

---

# Observability

Track:

- asset impressions
- click-through rates
- quote starts
- quote completion
- application progression
- provider handoff quality
- personalization uplift by vertical

---

# Common Pitfalls

Avoid:

- over-filtering too early
- embedding business logic into CMS queries
- using AI as a decision engine
- ignoring qualification and compliance constraints
- static personalization logic across all verticals

---

# Summary

The Content Personalization Strategy defines how offers and content become relevant to each lead.

It connects:

- customer state
- content and offer metadata
- behavioral signals
- ranking logic

to deliver:

> dynamic, qualified-conversion-focused lead experiences at scale

---

| <- Previous | Next -> |
|---|---|
| [Customer State Model](./02-customer-state-model.md) | [System Architecture](./04-system-architecture.md) |
