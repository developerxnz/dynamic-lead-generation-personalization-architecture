# AI and RAG Strategy

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Vector Search Design ->](./09-vector-search-design.md)

## Overview

This document defines how AI and Retrieval-Augmented Generation (RAG) should be used within the lead-generation platform.

The guiding principle is:

> AI enhances understanding and experience - deterministic systems control decisions

AI is used to improve:

- intent interpretation
- content and offer explanation
- conversational guidance
- semantic retrieval
- personalization enrichment

It is not used for authoritative business decisioning.

---

# Goals

The AI layer should:

- improve discovery across large offer and content sets
- support natural language interactions across service categories
- help explain why a recommendation is relevant
- enrich customer intent signals
- reduce friction in quote and application journeys
- remain grounded, reviewable, and safe

---

# AI vs Deterministic Systems

## Deterministic Systems (Authoritative)

Responsible for:

- ranking logic
- lead scoring
- eligibility and suitability rules
- provider suppression and campaign logic
- business constraints

These must be:

- explainable
- predictable
- testable
- auditable

---

## AI Systems (Augmentation Layer)

Responsible for:

- intent inference assistance
- summarization
- semantic understanding
- content enrichment
- conversational responses
- query expansion

AI is treated as:

> a supporting intelligence layer, not a decision engine

---

# Recommended AI Capabilities

## 1. Intent Interpretation

AI can infer customer intent from:

- form responses
- click behavior
- session activity
- free-text questions

Example outputs:

- researching options
- comparing providers
- checking eligibility
- ready for quote
- ready to apply

---

## 2. Offer And Content Summarization

AI can generate:

- short offer summaries
- "why this is relevant" explanations
- plain-language comparisons
- CTA context summaries

Used for improving:

- comprehension
- confidence
- conversion

---

## 3. Personalized Messaging

AI can adapt:

- hero messages
- descriptions
- onboarding prompts
- CTA explanations

Based on:

- service category
- customer profile
- funnel stage
- recent behavior

---

## 4. Conversational Experiences

AI enables:

- quote-prep assistants
- service discovery chat
- contextual help systems
- eligibility guidance journeys

---

## 5. Query Expansion

AI can expand user intent into richer search queries.

Example:

User input:
> "best broadband plan for working from home"

Expanded into:

- high-speed broadband
- reliable family household connection
- upload speed considerations
- address availability and contract flexibility

---

# RAG (Retrieval-Augmented Generation)

## Overview

RAG combines:

- retrieval (vector search + metadata filtering)
- generation (LLM responses)

to produce contextual, grounded outputs.

---

## RAG Flow

```text
User Input
        ↓
Intent Extraction (AI)
        ↓
Metadata + Vector Retrieval
        ↓
Context Assembly
        ↓
LLM Response Generation
        ↓
Personalized Output
```

---

## RAG Data Sources

RAG can use:

- managed offer and content assets
- customer profile data
- engagement history
- provider FAQs
- disclosure and eligibility guidance

---

## RAG Use Cases

### 1. Novated Leasing Guidance

- explain salary packaging concepts
- describe employer eligibility requirements
- recommend next calculators or contact actions

### 2. Health Insurance Discovery

- compare cover tiers in simple language
- explain hospital vs extras trade-offs
- guide leads toward an appropriate quote path

### 3. Broadband Selection

- explain speed tiers and household fit
- answer moving-house or switching questions
- recommend address-check or quote actions

---

# Guardrails

AI outputs should:

- cite retrieved source material where appropriate
- avoid inventing eligibility outcomes
- defer to deterministic systems for ranking and constraints
- avoid personalized advice that exceeds approved policy boundaries

---

# Summary

AI should make the platform easier to understand and use without replacing deterministic lead decisioning.

The long-term goal is a hybrid intelligence system:

> deterministic core + AI augmentation layer = scalable, explainable lead generation

---

| <- Previous | Next -> |
|---|---|
| [Ranking Engine](../services/07-ranking-engine.md) | [Vector Search Design](./09-vector-search-design.md) |
