# Product and Delivery Guide

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: Marketing and Growth Guide <-](./marketing-and-growth.md) | [Next: Engineering and Architecture Guide ->](./engineering-and-architecture.md)

## Overview

This reading path is for **product, delivery, and operations stakeholders** who need to understand:

- what journeys the platform supports
- how active-journey selection and next-best-action decisions work
- how rollout should be phased
- how success, guardrails, and ownership should be defined

It balances business clarity with enough system detail to support prioritization and delivery planning.

---

## Start Here

1. [README](../../README.md) - platform purpose, goals, and design principles
2. [Architecture Overview](../architecture/01-overview.md) - problem framing and business goals
3. [Customer State Model](../architecture/02-customer-state-model.md) - how customer and journey state are represented
4. [Content Personalization Strategy](../architecture/03-content-personalization-strategy.md) - how journeys, offers, and CTAs are assembled
5. [Feedback and Analytics](../operations/10-feedback-and-analytics.md) - what the operating model needs to measure
6. [Delivery Roadmap](../delivery/11-roadmap.md) - phased rollout and capability sequencing

---

## Product Deep Dives

Use these when you need more concrete delivery detail:

- [Customer Profile Service](../services/06-customer-profile-service.md)
- [Ranking Engine](../services/07-ranking-engine.md)
- [Success Measurement Deep Dive](../operations/feedback-and-analytics/01-success-measurement.md)
- [Event Model and Dashboards](../operations/feedback-and-analytics/02-event-model-and-dashboards.md)

---

## The Questions These Docs Answer

- How does the platform decide which journey should lead the session?
- What should be configurable versus code-owned?
- What should a phased rollout prove before AI-forward expansion goes further?
- Which metrics, dashboards, and owners are needed to run this capability well?

---

## Summary

Product readers should be able to move from the overview docs into roadmap and measurement guidance first, then dip into service-level detail only where it helps clarify delivery choices.

---

| <- Previous | Next -> |
|---|---|
| [Marketing and Growth Guide](./marketing-and-growth.md) | [Engineering and Architecture Guide](./engineering-and-architecture.md) |
