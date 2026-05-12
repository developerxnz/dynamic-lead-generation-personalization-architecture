# Customer State Model

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Content Personalization Strategy ->](./03-content-personalization-strategy.md)

## Overview

Customer state represents the platform's current understanding of a lead across one or more service categories.

The model should evolve continuously based on:

- declared needs
- session behavior
- quote and application activity
- provider or offer interactions
- lead qualification and conversion signals

---

## Customer State Structure

Example:

```json
{
  "customer_id": "cust-12345",
  "service_category_interests": ["novated_leasing", "health_insurance"],
  "profile": {
    "household_type": "family",
    "employment_type": "full_time",
    "location": "NSW",
    "budget_range": "mid",
    "life_stage": "young_family"
  },
  "intent_signals": {
    "current_intent": "comparing_options",
    "urgency": "high",
    "renewal_window_days": 21,
    "switching_intent": "active"
  },
  "eligibility_signals": {
    "employer_supported_novated_leasing": true,
    "coverage_region_match": true,
    "serviceability_confirmed": false
  },
  "engagement_level": "high",
  "conversion_stage": "quote_ready",
  "lead_score": 78
}
```

---

## Recommended Attributes

| Attribute | Purpose |
|---|---|
| service_category_interests | Which service lines are relevant now |
| household_type | Helps tailor offers and messaging |
| employment_type | Important for finance- and leasing-related eligibility |
| location | Supports region and provider availability rules |
| budget_range | Aligns recommendations to likely affordability |
| urgency | Identifies time-sensitive lead handling |
| renewal_window_days | Detects switch and renewal opportunity |
| switching_intent | Indicates comparison and churn likelihood |
| eligibility_signals | Determines what can be shown or promoted |
| conversion_stage | Tracks funnel alignment |
| lead_score | Overall qualified conversion likelihood |

---

## Intent Signals

Intent should be inferred dynamically.

Examples:

- researching options
- comparing providers
- checking eligibility
- seeking a quote
- ready to apply
- renewal-driven switching

Signals can come from:

- form responses
- quote starts and abandons
- provider comparison views
- calculator usage
- callback requests
- search and navigation patterns

---

## Service-Specific Signal Examples

### Novated Leasing

- employer eligibility checks
- EV tax-benefit calculator usage
- salary packaging content views
- lease budget exploration

### Health Insurance

- cover comparison behavior
- extras-focused browsing
- life-stage updates such as couples or family cover
- hospital vs extras trade-off analysis

### Broadband

- address availability checks
- speed-tier comparison
- moving-house journeys
- contract-expiry or switching intent

---

## Lead Scoring

Lead scoring should combine:

- engagement quality
- service-category fit
- eligibility confidence
- intent strength
- funnel progression
- recency of high-intent actions
- prior conversion and provider handoff outcomes

Lead scoring should remain deterministic initially.

The profile should also maintain behavioral history for replay, recalculation, and future model refinement.

---

## Key Design Principles

- support multiple service categories in one profile
- separate observed facts from inferred intent
- track both interest and qualification
- preserve event history for reprocessing
- keep fields explainable to marketing, sales, and compliance stakeholders

---

| <- Previous | Next -> |
|---|---|
| [Multi-Vertical Lead Generation Platform Overview](./01-overview.md) | [Content Personalization Strategy](./03-content-personalization-strategy.md) |
