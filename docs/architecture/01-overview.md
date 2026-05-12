# Multi-Vertical Lead Generation Platform Overview

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Customer State Model ->](./02-customer-state-model.md)

## Overview

This document outlines the overall vision and business goals for a multi-vertical lead-generation platform that personalizes service journeys in real time.

The platform is intended for service categories such as:

- novated leasing
- health insurance
- broadband
- other quote-led, callback-led, or application-led services

The core objective is to present the right combination of:

- offer
- educational content
- calculator or comparison tool
- next-best CTA

for the customer's current intent, eligibility, and likely conversion stage.

---

## Business Goals

- increase qualified lead volume
- improve conversion from visit to quote, application, or callback
- tailor journeys by service category without duplicating architecture
- support provider and campaign priorities without losing relevance
- maintain explainable and auditable decisioning
- allow incremental AI adoption over time

---

## Platform Scope

The platform combines:

- unified lead profiles
- behavioral signals
- service and offer metadata
- deterministic ranking and suitability rules
- optional AI augmentation

The reference implementation uses:

- .NET
- Azure
- Cosmos DB
- Contentful
- Azure OpenAI
- Azure AI Search

---

## High-Level Architecture

```text
Customer Session / Trigger
   ↓
Lead Profile Service
   ↓
Intent + Eligibility Evaluation
   ↓
Offer / Content Candidate Retrieval
   ↓
Ranking + Suitability Engine
   ↓
Next Best Action Selection
   ↓
Web / App / Assisted Sales Experience
```

---

## Vertical-Aware Personalization Model

The same architecture should support multiple services through metadata and configuration.

Examples:

- **Novated leasing:** show EV tax-benefit guides, salary-packaging calculators, and "check employer eligibility" CTAs
- **Health insurance:** show cover comparisons, extras explainers, and "get a quote" CTAs based on household and life stage
- **Broadband:** show speed guidance, provider comparisons, and "check address availability" CTAs based on move or churn intent

---

## Core Principles

- personalization should be dynamic and session-aware
- decisioning should optimize for qualified conversion, not just engagement
- deterministic rules should handle eligibility, suitability, and campaign constraints first
- AI should augment rather than replace business logic
- content and offer management should stay separate from ranking logic

---

## Recommended Delivery Approach

### Phase 1

- deterministic lead personalization
- unified lead profiles
- vertical-aware metadata model
- ranking with suitability guardrails

### Phase 2

- behavioral scoring
- intent refinement
- lead-quality analytics
- cross-vertical optimization loops

### Phase 3

- conversational guidance
- semantic retrieval
- AI-assisted summaries and query expansion
- richer recommendation explanation

---

## Success Outcomes

The platform should improve:

- quote starts
- application progression
- callback requests
- provider handoff quality
- conversion by service category
- explainability for marketing, operations, and compliance teams

---

| <- Previous | Next -> |
|---|---|
| [Documentation Home](../../README.md#documentation-structure) | [Customer State Model](./02-customer-state-model.md) |
