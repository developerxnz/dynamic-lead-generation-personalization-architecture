# Ownership And Operating Model

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: POC Scope <-](./12-poc-scope.md)

## Overview

This document defines **who should own which parts of the platform** so marketing, product, engineering, analytics, operations, and commercial teams can work against the same operating model.

The goal is not to create bureaucracy. The goal is to prevent recurring confusion about:

- who decides what should be shown
- who controls business rules versus managed content
- who owns telemetry quality and reporting
- who approves AI and ranking changes
- who is accountable for qualified conversion outcomes

---

## Why An Ownership Matrix Matters

Without explicit ownership, this kind of platform usually drifts into one of three failure modes:

1. content and campaign teams are expected to solve decisioning problems through metadata alone
2. engineering becomes the hidden owner of product and commercial prioritization choices
3. analytics and operations are asked to explain outcomes they were never given the data or decision trace to govern

An ownership matrix helps teams preserve the architecture's intended boundaries:

- **managed content** stays manageable
- **decisioning** stays explainable
- **AI** stays assistive rather than authoritative
- **measurement** stays trusted

---

## Operating Model Principles

### Product Owns Decision Intent

Product and delivery should define:

- what the platform is trying to optimize
- which journeys matter most
- what the next-best-action policy should feel like
- which guardrails must hold

### Engineering Owns Runtime Implementation

Engineering should own:

- service boundaries
- APIs and runtime contracts
- deterministic implementation of decisioning logic
- telemetry plumbing
- operational resilience

### Marketing And Commercial Teams Own Managed Inputs

Marketing, content, and commercial teams should own:

- campaign strategy
- offer packaging and messaging
- managed content inputs
- promotional windows
- provider priorities where commercially appropriate

### Data And Analytics Own Measurement Trust

Analytics and data should own:

- metric definitions with product partnership
- dashboard logic and projections
- experiment analysis
- readout quality and attribution confidence

### Operations Own Downstream Reality Checks

Operations and commercial teams should own:

- provider handoff quality
- callback and sales-assist workflow reality
- downstream acceptance and fallout feedback

---

## Capability Ownership Matrix

| Capability area | Primary owner | Key partners | What that owner is accountable for |
|---|---|---|---|
| Journey strategy and active-journey policy intent | Product / delivery | marketing, commercial, engineering | define which scenarios matter, what should lead the session, and what tradeoffs are acceptable |
| Managed content and offer metadata | Marketing / content / commercial | product, engineering, compliance | ensure offers, copy, CTA definitions, metadata, and disclosures are current and usable |
| Customer profile and journey projections | Engineering | product, data | maintain durable customer state, journey summaries, and projection reliability |
| Deterministic eligibility and suitability rules | Product + engineering | compliance, operations | define and implement factual constraints and policy-safe recommendation rules |
| Ranking configuration and next-best-action logic | Product + engineering | analytics, commercial | balance qualified conversion goals, customer fit, and controlled commercial priorities |
| Contentful integration and normalization | Engineering | marketing/content | turn managed assets into stable domain objects for retrieval and ranking |
| AI prompts, grounding, and fallback behavior | Engineering | product, compliance, analytics, content | implement safe AI behavior, versioning, observability, and fallback handling |
| Telemetry schema and event quality | Engineering | analytics, product | ensure required identifiers, versions, and decision traces are emitted correctly |
| Dashboards, measurement models, and experiment readouts | Analytics / data | product, marketing, engineering | define trusted reporting views and interpret outcome movement |
| Provider handoff quality and callback operations | Operations / commercial | product, analytics | capture whether the platform is creating better downstream lead quality |

---

## Decision Rights Matrix

The table below helps clarify not just who contributes, but **who should make the final call** for common changes.

| Decision or change type | Final decision owner | Required input from | Notes |
|---|---|---|---|
| Which journey should typically lead in a scenario | Product | marketing, operations, engineering | business and UX decision with technical implementation impact |
| Hard eligibility rule changes | Product + engineering | compliance, operations | should remain deterministic and explicitly approved |
| Suitability or suppression policy changes | Product + engineering | compliance, analytics, commercial | changes should be traceable and measurable |
| Offer copy, disclosures, and CTA wording | Marketing / content | compliance, product | managed in content operations, not ranking code |
| Provider priority or campaign weighting changes | Product / commercial | marketing, analytics, engineering | should be visible and constrained rather than hidden in content |
| Ranking weight tuning | Product + engineering | analytics | should be reviewed against qualified conversion and guardrails |
| AI prompt template or grounding policy changes | Engineering | product, compliance, content, analytics | requires versioning and outcome review |
| Metric definition changes | Product + analytics | engineering, commercial | outcome and guardrail definitions should stay consistent across dashboards |
| Dashboard design and reporting cuts | Analytics | product, marketing, engineering | analytics owns reporting trust, not platform runtime behavior |
| POC scope and rollout sequencing | Product / delivery | engineering, marketing, analytics, commercial | roadmap decisions should reflect delivery confidence and measurability |

---

## What Each Team Should Own Day To Day

### Marketing, Content, And Commercial

Should own:

- campaign and offer strategy
- managed content creation
- metadata completeness for assets
- provider and promo windows
- messaging hypotheses

Should not own:

- ranking code
- eligibility enforcement
- telemetry plumbing
- AI runtime behavior

### Product And Delivery

Should own:

- journey definitions
- active-journey policy intent
- next-best-action priorities
- platform guardrails
- rollout and experiment sequencing

Should not own directly:

- infrastructure implementation
- storage design
- event-pipeline mechanics

### Engineering

Should own:

- service implementation
- deterministic runtime logic
- integration contracts
- observability and telemetry emission
- AI runtime integration and fallbacks

Should not own in isolation:

- commercial priority decisions
- journey-policy intent
- outcome-metric interpretation

### Analytics And Data

Should own:

- metric definitions with product alignment
- dashboard logic
- experiment analysis
- attribution and traceability quality checks

Should not own:

- live recommendation decisions
- content operations
- provider messaging

### Operations And Commercial Stakeholders

Should own:

- downstream handoff quality review
- callback and assisted-sales outcome feedback
- provider acceptance insights
- operational constraints that should feed product policy

Should not own directly:

- low-level ranking mechanics
- raw telemetry implementation

---

## Typical Review Forums

The platform becomes easier to govern when changes are reviewed in the right forum instead of one generic backlog meeting.

| Review forum | Primary owner | Typical topics |
|---|---|---|
| Journey and policy review | Product | active-journey rules, next-best-action behavior, edge-case treatment, guardrails |
| Content and campaign review | Marketing / content / commercial | metadata quality, offer readiness, disclosures, CTA messaging |
| Ranking and experiment review | Product + analytics | weight tuning candidates, recommendation quality, uplift and guardrail readouts |
| AI quality review | Engineering + product | prompt versions, fallback rates, grounding issues, explanation usefulness |
| Telemetry and dashboard review | Engineering + analytics | event quality, decision-trace completeness, dashboard trustworthiness |
| Provider and operational review | Operations / commercial | handoff quality, callback outcomes, provider fallout, downstream acceptance |

---

## What Good Governance Looks Like

The operating model is working well when:

- product can explain why one journey leads another
- marketing can change managed inputs without bypassing platform rules
- engineering can ship decisioning changes without becoming the hidden business owner
- analytics can attribute outcomes to decisions, versions, and experiments
- operations can feed downstream reality back into prioritization

This is how the platform stays adaptable without becoming opaque.

---

## Summary

The platform needs a shared ownership model because it sits across customer experience, decisioning, analytics, content operations, and AI support layers.

The simplest durable split is:

- **product** owns decision intent and rollout choices
- **engineering** owns runtime implementation and reliability
- **marketing/content/commercial** own managed inputs and campaign levers
- **analytics** owns reporting trust and experiment readouts
- **operations/commercial** own downstream quality feedback

That split keeps the system governable while still allowing fast iteration.

---

| <- Previous | Next -> |
|---|---|
| [POC Scope](./12-poc-scope.md) | [Documentation Home](../../README.md#documentation-structure) |
