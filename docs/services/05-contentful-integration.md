# Contentful Integration

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Customer Profile Service ->](./06-customer-profile-service.md)

## Overview

Contentful should remain the source of truth for managed content, campaign copy, provider descriptions, and offer metadata.

Business logic, suitability rules, and ranking should not live in the CMS.

---

## Content Model Requirements

### Universal Metadata Fields

Recommended fields:

- service_category
- subtype
- provider
- region
- funnel_stage
- conversion_goal
- cta_type
- compliance_flags
- freshness
- priority
- lifecycle_status

---

### Service-Specific Extensions

Examples:

- novated leasing: employer_requirement, vehicle_type, tax_benefit_theme
- health insurance: cover_tier, household_fit, extras_focus
- broadband: speed_tier, access_technology, contract_length

---

## Query Strategy

Use broad candidate retrieval based on:

- service-category alignment
- region and provider availability
- funnel stage
- campaign context

Avoid encoding business rules directly in CMS queries beyond hard constraints such as:

- unpublished content
- expired offers
- missing compliance approval

---

## Normalization Requirements

The adapter should transform Contentful entries into stable domain models that include:

- normalized identifiers
- provider and service taxonomy
- structured CTA definitions
- references to disclosures and eligibility rules
- publish and expiry timestamps

---

## Integration Recommendations

- use GraphQL APIs
- normalize responses into domain models
- isolate CMS logic in infrastructure adapters
- support publish-aware caching
- emit change events when metadata changes affect personalization

---

## Governance

The CMS model should support collaboration across:

- marketing
- provider management
- operations
- compliance
- product and engineering

That requires explicit fields for approvals, expiry, disclosures, and campaign ownership.

---

| <- Previous | Next -> |
|---|---|
| [System Architecture](../architecture/04-system-architecture.md) | [Customer Profile Service](./06-customer-profile-service.md) |
