# Feedback and Analytics

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Delivery Roadmap ->](../delivery/11-roadmap.md)

## Overview

Behavioral feedback and analytics are critical for improving lead quality and personalization effectiveness over time.

The platform should continuously measure:

- engagement
- qualification behavior
- active-journey selection quality
- ranking effectiveness
- AI-assisted experience quality
- progression toward quote, application, and provider handoff

Analytics should be treated as a core platform capability rather than a secondary concern.

---

## Analytics Architecture At A Glance

At a high level, events from frontend, CRM, and assisted-sales channels flow through Segment.io into the event pipeline, are processed into aggregated projections, and then support both Mixpanel dashboards and decisioning feedback loops.

---

## What This Capability Must Deliver

- measurement of qualified conversion, not just engagement
- journey-aware telemetry through Segment.io
- decision traceability for recommendations, suppression, and AI assistance
- Mixpanel dashboards that reflect journeys and decision quality
- analytics outputs that support safer optimization

---

## Recommended Technologies

| Capability | Suggested Technology |
|---|---|
| Telemetry collection | Segment.io |
| Event ingestion backbone | Azure Event Hubs |
| Queue processing | Azure Service Bus |
| Stream processing | Azure Functions |
| Operational storage | Cosmos DB |
| Analytical storage | Data Lake / Synapse |
| Dashboarding | Mixpanel |
| Deep reporting | Power BI |

---

## How To Read This Section

Use the overview page for orientation, then go deeper based on what you need:

| Detail page | Best for | Covers |
|---|---|---|
| [Success Measurement](./feedback-and-analytics/01-success-measurement.md) | marketing + product + analytics | outcome model, guardrails, ownership, and what changes make success easy to measure |
| [Event Model and Dashboards](./feedback-and-analytics/02-event-model-and-dashboards.md) | engineering + analytics | signals, event taxonomy, Segment-to-Mixpanel flow, dashboard definitions, and projections |

---

| <- Previous | Next -> |
|---|---|
| [Vector Search Design](../ai/09-vector-search-design.md) | [Delivery Roadmap](../delivery/11-roadmap.md) |
