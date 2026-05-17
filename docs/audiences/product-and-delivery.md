# Product and Delivery Guide

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: Marketing and Growth Guide <-](./marketing-and-growth.md) | [Next: Engineering and Architecture Guide ->](./engineering-and-architecture.md)

## Overview

This reading path is for **product, delivery, and operations stakeholders** who need to understand:

- what the platform is trying to improve
- how active-journey selection and next-best-action decisions work
- what should be configurable versus code-owned
- how rollout should be phased and measured

It is intended to help product readers make prioritization, scope, ownership, and rollout decisions without needing to read every engineering deep dive first.

---

## Why This Matters For Product And Delivery

Many delivery teams can already ship landing pages, campaign changes, and point optimizations.

What is harder is creating a shared platform that can answer:

- which journey should lead the session right now
- which next action is best for the customer and the business
- when an experience should educate, compare, qualify, or convert
- how to improve personalization without making decisions opaque or ungovernable

Without that structure, product teams usually end up with:

- fixed page flows that are hard to adapt
- campaign logic that bleeds into product behavior
- weak handling of returning customers
- unclear boundaries between content, decisioning, analytics, and AI
- roadmap discussions that mix strategy, configuration, and platform engineering together

This platform is meant to turn personalization into an operating capability rather than a set of isolated experiences.

---

## What Product Should Take Away

Product and delivery readers do not need every implementation detail, but they should understand six core ideas.

### 1. The Platform Chooses A Journey, Not Just A Page

The central runtime decision is not "which page should we show?" It is:

> which journey should lead this session, and what is the next best action inside that journey?

That matters because the same customer may be:

- returning to resume a quote
- comparing offers for a current category
- drifting toward a different service category
- responding to campaign intent that does not fully match current need

Product should think in terms of **journey leadership** and **next-best-action orchestration**, not only page composition.

**Associated docs:** [Customer State Model](../architecture/02-customer-state-model.md), [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md)

### 2. Product Needs To Separate Configurable Choices From Code-Owned Logic

One of the most important delivery decisions is knowing what should be easy to tune versus what should stay in controlled backend logic.

Good candidates for configuration:

- content metadata
- provider and campaign priorities
- vertical-specific scoring weights
- CTA labels and deep-link destinations
- experimentation assignments

Good candidates for code-owned or tightly controlled logic:

- eligibility and suitability rules
- serviceability checks
- deterministic ranking flow
- state-model structure
- telemetry contracts

This distinction keeps the platform adaptable without making it fragile.

**Associated docs:** [Contentful Integration](../services/05-contentful-integration.md), [Ranking: Scoring Model and Policy](../services/ranking-engine/01-scoring-model-and-policy.md)

### 3. Active-Journey Selection Is A Product Decision As Much As A Technical One

The platform can track multiple journey states, but only one journey should usually anchor the current session experience.

Product needs to define:

- what evidence is strong enough to switch journeys
- when resume behavior should override campaign intent
- how secondary journeys can surface without polluting the primary path
- which scenarios deserve special handling, such as renewal or interrupted quotes

These are business and UX decisions, not just implementation details.

**Associated docs:** [Customer State Model](../architecture/02-customer-state-model.md), [Customer Profile Service](../services/06-customer-profile-service.md)

### 4. The Platform Optimizes For Qualified Conversion

The platform should not optimize for engagement in isolation.

The preferred outcomes remain:

- quote starts and completions
- callback requests
- application progression
- provider handoff quality
- downstream activation proxies where available

That means product decisions should be judged by whether they improve qualified progression, not just click-through.

**Associated docs:** [README](../../README.md), [Success Measurement](../operations/feedback-and-analytics/01-success-measurement.md)

### 5. AI Improves Interpretation And Explanation, Not Policy Control

AI is important in this architecture, but product should frame it correctly.

AI can help with:

- journey interpretation support
- summarization
- semantic retrieval
- explanation generation
- conversational guidance

AI should not own:

- final ranking authority
- hard eligibility decisions
- compliance overrides
- protected business constraints

This matters because product needs to plan AI-visible features without assuming AI becomes the system of record for core decisions.

**Associated docs:** [AI and RAG Strategy](../ai/08-ai-and-rag-strategy.md), [Vector Search Design](../ai/09-vector-search-design.md)

### 6. Delivery Should Be Phased Around Confidence, Not Feature Volume

The roadmap is intentionally staged so the team can prove:

1. the state and decisioning model works
2. the platform is measurable
3. behavioral and AI layers improve outcomes without weakening control

That means later-phase AI or orchestration capabilities should be justified by what earlier phases prove, not added just because they are available.

**Associated docs:** [Delivery Roadmap](../delivery/11-roadmap.md), [POC Scope](../delivery/12-poc-scope.md)

---

## Runtime Decisions Product Should Understand

At runtime, the platform makes a small set of important decisions:

1. load customer profile and journey states
2. select the active journey for this session
3. retrieve a broad candidate set of offers, content, tools, and CTAs
4. apply deterministic eligibility and suitability checks
5. rank what remains
6. return the next best action and supporting content
7. emit telemetry so outcomes can be measured later

For product teams, this means changes can affect:

- which journey leads
- which content is eligible to appear
- which CTA is promoted
- what explanatory content supports conversion
- which metrics are expected to move

This is why prioritization should be tied to decisioning behavior, not just surface-level UI changes.

**Associated docs:** [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md), [Ranking Engine](../services/07-ranking-engine.md)

---

## What Product Should Own

Product and delivery teams should own:

- journey design principles
- active-journey selection policy intent
- next-best-action priorities by scenario
- experiment design and rollout decisions
- success metrics and guardrails
- definition of what should be configurable
- cross-functional operating model decisions

This is the layer that translates business strategy into a decisioning platform.

---

## What Product Should Not Need To Own

Product should not need to own:

- raw event-pipeline implementation
- infrastructure-level AI behavior
- low-level retrieval or indexing mechanics
- deterministic scoring code paths
- service storage design
- telemetry plumbing

Those stay with engineering and data teams, while product focuses on experience intent, priorities, and measurable outcomes.

---

## Key Product Questions To Resolve Early

Before rollout, product and delivery teams should align on:

### 1. What makes one journey active over another?

Define the precedence between:

- current session evidence
- returning-customer resume behavior
- campaign context
- renewal or urgency signals

### 2. What is the expected next best action in each major scenario?

Examples:

- first-time research
- compare-ready customer
- quote-ready returning customer
- callback-led high-friction scenario
- renewal or re-entry scenario

### 3. What should be configurable?

Make explicit which levers belong to:

- content and campaign teams
- product configuration
- engineering-controlled logic

### 4. What should Phase 1 prove?

A product roadmap is easier to defend when Phase 1 success is explicit, measurable, and narrow enough to execute cleanly.

### 5. Which guardrails matter most?

Examples:

- unsuitable recommendation rate
- provider rejection rate
- compliance exception rate
- AI explanation defect rate

These should be defined before optimization work accelerates.

---

## How The Platform Changes Delivery Planning

### Scope Definition

Delivery planning should shift from feature lists toward:

- which journeys are in scope
- which decision points are in scope
- which data and metadata are required
- which telemetry must exist before launch

### Rollout Planning

Rollout becomes easier when teams treat capabilities separately:

- state foundation
- retrieval and metadata
- ranking and next-best-action logic
- analytics and dashboards
- AI assistance and explanation

### Dependency Planning

Dependencies should be called out clearly between:

- Contentful model readiness
- customer-profile and journey-state availability
- ranking configuration
- dashboard and telemetry readiness
- compliance and disclosure support

This is more robust than planning only around front-end milestones.

**Associated docs:** [Delivery Roadmap](../delivery/11-roadmap.md), [POC Scope](../delivery/12-poc-scope.md)

---

## What Good Looks Like For Product

From a product and delivery perspective, the platform is working well when:

- the right journey leads the session more often
- next-best-action logic is explainable
- returning customers resume faster with less friction
- rollout phases prove meaningful business value before the next layer is added
- product can tune the platform without unsafe changes to core logic
- teams can tell which changes improved outcomes and which did not

---

## Metrics Product Should Care About First

If product leaders only track a small initial set, start with:

1. qualified lead rate
2. quote completion rate
3. active-journey selection quality
4. returning-customer resume completion
5. provider handoff acceptance rate

Then layer in:

- recommendation-to-quote rate
- unsuitable recommendation rate
- abandonment by stage
- AI-assisted versus non-AI progression delta
- telemetry completeness for key decision traces

**Associated docs:** [Success Measurement](../operations/feedback-and-analytics/01-success-measurement.md), [Event Model and Dashboards](../operations/feedback-and-analytics/02-event-model-and-dashboards.md)

---

## Recommended Reading Path

Use this order if you want the clearest product and delivery narrative:

1. [README](../../README.md) - platform framing, goals, and core definitions
2. [Architecture Overview](../architecture/01-overview.md) - problem framing and platform value
3. [Customer State Model](../architecture/02-customer-state-model.md) - how profile and journeys are represented
4. [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md) - how journeys, content, and CTAs are selected
5. [Success Measurement](../operations/feedback-and-analytics/01-success-measurement.md) - what outcomes and guardrails matter
6. [Delivery Roadmap](../delivery/11-roadmap.md) - phased rollout and proof points
7. [POC Scope](../delivery/12-poc-scope.md) - focused implementation slice

---

## The Questions These Docs Answer

- How does the platform decide which journey should lead the session?
- What should be configurable versus code-owned?
- What should product define before rollout begins?
- What should a phased rollout prove before AI-forward expansion goes further?
- Which metrics, dashboards, and owners are needed to run this capability well?

---

## Summary

Product readers should leave this guide understanding the platform as a **decisioning and operating model**, not just a collection of pages or features.

The key product responsibilities are:

- define journey and next-best-action intent
- align configurable versus controlled logic
- phase rollout around measurable proof
- keep qualified conversion and trust as the primary optimization goals

---

| <- Previous | Next -> |
|---|---|
| [Marketing and Growth Guide](./marketing-and-growth.md) | [Engineering and Architecture Guide](./engineering-and-architecture.md) |
