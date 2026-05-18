# Customer Profile Service

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Ranking Engine ->](./07-ranking-engine.md)

## Overview

The Customer Profile Service is responsible for maintaining a continuously updated view of each lead.

It should manage:

- a durable customer profile
- one or more journey states per customer
- the projections needed for personalization and lead generation across service categories

---

## What This Service Owns

- durable customer profile facts
- multiple concurrent journey states
- customer-level and journey-level projections
- event-driven updates and replayable state rebuilds
- read models used by live decisioning

The service owns current profile and journey projections. Longer-term analytical history and dashboarding stay in the analytics layer.

---

## High-Level Architecture

At a high level, digital and assisted events flow into the ingestion layer, through profile processing, into customer and journey projections stored in Cosmos DB, and then out to decisioning services as stable read models.

---

## How To Read This Section

Use the overview page for orientation, then go deeper based on what you need:

| Detail page | Best for | Covers |
|---|---|---|
| [State and Persistence](./customer-profile-service/01-state-and-persistence.md) | product + engineering | customer and journey ownership, persistence split, storage, and projection design |
| [Event Processing and APIs](./customer-profile-service/02-event-processing-and-apis.md) | engineering | event model, processing guarantees, read payloads, CQRS, and endpoints |

---

| <- Previous | Next -> |
|---|---|
| [Contentful Integration](./05-contentful-integration.md) | [Ranking Engine](./07-ranking-engine.md) |
