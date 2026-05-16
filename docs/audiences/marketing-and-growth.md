# Marketing and Growth Guide

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Product and Delivery Guide ->](./product-and-delivery.md)

## Overview

This reading path is for **marketing, growth, and commercial stakeholders** who need to understand:

- what business problem the platform solves
- how personalization improves qualified lead outcomes
- how campaigns, content, and offers fit into the architecture
- how success will be measured

It keeps the focus on outcomes, customer journeys, and optimization levers rather than implementation mechanics.

---

## Why This Matters For Marketing And Growth

Many lead-generation teams already know how to drive traffic, launch campaigns, and publish content quickly.

What is often missing is a reliable way to answer:

- which journey should this visitor see now
- which offer or CTA is most likely to create a **qualified** conversion
- how should a returning visitor be handled differently from a first-time visitor
- whether campaign volume is actually turning into better handoff quality downstream

Without that shared decisioning layer, growth teams often end up with:

- generic landing experiences
- campaign routing that is too rigid
- content that exists but is not surfaced at the right moment
- weak reuse of returning-customer context
- dashboards that explain clicks better than commercial outcomes

This platform is meant to fix that by making personalization a **growth and conversion capability**, not just a content-delivery feature.

---

## What Marketing Should Take Away

Marketing teams do not need to understand every service contract in the platform, but they should understand five core ideas:

### 1. The Platform Optimizes For Qualified Conversion

The goal is not simply to improve engagement. The platform is designed to improve outcomes such as:

- quote starts and completions
- callback requests
- application progression
- provider handoff quality
- downstream activation proxies where available

This matters because some experiences that generate more clicks can still produce worse lead quality.

**Associated docs:** [README](../../README.md), [Architecture Overview](../architecture/01-overview.md), [Success Measurement](../operations/feedback-and-analytics/01-success-measurement.md)

### 2. Returning Customers Matter As Much As New Customers

The platform treats each meaningful session as a re-evaluation event.

That means a returning visitor is not treated as a blank slate. The system should recognize:

- prior research
- quote progress
- category interest
- renewal or resume signals
- whether the customer may now be better suited to a different journey

For marketing, this creates better opportunities for:

- remarketing and re-entry journeys
- resume-flow campaigns
- renewal nudges
- cross-sell prompts that are context-aware rather than random

**Associated docs:** [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md), [POC Scope](../delivery/12-poc-scope.md)

### 3. Campaigns Still Matter, But They Are Not The Only Signal

Campaign source, partner source, and landing context remain important inputs, but they should not fully override:

- customer history
- journey stage
- suitability
- current session behavior

The benefit is that campaign intent can be respected without forcing the customer into the wrong experience.

**Associated docs:** [Architecture Overview](../architecture/01-overview.md), [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md)

### 4. Content Strategy Becomes Metadata-Driven

Marketing and content teams still own offers, messaging, and campaign assets, but content becomes more useful when it is structured with metadata such as:

- service category
- funnel stage
- conversion goal
- CTA type
- provider
- region
- compliance flags

This allows the platform to retrieve a broader set of eligible candidates and then choose the most appropriate one for the session.

**Associated docs:** [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md)

### 5. Measurement Becomes More Commercially Useful

The platform makes it easier to answer:

- which journeys are creating better-qualified leads
- which campaigns are driving progress, not just traffic
- which offers and CTAs perform best by journey and vertical
- whether personalization is helping or hurting conversion quality

That is a more useful operating model than looking only at page views and clicks.

**Associated docs:** [Feedback and Analytics](../operations/10-feedback-and-analytics.md), [Success Measurement](../operations/feedback-and-analytics/01-success-measurement.md)

---

## How The Platform Changes Marketing Operations

### Campaign Planning

Campaign planning shifts from "which landing page should we send traffic to?" toward:

- which customer situation are we trying to capture
- which journey should likely lead the session
- which offer, guide, or CTA should be most available for that traffic source
- what counts as success beyond the click

**Associated docs:** [Architecture Overview](../architecture/01-overview.md), [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md), [POC Scope](../delivery/12-poc-scope.md)

### Content Planning

Content planning becomes more deliberate because assets can be designed for:

- discovery
- comparison
- eligibility support
- quote readiness
- application progression
- returning-customer resume flows

This gives growth teams more ways to support customers at different stages without hardcoding a single page path for every campaign.

**Associated docs:** [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md)

### Optimization Reviews

Optimization conversations become stronger because teams can review:

- journey-level conversion
- recommendation quality
- returning-customer recovery
- provider handoff quality
- campaign performance in the context of qualification, not just acquisition volume

**Associated docs:** [Feedback and Analytics](../operations/10-feedback-and-analytics.md), [Success Measurement](../operations/feedback-and-analytics/01-success-measurement.md)

---

## What Marketing Still Owns

The platform does **not** remove ownership from marketing and growth teams.

Marketing still owns:

- campaign strategy
- offer packaging and messaging
- content production
- commercial priorities and promotional windows
- funnel hypotheses
- experimentation ideas

What changes is that those inputs are used inside a more structured decisioning system rather than being expressed only through fixed routing and static page design.

**Associated docs:** [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md), [Delivery Roadmap](../delivery/11-roadmap.md), [POC Scope](../delivery/12-poc-scope.md)

---

## What Marketing Does Not Need To Own

Marketing should not need to own:

- deterministic suitability rules
- hard eligibility logic
- service-specific ranking code
- telemetry plumbing
- AI model behavior at the infrastructure level

Those stay in product, engineering, data, and platform-owned areas, while marketing focuses on the commercial and customer-facing levers.

**Associated docs:** [Feedback and Analytics](../operations/10-feedback-and-analytics.md), [Delivery Roadmap](../delivery/11-roadmap.md)

---

## What Good Looks Like

From a marketing and growth perspective, the platform is working well when:

- more visitors reach quote, callback, or application steps
- returning customers resume faster with less friction
- recommendation and CTA performance improves by journey type
- campaign traffic lands in more relevant experiences
- provider or sales handoff quality improves, not just top-of-funnel volume
- teams can explain why a journey or offer was prioritized

**Associated docs:** [Architecture Overview](../architecture/01-overview.md), [Feedback and Analytics](../operations/10-feedback-and-analytics.md), [Success Measurement](../operations/feedback-and-analytics/01-success-measurement.md)

---

## Metrics Marketing Should Care About First

If a marketing stakeholder only tracks a small set of metrics, start with:

1. qualified lead rate
2. quote completion rate
3. callback conversion rate
4. returning-customer reactivation or resume rate
5. provider handoff acceptance rate

Then layer in supporting diagnostics such as:

- active-journey selection quality
- recommendation click-through rate
- recommendation-to-quote rate
- abandonment by stage
- campaign performance by journey type

**Associated docs:** [Success Measurement](../operations/feedback-and-analytics/01-success-measurement.md), [Event Model and Dashboards](../operations/feedback-and-analytics/02-event-model-and-dashboards.md)

---

## Recommended Reading Path

Use this order if you want the clearest business-to-operating-model narrative:

1. [README](../../README.md) - platform framing, strategic outcomes, and the high-level flow
2. [Architecture Overview](../architecture/01-overview.md) - why the platform exists and how it improves lead quality
3. [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md) - how offers, content, and CTAs are selected
4. [Feedback and Analytics](../operations/10-feedback-and-analytics.md) - how success is tracked at a high level
5. [Success Measurement Deep Dive](../operations/feedback-and-analytics/01-success-measurement.md) - what the business should monitor and own

---

## The Questions These Docs Answer

- How does the platform improve qualified conversion rather than just clicks?
- How should campaigns and content adapt to returning customers and multi-journey behavior?
- Which metrics matter most for business and commercial stakeholders?
- How do marketing and analytics teams know whether personalization is helping?

---

## If You Need More Detail

Use these only when you need to understand why the platform behaves the way it does:

- [Customer State Model](../architecture/02-customer-state-model.md)
- [Delivery Roadmap](../delivery/11-roadmap.md)
- [Event Model and Dashboards](../operations/feedback-and-analytics/02-event-model-and-dashboards.md)

---

## Summary

Marketing readers should be able to use this guide to understand the **business value, operating implications, measurement model, and ownership boundaries** of the platform before diving into deeper architecture docs.

---

| <- Previous | Next -> |
|---|---|
| [Documentation Home](../../README.md#documentation-structure) | [Product and Delivery Guide](./product-and-delivery.md) |
