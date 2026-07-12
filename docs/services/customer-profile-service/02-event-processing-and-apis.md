# Customer Profile Service: Event Processing and APIs

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: Customer Profile Service](../06-customer-profile-service.md) | [Previous: State and Persistence <-](./01-state-and-persistence.md)

## Overview

This document covers how the Customer Profile Service ingests events, updates projections, and exposes read models for live decisioning.

---

## Event Model

### Supported Events

The system should ingest structured events such as:

- `content_viewed`
- `offer_viewed`
- `quote_started`
- `quote_completed`
- `callback_requested`
- `application_started`
- `eligibility_checked`
- `provider_handoff_completed`
- `journey_resumed`

### Example Event

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

## Event Processing Detail

Recommended processing steps for each event:

1. validate schema and required identifiers
2. check idempotency using `eventId`
3. load current profile and affected journey documents
4. apply deterministic aggregation rules
5. update derived scores and summaries
6. write updated projections with optimistic concurrency
7. emit downstream change notifications if needed

This keeps the service deterministic and replayable.

### Event To State Transformation

```text
Event Stream
        ↓
Event Processor
        ↓
Profile + Journey Aggregation Logic
        ↓
Profile / Journey Update
        ↓
Cosmos DB Projection
```

### Key Principle

The system should always prefer:

> event-driven updates over direct mutations

This ensures:

- auditability
- replayability
- correctness over time

Direct profile mutations should be reserved for controlled repair or backfill workflows, not normal application traffic.

### Processing Guarantees

To keep profile state correct under asynchronous delivery, the service should guarantee:

- idempotent processing keyed by `eventId`
- per-customer ordering using source sequence numbers where available, otherwise `occurredAt`
- optimistic concurrency on profile and journey projections using document versioning / ETags
- deterministic replay for rebuilds and historical correction

If an older event arrives after a newer projection has already been written, the processor should not blindly overwrite state. It should either merge deterministically or trigger reprocessing for that customer's profile and journeys from the authoritative event stream.

---

## Active Journey Support

The profile service does not have to decide the active journey permanently, but it should expose enough information for the decisioning layer to choose it reliably.

Recommended outputs:

- all current journey states
- journey-level scores
- journey recency
- resume indicators
- cross-journey return summary

This allows live decisioning to pick the best journey for the current session without losing visibility of parallel journeys.

---

## Example Read Payloads

### `GET /customers/{customerId}`

```json
{
  "customerId": "12345",
  "attributes": {
    "householdType": "family",
    "employmentType": "full_time",
    "location": "QLD"
  },
  "customerSummary": {
    "leadScore": 78
  }
}
```

### `GET /customers/{customerId}/journeys`

```json
{
  "customerId": "12345",
  "journeys": [
    {
      "journeyId": "journey-health-001",
      "serviceCategory": "health_insurance",
      "intent": "comparing_providers",
      "stage": "quote_ready",
      "resumeCandidate": true,
      "journeyScore": 0.78
    },
    {
      "journeyId": "journey-broadband-001",
      "serviceCategory": "broadband",
      "intent": "checking_availability",
      "stage": "research",
      "resumeCandidate": false,
      "journeyScore": 0.41
    }
  ]
}
```

### Consistency And Read Strategy

Recommended read behavior for live personalization:

- read the latest committed profile document
- read all active or recent journey documents for the customer
- prefer bounded-staleness or session-consistent reads where supported
- avoid fan-out across unrelated partitions during live decisioning

This keeps the service fast enough for request-time orchestration while preserving useful consistency for active-journey selection.

---

## CQRS Approach

The service should follow CQRS principles.

### Commands

- ingest event
- update profile attributes
- recalculate scores
- refresh journey intent and eligibility projections

### Queries

- get customer profile
- get journey states
- get engagement summary
- get qualification summary
- get return and resume summary

### Projections

Maintain derived views such as:

- engagement summary
- journey progression
- provider affinity
- service-category propensity
- return readiness

---

## API Design

### Example Endpoints

#### Get Profile

```http
GET /customers/{customerId}
```

#### Get Summary

```http
GET /customers/{customerId}/summary
```

#### Get Journeys

```http
GET /customers/{customerId}/journeys
```

#### Ingest Event

```http
POST /customers/{customerId}/events
```

#### Trigger Recalculation

```http
POST /customers/{customerId}/recalculate
```

---

## Summary

The service should expose stable read models for live decisioning while keeping writes event-driven, replayable, and safe under asynchronous delivery.

---

| <- Previous | Next -> |
|---|---|
| [State and Persistence](./01-state-and-persistence.md) | [Ranking Engine](../07-ranking-engine.md) |
