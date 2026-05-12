# Customer Profile Service

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Ranking Engine ->](./07-ranking-engine.md)

## Overview

The Customer Profile Service is responsible for maintaining a continuously updated view of each lead.

It aggregates declared attributes, behavioral signals, and inferred intent into a single lead state that powers personalization and lead generation across service categories.

---

# Goals

The Customer Profile Service should:

- maintain an up-to-date lead profile
- aggregate behavior over time
- support real-time and near-real-time updates
- calculate and evolve lead scores
- expose a consistent API for downstream systems
- support reprocessing and historical correction

---

# Core Responsibilities

## 1. Lead State Management

Maintain a unified profile including:

- identity and account linkage
- service interests
- household and employment attributes
- behavioral history
- funnel stage
- lead score
- eligibility evidence

---

## 2. Behavioral Aggregation

Ingest and aggregate events such as:

- content_viewed
- offer_clicked
- quote_started
- quote_completed
- callback_requested
- application_started
- address_checked
- provider_selected

These events are transformed into meaningful intent and qualification signals.

---

## 3. Intent Inference

Derive customer intent from behavior.

Examples:

- researching
- comparing
- checking eligibility
- quote-ready
- application-ready
- renewal-switching

Intent is continuously updated, not static.

---

## 4. Lead Scoring

Compute a lead score based on:

- engagement quality
- service-category fit
- qualification confidence
- conversion actions
- recency of activity
- prior provider handoff outcomes

Lead score is a dynamic projection, not a stored constant.

---

# High-Level Architecture

```text
Digital And Assisted Events
        ↓
Event Ingestion Layer
        ↓
Profile Processing Pipeline
        ↓
Lead State Aggregation
        ↓
Cosmos DB (Profile Store)
        ↓
Decisioning / Ranking Services
```

---

# Customer State Model

## Example Structure

```json
{
  "customerId": "12345",
  "serviceInterests": ["broadband", "health_insurance"],
  "profile": {
    "householdType": "family",
    "employmentType": "full_time",
    "location": "QLD"
  },
  "engagement": {
    "level": "high",
    "recentActivityScore": 0.82,
    "sessionFrequency": "weekly"
  },
  "funnel": {
    "stage": "quote_ready",
    "progressionScore": 0.65
  },
  "intent": {
    "current": "comparing_providers",
    "confidence": 0.78
  },
  "eligibility": {
    "serviceabilityConfirmed": true,
    "renewalWindowDays": 14
  },
  "leadScore": 78
}
```

---

# Event Model

## Supported Events

The system should ingest structured events such as:

- `content_viewed`
- `offer_viewed`
- `quote_started`
- `quote_completed`
- `callback_requested`
- `application_started`
- `eligibility_checked`
- `provider_handoff_completed`

---

## Example Event

```json
{
  "eventId": "evt-001",
  "eventType": "quote_started",
  "customerId": "12345",
  "occurredAt": "2026-05-11T10:15:00Z",
  "ingestedAt": "2026-05-11T10:15:02Z",
  "metadata": {
    "serviceCategory": "health_insurance",
    "provider": "Provider A",
    "funnelStage": "quote",
    "region": "QLD"
  }
}
```

---

# Processing Model

## Event -> State Transformation

```text
Event Stream
        ↓
Event Processor
        ↓
State Aggregation Logic
        ↓
Lead Profile Update
        ↓
Cosmos DB Projection
```

---

## Key Principle

The system should always prefer:

> event-driven updates over direct mutations

This ensures:

- auditability
- replayability
- correctness over time

Direct profile mutations should be reserved for controlled repair or backfill workflows, not normal application traffic.

---

## Processing Guarantees

To keep profile state correct under asynchronous delivery, the service should guarantee:

- idempotent processing keyed by `eventId`
- per-customer ordering using source sequence numbers where available, otherwise `occurredAt`
- optimistic concurrency on profile projections using document versioning / ETags
- deterministic replay for profile rebuilds and historical correction

If an older event arrives after a newer projection has already been written, the processor should not blindly overwrite state. It should either merge deterministically or trigger reprocessing for that customer's projection from the authoritative event stream.

---

# Storage Strategy

## Cosmos DB Usage

Cosmos DB is used for:

- lead profiles
- aggregated state
- projection storage
- fast read access for personalization

---

## Partitioning Strategy

Recommended partition key:

- `customerId`

This ensures:

- fast lookup per customer
- scalable horizontal partitioning
- predictable access patterns

---

## Separation Of Data

| Type | Storage |
|---|---|
| Raw events | Event store / stream |
| Aggregated state | Cosmos DB |
| Analytics projections | Data warehouse / lake |

---

# CQRS Approach

The service should follow CQRS principles.

## Commands

- ingest event
- update profile attributes
- recalculate lead score
- refresh intent and eligibility projections

---

## Queries

- get customer profile
- get engagement summary
- get funnel state
- get intent and eligibility state

---

## Projections

Maintain derived views such as:

- engagement summary
- funnel progression
- provider affinity
- service-category propensity

---

# API Design

## Example Endpoints

### Get Profile

```http
GET /customers/{customerId}
```

### Get Summary

```http
GET /customers/{customerId}/summary
```

### Ingest Event

```http
POST /customers/{customerId}/events
```

### Trigger Recalculation

```http
POST /customers/{customerId}/recalculate
```

---

# Summary

The Customer Profile Service is the system of record for current lead understanding.

It transforms raw interactions into structured intelligence used by:

- ranking engines
- personalization services
- sales-assist experiences
- AI augmentation layers

Its core value is:

> turning behavior and qualification evidence into actionable lead intelligence in real time

---

| <- Previous | Next -> |
|---|---|
| [Contentful Integration](./05-contentful-integration.md) | [Ranking Engine](./07-ranking-engine.md) |
