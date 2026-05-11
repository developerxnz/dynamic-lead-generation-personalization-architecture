# Contentful Integration

## Overview

Contentful should remain the source of truth for content.

Business logic and personalization should not live in the CMS.

---

## Content Model Requirements

Recommended metadata fields:

- persona_fit
- funnel_stage
- topics
- conversion_goal
- cta_type
- experience_level
- tags
- freshness

---

## Query Strategy

Use broad candidate retrieval:

- persona-aligned queries
- topic-aligned queries
- funnel-stage filtering

Avoid highly restrictive queries.

---

## Integration Recommendations

- use GraphQL APIs
- normalize responses into domain models
- isolate CMS logic in infrastructure adapters
- support caching where appropriate