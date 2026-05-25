# Runtime Decisioning

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: Content Personalization Strategy](../03-content-personalization-strategy.md) | [Next: Metadata and Rules ->](./02-metadata-and-rules.md)

## Overview

This document covers how the personalization layer makes runtime decisions: what inputs it uses, how it selects an active journey, how it retrieves candidates, and how ranking fits into the flow.

---

## Core Personalization Model

Personalization is based on five inputs.

### 1. Customer Profile

Provided by the Customer Profile Service:

- household and employment attributes
- location and stable customer facts
- returning-customer summary
- cross-journey lead score

### 2. Journey States

Also provided by the Customer Profile Service:

- service category
- intent
- stage
- urgency
- resume status
- qualification state
- journey-level score

The platform may have multiple journey states for a customer, but it should select one as the primary driver for the current session.

### 3. Activity Metadata

Provided by metadata attached to existing activities:

- service category
- subtype
- provider
- region
- eligibility rules reference
- conversion goal
- cta type
- compliance flags
- freshness
- priority

### 4. Context Signals

Real-time signals such as:

- session origin
- device type
- campaign source
- referral partner
- current session behavior
- assisted-sales versus self-serve journey type

### 5. Business Constraints

Deterministic rules such as:

- region restrictions
- serviceability checks
- provider suppression lists
- compliance guardrails
- campaign windows

---

## Active-Journey Selection

When multiple journeys exist, the platform should choose the active journey using:

- recency of meaningful events
- current session behavior
- campaign and channel context
- journey-level score
- qualification confidence
- whether the customer is resuming an interrupted flow

This avoids forcing the customer into a single permanent category while still keeping the current experience coherent.

---

## Candidate Selection Strategy

### Principle: Broad First, Narrow Later

The system should first retrieve a broad candidate set, then narrow through deterministic filtering and ranking.

Avoid overly restrictive activity queries unless needed for hard constraints such as:

- expired offers
- unsupported regions
- missing compliance approval

### Filtering Dimensions

Initial filtering should include:

- active-journey category alignment
- funnel-stage compatibility
- region and provider availability
- basic eligibility and suitability checks

Cross-sell or secondary-journey candidates can still be included, but they should be intentionally positioned rather than mixed blindly into the primary journey set.

---

## Ranking Strategy Overview

Ranking determines final ordering of offers, content, and CTAs.

It is handled by the Ranking Engine, but relies on this strategy.

### Primary Ranking Signals

| Signal | Purpose |
|---|---|
| active-journey match | Align to the journey the platform should optimize now |
| intent alignment | Match the current customer need |
| eligibility fit | Prefer actions the lead can actually complete |
| funnel alignment | Match the current decision stage |
| behavioral relevance | Reflect recent actions and repeat interests |
| CTA alignment | Improve quote, callback, or application likelihood |
| provider or campaign priority | Support commercial objectives explicitly |
| freshness | Ensure current offers and guidance |

### Qualified Conversion Focus

Ranking is optimized for:

- quote starts
- quote completion
- application starts
- callback requests
- provider handoff success
- downstream qualified lead outcomes

---

## Summary

The runtime decisioning pattern is:

1. load the customer context
2. select the active journey
3. retrieve broadly
4. filter deterministically
5. rank what remains for qualified conversion

---

| <- Previous | Next -> |
|---|---|
| [Content Personalization Strategy](../03-content-personalization-strategy.md) | [Metadata and Rules](./02-metadata-and-rules.md) |
