# Metadata And Rules

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: Content Personalization Strategy](../03-content-personalization-strategy.md) | [Previous: Runtime Decisioning <-](./01-runtime-decisioning.md) | [Next: AI, Edge Cases, and Performance ->](./03-ai-edge-cases-and-performance.md)

## Overview

This document defines the metadata model and decisioning rules that make content personalization governable and explainable.

---

## Content Metadata Strategy

### Required Metadata Model

All managed assets should include universal metadata.

| Field | Purpose |
|---|---|
| service_category | Lead vertical such as novated leasing, health insurance, or broadband |
| subtype | More specific classification inside a vertical |
| provider | Provider or partner association |
| region | Geographic availability |
| funnel_stage | Research, compare, quote, apply, renew, or resume |
| conversion_goal | Intended business outcome |
| cta_type | Quote, callback, compare, check eligibility, apply, resume |
| cta_deep_link | Deep-link destination the CTA should open for the selected journey and channel |
| compliance_flags | Approval and disclosure requirements |
| freshness | Recency and validity relevance |
| priority | Explicit business control |

### Service-Specific Extensions

Verticals can extend the metadata model.

Examples:

- **Novated leasing:** vehicle type, employer requirement, tax-benefit angle
- **Health insurance:** cover tier, household fit, extras focus
- **Broadband:** speed tier, technology availability, contract type

---

## Personalization Dimensions

### 1. Journey Matching

Match assets based on:

- active journey category
- active journey intent
- active journey stage
- qualification and suitability state

### 2. Cross-Journey Support

When appropriate, the experience can also include:

- adjacent-service cross-sell prompts
- secondary-journey reminders
- lightweight exploration hooks for other active categories

### 3. Behavioral Alignment

Use observed behavior:

- viewed providers or plans
- repeated category visits
- quote or form abandonment
- calculator usage
- return frequency

### 4. Intent Alignment

Infer intent such as:

- exploring options
- comparing providers
- checking eligibility
- ready for quote
- ready to apply
- likely to switch

---

## Personalization Rules

### Rule 1: Relevance First

Candidates must pass basic relevance thresholds before ranking.

### Rule 2: Suitability Before Promotion

Do not promote offers or CTAs that fail deterministic suitability, eligibility, or compliance constraints.

When a CTA is promoted, its deep link should also be validated for the active journey, channel, and region so the next step lands the customer in the intended flow rather than on a generic page.

### Rule 3: Active Journey Leads

The most relevant current journey should anchor the session experience.

### Rule 4: Cross-Journey Support Must Be Intentional

Secondary-journey content should support, not confuse, the primary path.

### Rule 5: Qualified Conversion Bias

When multiple items are similarly relevant:

> prefer the item most likely to produce a qualified lead outcome

### Rule 6: Diversity

Avoid showing:

- repeated providers
- duplicate CTA types
- overly similar assets in the same slot set

---

## Summary

Metadata makes broad retrieval possible. Rules make promotion safe and explainable.

Those two layers should stay distinct:

- metadata helps the platform discover possibilities
- rules decide what is valid to show now

---

| <- Previous | Next -> |
|---|---|
| [Runtime Decisioning](./01-runtime-decisioning.md) | [AI, Edge Cases, and Performance](./03-ai-edge-cases-and-performance.md) |
