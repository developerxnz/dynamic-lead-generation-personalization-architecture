# Feedback and Analytics: Success Measurement

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: Feedback and Analytics](../10-feedback-and-analytics.md) | [Next: Event Model and Dashboards ->](./02-event-model-and-dashboards.md)

## Overview

This document defines what success looks like, who owns it, and which implementation choices make that success measurable.

It is useful for marketing, product, analytics, and engineering readers who need a shared measurement model.

---

## Success Measurement Framework

The platform should use a layered success framework so teams can distinguish:

- whether more customers are converting
- whether those conversions are higher quality
- whether the system is choosing the right journey and recommendation
- whether the operating model is becoming easier to optimize

### 1. Outcome Metrics

These are the primary measures of business success.

Recommended outcome metrics:

- qualified lead rate
- quote completion rate
- application progression rate
- callback conversion rate
- provider handoff acceptance rate
- downstream activation or funded outcome where available

### 2. Leading Indicators

These help teams see movement earlier than downstream conversion outcomes.

Recommended leading indicators:

- quote starts
- eligibility checks completed
- resume-flow completion
- comparison depth
- calculator completion
- return-session reactivation

### 3. Guardrail Metrics

These protect the platform from optimizing in the wrong direction.

Recommended guardrails:

- unsuitable recommendation rate
- compliance exception rate
- provider rejection rate
- handoff fallout after recommendation
- AI explanation defect or escalation rate

### 4. Operating Metrics

These show whether the platform is easy to trust and improve.

Recommended operating metrics:

- decision-trace coverage
- percentage of sessions with active-journey attribution
- content revision traceability
- ranking policy version traceability
- experiment cycle time

---

## What Success Should Look Like

For the platform to be considered successful, teams should be able to say:

- we increased qualified conversion, not just engagement
- we improved returning-customer recovery and resume behavior
- we reduced uncertainty around which journey should lead the session
- we can explain why recommendations were shown
- we can identify which changes improved outcomes and which did not

This is the difference between a personalization feature and a measurable growth capability.

---

## Feedback Into Decisioning

The analytics stack should produce inputs that are operationally useful, not just reportable.

Recommended feedback outputs:

- active-journey selection quality by scenario
- provider performance by journey stage
- CTA effectiveness by vertical
- ranking weight review candidates
- AI explanation performance metrics

These outputs should feed configuration review and future model tuning, not directly overwrite live decision logic.

---

## Changes Required To Make Success Easy To Measure

Measurement quality depends on implementation choices. The platform should make the following changes explicit:

### 1. Standardize Segment.io Event Schemas

All channels sending telemetry through Segment.io should emit:

- customer ID where available
- journey ID
- session ID
- service category
- recommendation identifiers
- ranking policy version
- content revision

### 2. Add Decision Trace Events

The platform should emit:

- active journey selected
- recommendation served
- candidate suppressed
- AI explanation shown

Without these events, teams cannot reliably explain or improve decision quality.

### 3. Shape Mixpanel Around Decisions, Not Just Page Views

Mixpanel dashboards should be organized around:

- outcomes
- journeys
- recommendation quality
- AI experience quality
- telemetry completeness

This makes dashboards far more useful than generic page and click reporting.

### 4. Define Metric Ownership

Recommended ownership split:

| Area | Primary owner |
|---|---|
| outcome metrics | product + commercial stakeholders |
| telemetry and event quality | engineering |
| dashboards and projections | data / analytics |
| experiment design | product + analytics |
| provider-quality measures | operations / commercial |

### 5. Establish Baselines Before Optimization

Before teams optimize, they should capture:

- current conversion performance by journey
- current returning-customer behavior
- current provider handoff quality
- current channel-level differences

This makes later uplift claims far more credible.

### 6. Version What Influences Decisions

Make it easy to tie outcomes back to:

- ranking configuration version
- CMS content revision
- AI prompt or model version where applicable
- experiment assignment

If these cannot be linked, teams will struggle to attribute success correctly.

---

## Summary

Success measurement should be explicit, shared across functions, and built into the design of telemetry, dashboards, and decision traceability from the start.

---

| <- Previous | Next -> |
|---|---|
| [Feedback and Analytics](../10-feedback-and-analytics.md) | [Event Model and Dashboards](./02-event-model-and-dashboards.md) |
