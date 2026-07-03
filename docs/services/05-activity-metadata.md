# Activity Metadata

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Next: Customer Profile Service ->](./06-customer-profile-service.md)

## Overview

Existing activities should remain the source of truth for what can be shown to a customer, with the required personalization metadata added directly onto those activities.

It should not become the place where:

- ranking policy lives
- suitability rules are enforced
- journey selection logic is embedded
- AI prompts or system decisioning are hardcoded into activity records

Activities should describe what exists and how it is configured. Backend services should decide what should be shown, to whom, and why.

---

## Role Of Activity Metadata In The Platform

Activity metadata is most valuable when it makes existing activities retrievable and governable without destabilizing decision systems.

It should enable teams to manage:

- offers and offer variants
- provider-specific activity definitions
- disclosures and supporting guidance
- CTAs and deep links
- educational or comparison activities
- campaign-aware variants where needed

while the platform handles:

- active-journey selection
- qualification and suitability checks
- ranking
- AI-assisted interpretation and explanation

---

## Recommended Activity Metadata Model

### Universal Metadata Fields

Every activity should carry a common metadata backbone.

| Field | Purpose |
|---|---|
| service_category | Which journey or vertical the asset belongs to |
| subtype | More specific classification inside the category |
| provider | Provider or partner association |
| region | Geographic availability |
| funnel_stage | Discover, research, compare, quote, apply, renew, or resume |
| conversion_goal | Intended business outcome |
| cta_type | Quote, callback, compare, check eligibility, apply, resume |
| cta_deep_link | Deep-link destination or route template used when the CTA is rendered |
| compliance_flags | Approval and disclosure requirements |
| freshness | Recency and validity relevance |
| priority | Explicit business control |
| lifecycle_status | Draft, approved, active, expired, withdrawn |
| metadata_revision | Traceable version of the activity metadata used in decisions |
| campaign_owner | Operational accountability |

### Service-Specific Extensions

Examples:

- **Novated leasing:** employer requirement, vehicle type, tax-benefit theme
- **Health insurance:** cover tier, household fit, extras focus
- **Broadband:** speed tier, access technology, contract length

### AI Support Fields

To support AI-assisted experiences well, the model should also allow:

- short plain-language summary
- approved explainer text
- FAQ fragments
- structured objections or reassurance points
- retrieval tags or semantic hints

These fields improve AI grounding without moving control away from approved activity definitions.

---

## Activity Types To Model Explicitly

Recommended first-class activity types:

- offer
- provider profile
- educational explainer
- comparison module
- calculator or tool descriptor
- CTA definition
- disclosure or compliance asset
- AI grounding fragment

This helps product and engineering reason about how each activity enters candidate retrieval and final slot composition.

---

## Suggested Activity Type Mapping

The implementation becomes clearer if each activity type maps to a normalized domain object.

| Activity type | Normalized domain object | Primary usage |
|---|---|---|
| offer | `OfferCandidate` | ranking and CTA generation |
| provider profile | `ProviderDescriptor` | comparison and explainer content |
| explainer article | `GuidanceAsset` | research and reassurance slots |
| CTA definition | `ActionDefinition` | next-best-action rendering |
| disclosure asset | `DisclosureReference` | compliance support |
| AI grounding fragment | `GroundingSnippet` | RAG and explanation generation |

This avoids making downstream services understand raw activity-source shapes directly.

---

## Query Strategy

The activity query layer should support broad candidate retrieval based on:

- service-category alignment
- region and provider availability
- funnel stage fit
- campaign context
- lifecycle and approval state

Avoid encoding business rules directly in activity queries beyond hard constraints such as:

- inactive activities
- expired offers
- missing compliance approval
- withdrawn provider assets

This keeps activity metadata responsible for availability and description, while backend services remain responsible for decision logic.

---

## Normalization Requirements

The adapter should transform activities into stable domain models that include:

- normalized identifiers
- provider and service taxonomy
- structured CTA definitions
- deep-link targets or route templates for CTA execution
- references to disclosures and eligibility rules
- metadata and expiry timestamps
- retrieval-friendly summaries
- asset version or metadata revision

Normalization is important because the ranking and AI layers should consume a predictable domain model, not raw activity-source shapes.

### Example Normalized Asset

```json
{
  "assetId": "offer-health-family-001",
  "assetType": "OfferCandidate",
  "serviceCategory": "health_insurance",
  "provider": "Provider A",
  "funnelStages": ["compare", "quote"],
  "conversionGoal": "start_quote",
  "cta": {
    "type": "get_quote",
    "label": "Get a family cover quote",
    "deepLink": "/quote/health-insurance?family=true"
  },
  "disclosures": ["disc-health-001"],
  "retrievalSummary": "Family cover with extras and quote-ready positioning",
  "lifecycle": {
    "status": "active",
    "metadataUpdatedAt": "2026-05-10T08:00:00Z",
    "expiresAt": "2026-06-30T23:59:59Z"
  },
  "metadataRevision": "offer-health-family-001@17"
}
```

---

## Integration Architecture

```mermaid
flowchart TD
    A[Existing activities] --> B[Activity metadata adapter]
    B --> C[Normalized domain models]
    C --> D[Candidate retrieval]
    C --> E[AI grounding and retrieval index]
    C --> F[Ranking and personalization]
```

The adapter layer should isolate:

- activity-source schema details
- query logic
- metadata-state handling
- cache invalidation behavior

from the rest of the platform.

---

## Update And Sync Flow

Recommended update flow:

```mermaid
flowchart TD
    A[Activity metadata is updated] --> B[Change event or scheduled sync]
    B --> C[Activity metadata adapter refresh]
    C --> D[Normalized asset update]
    D --> E[Cache invalidation]
    D --> F[Vector or AI grounding refresh if needed]
```

Implementation notes:

- invalidate only the affected normalized assets where possible
- refresh embeddings only when meaning-bearing fields change
- record metadata revision so decision traces can refer to the exact activity version served

---

## Integration Recommendations

- use the existing activity APIs
- normalize responses into domain models
- isolate activity-source logic in infrastructure adapters
- support change-aware caching
- emit change events when metadata changes affect personalization
- version normalized assets so ranking and analytics can reference what was actually served

---

## Caching And Invalidation Detail

Recommended cache boundaries:

- cache raw source responses only inside the adapter
- cache normalized assets for downstream retrieval
- treat metadata updates, expiry, and approval changes as invalidation triggers
- keep AI grounding and vector-index refresh decoupled from live retrieval where possible

This reduces source-system load without allowing stale or non-compliant activities to remain in circulation.

---

## Governance And Operating Model

The activity metadata model should support collaboration across:

- marketing
- provider management
- operations
- compliance
- product
- engineering

That requires explicit fields for:

- approvals
- expiry
- disclosures
- campaign ownership
- provider ownership
- content stewardship

Recommended governance questions:

- who can change activity metadata
- who approves AI grounding text
- who owns compliance-sensitive edits
- who owns and validates CTA deep-link targets
- how withdrawn activities are suppressed across channels

---

## Common Pitfalls

Avoid:

- storing ranking rules in activity metadata
- making the activity source the source of truth for suitability
- using freeform content fields where structured fields are needed
- under-modeling approval and lifecycle state
- treating AI grounding content as uncontrolled editorial text

---

## Summary

Activity metadata should act as the platform's source of describable, retrievable options, not its decision engine.

Its job is to make high-quality, well-governed activities available for:

- deterministic candidate retrieval
- AI grounding and semantic retrieval
- personalized experience assembly

while backend services retain control of ranking, qualification, and journey decisioning.

---

| <- Previous | Next -> |
|---|---|
| [System Architecture](../architecture/04-system-architecture.md) | [Customer Profile Service](./06-customer-profile-service.md) |
