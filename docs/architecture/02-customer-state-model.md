# Customer State Model

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Content Personalization Strategy ->](./03-content-personalization-strategy.md)

## Overview

Customer state represents the platform's current understanding of a customer.

The model should evolve continuously based on:

- onboarding responses
- application behavior
- engagement history
- content interactions
- conversion activity

---

## Customer State Structure

Example:

```json
{
  "role": "engineer",
  "seniority": "senior",
  "tech_stack": [".net", "azure"],
  "intent_signals": {
    "recent_activity": "viewed_ci_cd_docs",
    "engagement_level": "high",
    "conversion_stage": "consideration"
  },
  "lead_score": 78
}
```

---

## Recommended Attributes

| Attribute | Purpose |
|---|---|
| role | Persona targeting |
| seniority | Experience targeting |
| tech_stack | Technical alignment |
| engagement_level | Conversion readiness |
| conversion_stage | Funnel alignment |
| lead_score | Overall conversion likelihood |
| recent_activity | Current behavioral context |

---

## Intent Signals

Intent should be inferred dynamically.

Examples:

- learning
- evaluating
- troubleshooting
- comparing
- preparing to purchase

Signals can come from:

- onboarding questions
- recent content interactions
- click patterns
- session behavior
- feature usage

---

## Lead Scoring

Lead scoring should combine:

- engagement
- funnel stage
- behavior
- content interaction
- conversion history

Lead scoring should remain deterministic initially.
- maintain behavioral history for future reprocessing

---

| <- Previous | Next -> |
|---|---|
| [Dynamic Lead Generation Personalization Platform](./01-overview.md) | [Content Personalization Strategy](./03-content-personalization-strategy.md) |
