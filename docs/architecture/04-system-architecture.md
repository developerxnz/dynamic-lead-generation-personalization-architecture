# System Architecture

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Contentful Integration ->](../services/05-contentful-integration.md)

## Overview

This document defines the technical architecture for the personalization platform.

---

## Core Services

### Profile Service

Responsibilities:

- manage customer state
- aggregate behavioral data
- calculate lead scores

Suggested technology:

- .NET
- Cosmos DB

---

### Personalization Service

Responsibilities:

- determine customer intent
- retrieve candidate content
- perform ranking
- return personalized results

Suggested architecture:

- CQRS-friendly design
- isolated domain services

---

### Contentful Adapter

Responsibilities:

- query Contentful
- normalize content
- map metadata into domain models

Suggested implementation:

- GraphQL client
- infrastructure adapter layer

---

## Login Flow

```text
Login Event
   ↓
Load Customer State
   ↓
Update Intent Signals
   ↓
Retrieve Candidate Content
   ↓
Rank Content
   ↓
Return Personalized Experience
```

For login-triggered personalization, `Load Customer State` should use the latest committed customer profile projection, or a bounded-staleness equivalent with an explicit freshness target, before candidate retrieval and ranking continue.

---

## Architectural Principles

- keep business logic server-side
- keep frontend rendering lightweight
- isolate ranking logic
- avoid coupling AI directly into core flows
- support asynchronous analytics processing

---

| <- Previous | Next -> |
|---|---|
| [Content Personalization Strategy](./03-content-personalization-strategy.md) | [Contentful Integration](../services/05-contentful-integration.md) |
