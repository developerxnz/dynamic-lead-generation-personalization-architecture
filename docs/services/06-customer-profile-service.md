# Customer Profile Service

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Ranking Engine ->](./07-ranking-engine.md)

## Overview

The Customer Profile Service is responsible for maintaining a continuously updated view of each customer.

It aggregates behavioural signals, explicit attributes, and inferred intent into a single “customer state” that powers personalization and lead generation.

This service is a foundational component of the platform because it determines *who the customer is right now*, not just what they said at onboarding.

---

# Goals

The Customer Profile Service should:

- maintain an up-to-date customer state
- aggregate behavioural signals over time
- support real-time and near-real-time updates
- calculate and evolve lead scores
- expose a consistent API for downstream systems
- support reprocessing and historical correction

---

# Core Responsibilities

## 1. Customer State Management

Maintain a unified customer profile including:

- static attributes (role, seniority, industry)
- dynamic attributes (engagement level, intent)
- behavioural history
- funnel stage
- lead score

---

## 2. Behavioural Aggregation

Ingest and aggregate events such as:

- content views
- CTA clicks
- session activity
- feature usage
- onboarding responses

These events are transformed into meaningful signals.

---

## 3. Intent Inference

Derive customer intent from behaviour:

Examples:

- learning
- evaluating
- comparing
- troubleshooting
- purchase-ready

Intent is continuously updated, not static.

---

## 4. Lead Scoring

Compute a lead score based on:

- engagement frequency
- conversion actions
- funnel progression
- recency of activity
- content interaction depth

Lead score is a *dynamic projection*, not a stored constant.

---

# High-Level Architecture

```text
Application Events
        ↓
Event Ingestion Layer
        ↓
Profile Processing Pipeline
        ↓
Customer State Aggregation
        ↓
Cosmos DB (Profile Store)
        ↓
Personalization / Ranking Services
```

---

# Customer State Model

## Example Structure

```json
{
  "customerId": "12345",
  "persona": {
    "role": "engineer",
    "seniority": "senior",
    "industry": "software"
  },
  "attributes": {
    "tech_stack": [".net", "azure"],
    "preferred_topics": ["ci-cd", "cloud"]
  },
  "engagement": {
    "level": "high",
    "recent_activity_score": 0.82,
    "session_frequency": "daily"
  },
  "funnel": {
    "stage": "consideration",
    "progression_score": 0.65
  },
  "intent": {
    "current": "evaluating",
    "confidence": 0.78
  },
  "lead_score": 78
}
```

---

# Event Model

## Supported Events

The system should ingest structured events such as:

- `content_viewed`
- `cta_clicked`
- `session_started`
- `onboarding_completed`
- `feature_used`
- `search_performed`

---

## Example Event

```json
{
  "eventId": "evt-001",
  "eventType": "content_viewed",
  "customerId": "12345",
  "contentId": "abc-001",
  "occurredAt": "2026-05-11T10:15:00Z",
  "ingestedAt": "2026-05-11T10:15:02Z",
  "metadata": {
    "topic": "ci-cd",
    "persona": "engineer",
    "funnelStage": "consideration"
  }
}
```

---

# Processing Model

## Event → State Transformation

```text
Event Stream
        ↓
Event Processor
        ↓
State Aggregation Logic
        ↓
Customer Profile Update
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

- customer profiles
- aggregated state
- projection storage
- fast read access for personalization

---

## Partitioning Strategy

Recommended partition key:

- `customerId`

This ensures:

- fast lookup per user
- scalable horizontal partitioning
- predictable access patterns

---

## Separation of Data

| Type | Storage |
|---|---|
| Raw events | Event store (stream/queue) |
| Aggregated state | Cosmos DB |
| Analytics projections | Data warehouse / lake |

---

# CQRS Approach

The service should follow CQRS principles:

## Commands

- ingest event
- update profile
- recalculate lead score
- update intent

---

## Queries

- get customer profile
- get engagement summary
- get funnel state
- get intent state

---

## Projections

Maintain derived views such as:

- engagement summary
- funnel progression
- content affinity profile

---

# API Design

## Example Endpoints

### Get Profile

```http
GET /customers/{customerId}
```

---

### Get Summary

```http
GET /customers/{customerId}/summary
```

---

### Ingest Event

```http
POST /customers/{customerId}/events
```

---

### Trigger Recalculation

```http
POST /customers/{customerId}/recalculate
```

---

# Lead Scoring Model

## Scoring Inputs

Lead score is derived from:

- engagement frequency
- conversion actions
- funnel progression
- recency of activity
- content interaction depth

---

## Example Scoring Logic

```text
Lead Score =
  Engagement Score
+ Conversion Score
+ Funnel Progression Score
+ Recency Boost
+ Content Depth Score
```

---

## Key Principle

Lead scoring should be:

- deterministic (initially)
- explainable
- configurable
- versioned

---

# Performance Considerations

## Requirements

The service should support:

- high-frequency event ingestion
- low-latency profile reads
- scalable aggregation pipelines

---

## Optimisation Strategies

- pre-aggregate behavioural metrics
- batch event processing
- cache frequently accessed profiles with short TTLs
- separate read/write models

---

# Consistency Model

The system should operate with:

- eventual consistency for behavioural updates and background recalculations
- strong consistency, or explicitly bounded staleness, for login-time profile reads used in personalization
- asynchronous processing for non-critical updates, analytics projections, and backfills

---

## Login-Time Read Requirements

Every login is a re-evaluation point for personalization. The read path that loads customer state before candidate retrieval and ranking should therefore use the latest committed profile projection, or a bounded-staleness mode with an explicit freshness target that downstream services can rely on.

Background analytics, experimentation summaries, and non-user-facing recalculations can tolerate eventual consistency because they do not directly affect the current session experience.

---

## Cache Invalidation

Profile caching should use a read-through or cache-aside approach with:

- short TTLs for hot profiles
- invalidation when a newer profile projection version is written
- cache keys that include the customer identity and profile version where possible

If a login-time read detects that the cache is older than the latest committed projection, it should refresh from Cosmos DB before the ranking flow continues.

---

# Observability

Track:

- event ingestion rate
- processing latency
- profile update frequency
- lead score changes
- intent shift frequency

---

# Security & Privacy

The service must support:

- data minimisation
- PII handling policies
- customer data deletion requests
- audit logging
- secure event ingestion

---

# Common Pitfalls

Avoid:

- tightly coupling raw events to profiles
- synchronous event processing during login
- embedding business rules in multiple layers
- storing duplicated state without clear ownership
- relying on manual updates instead of event-driven systems

---

# Future Enhancements

Potential improvements:

- ML-based lead scoring
- cross-device identity resolution
- predictive intent modelling
- real-time streaming updates
- dynamic segmentation
- behavioural clustering
- lifetime value prediction

---

# Summary

The Customer Profile Service is the **system of record for customer understanding**.

It transforms raw behaviour into structured intelligence used by:

- ranking engines
- personalization systems
- AI augmentation layers

Its core value is:

> turning events into actionable customer intelligence in real time

---

| <- Previous | Next -> |
|---|---|
| [Contentful Integration](./05-contentful-integration.md) | [Ranking Engine](./07-ranking-engine.md) |
