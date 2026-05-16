# Ranking Engine

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: AI and RAG Strategy ->](../ai/08-ai-and-rag-strategy.md)

## Overview

The Ranking Engine is the core decisioning component of the platform.

It determines which offers, content items, and CTAs are shown to a lead after candidate retrieval and suitability screening.

It assumes the customer may have multiple journey states, but ranking should operate against the **active journey selected for the current session**.

Its purpose is to:

> maximize qualified lead conversion in an explainable, deterministic way

---

## Who Should Read This

| Audience | Why this page matters |
|---|---|
| Product and growth | understand which levers shape relevance, guardrails, and commercial prioritization |
| Engineering | understand the ranking boundary and where to find runtime and contract detail |
| Analytics | understand which ranking inputs and outputs should be measurable |

---

## Goals

The ranking engine should:

- produce ordered recommendations
- optimize for qualified conversion likelihood
- remain deterministic and explainable
- support configurable business rules
- integrate with customer-profile, journey-state, and behavioral signals
- allow future experimentation and AI-assisted relevance support

---

## Core Responsibilities

The ranking engine is responsible for:

- scoring candidate assets
- applying business and suitability rules
- ordering results
- explaining ranking decisions
- supporting configuration-based tuning
- ensuring consistency across similar sessions

---

## At-A-Glance Flow

```text
Candidate Retrieval
        ↓
Suitability Filters
        ↓
Ranking Engine
        ↓
Ranked Recommendations + Suppression Reasons
```

---

## How To Read This Section

Use the overview page for orientation, then go deeper based on what you need:

| Detail page | Best for | Covers |
|---|---|---|
| [Scoring Model and Policy](./ranking-engine/01-scoring-model-and-policy.md) | product + engineering | ranking inputs, weights, policy rules, and configuration |
| [Runtime and Contracts](./ranking-engine/02-runtime-and-contracts.md) | engineering | request and response shapes, runtime steps, explainability, and performance boundaries |

---

## What Product Should Take Away

- ranking is where relevance, suitability, and commercial priorities are balanced
- deterministic controls stay authoritative even when AI contributes relevance signals
- explainability is part of the output, not an afterthought

## What Engineering Should Take Away

- the engine ranks a provided candidate set rather than owning retrieval or CMS logic
- scoring, suppression, and traceability should be configurable and testable
- runtime contracts and cache invalidation boundaries matter as much as the weight model

---

## Summary

The Ranking Engine is the decision layer of the platform.

It is responsible for:

- selecting the best next actions
- applying business logic
- choosing results that best fit the active journey
- optimizing for qualified lead outcomes
- ensuring explainability and consistency

The long-term vision is a hybrid system:

> deterministic ranking core + AI-assisted optimization layer

---

| <- Previous | Next -> |
|---|---|
| [Customer Profile Service](./06-customer-profile-service.md) | [AI and RAG Strategy](../ai/08-ai-and-rag-strategy.md) |
