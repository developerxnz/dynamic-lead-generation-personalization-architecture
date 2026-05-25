# Ranking Engine

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: AI and RAG Strategy ->](../ai/08-ai-and-rag-strategy.md)

## Overview

The Ranking Engine is the core decisioning component of the platform.

It determines which activities, offers, content items, and CTAs are shown to a lead after candidate retrieval and suitability screening.

It assumes the customer may have multiple journey states, but ranking should operate against the **active journey selected for the current session**.

Its purpose is to:

> maximize qualified lead conversion in an explainable, deterministic way

---

## Core Responsibilities

The ranking engine is responsible for:

- scoring candidate assets
- applying business and suitability rules
- ordering results
- explaining ranking decisions
- supporting configuration-based tuning
- ensuring consistency across similar sessions

Retrieval supplies the candidate set. The ranking engine decides what is safe and best to show from that set.

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

| <- Previous | Next -> |
|---|---|
| [Customer Profile Service](./06-customer-profile-service.md) | [AI and RAG Strategy](../ai/08-ai-and-rag-strategy.md) |
