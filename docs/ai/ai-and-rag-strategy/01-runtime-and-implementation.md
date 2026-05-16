# AI and RAG Strategy: Runtime and Implementation

> **Navigation:** [Docs home](../../../README.md#documentation-structure) | [Parent: AI and RAG Strategy](../08-ai-and-rag-strategy.md) | [Next: Vector Search Design ->](../09-vector-search-design.md)

## Overview

This document covers the concrete runtime design behind the AI layer: how prompts are assembled, how journey interpretation is shaped, how responses are validated, and what should be measured in production.

---

## Suggested Runtime Components

The AI layer becomes easier to implement when it is split into a small set of focused runtime responsibilities.

| Component | Primary role | Typical execution mode |
|---|---|---|
| AI orchestration service | assembles prompts, context, and safety controls | synchronous for lightweight interactions, asynchronous for heavier workflows |
| Retrieval service | fetches vector, keyword, and metadata-grounded context | synchronous |
| Prompt template registry | versions system prompts and task-specific instructions | read-only at runtime |
| Response safety layer | moderation, policy checks, and fallback routing | synchronous |
| AI telemetry pipeline | captures prompt version, model version, grounding set, and response outcome | asynchronous |

This keeps the live request path explicit and prevents prompt logic from leaking across unrelated services.

---

## Journey Interpretation Contract

AI-assisted journey interpretation should produce a **decision-support payload**, not an authoritative decision.

Suggested shape:

```json
{
  "customerId": "12345",
  "sessionId": "session-001",
  "candidateJourneys": [
    {
      "journeyId": "journey-health-001",
      "serviceCategory": "health_insurance"
    },
    {
      "journeyId": "journey-broadband-001",
      "serviceCategory": "broadband"
    }
  ],
  "aiInterpretation": {
    "suggestedJourneyId": "journey-health-001",
    "confidence": 0.74,
    "reasonSummary": "Recent quote behavior and family-cover language suggest the health journey should lead."
  }
}
```

The deterministic journey-selection step can then:

- accept the suggestion
- down-rank it
- ignore it when stronger deterministic evidence exists

This preserves explainability while still allowing AI to help with messy or incomplete intent.

---

## Context Assembly Strategy

Before generation, the orchestration layer should assemble a bounded context package containing:

- retrieved content chunks
- canonical content identifiers
- active-journey summary
- high-signal customer-profile summary
- allowed CTA or next-step options
- disclosure fragments where applicable

Recommended rules:

- prefer compact summaries over raw large documents
- cap the number of retrieved chunks per answer
- keep content IDs and publish revisions attached to each chunk
- separate customer state from managed content so prompts stay readable

---

## Suggested Prompt Structure

Prompt assembly should be explicit and versioned.

Recommended prompt layers:

1. **system prompt** - role, safety boundaries, and tone
2. **task prompt** - explain, summarize, compare, or guide
3. **journey context** - active journey, stage, and known constraints
4. **grounding context** - retrieved Contentful content, FAQs, and disclosures
5. **response contract** - required output fields and formatting constraints

Example response contract:

```json
{
  "summary": "Plain-language explanation of why this option is relevant.",
  "keyPoints": [
    "Family cover focus",
    "Extras emphasis",
    "Quote-ready next step"
  ],
  "ctaSupportText": "You can start a quote without losing your current comparison progress.",
  "groundingAssetIds": [
    "offer-health-family-001",
    "faq-health-extras-003"
  ]
}
```

This makes downstream rendering and evaluation much easier than relying on free-form prose alone.

---

## Response Handling And Fallbacks

AI responses should be accepted only when they satisfy product and safety expectations.

Recommended checks:

- grounding assets are present
- required output fields are populated
- response length stays within configured bounds
- no unsupported policy or eligibility claims are introduced

If the response fails these checks, the platform should:

- drop back to deterministic copy or pre-authored explanation text
- preserve the same next-best-action decision
- emit telemetry showing that the AI layer was bypassed or rejected

The goal is to avoid silent degradation while keeping customer-facing behavior predictable.

---

## Latency And Execution Guidance

Recommended runtime discipline:

- use synchronous AI calls only for short explanation or interpretation tasks
- precompute summaries where possible for high-traffic offers and providers
- move batch summarization, embedding refresh, and heavier enrichment off the request thread
- set hard timeouts so deterministic flows can continue without waiting indefinitely for AI

This keeps AI additive to the journey instead of becoming a bottleneck.

---

## Recommended Observability Fields

For every AI interaction, capture:

- response ID
- AI task type
- experiment assignment
- session ID
- customer ID where available
- model name and version
- prompt template version
- grounding asset IDs
- active journey ID
- service category
- response acceptance or rejection outcome
- rejection or fallback reason where applicable
- CTA or next-step identifier returned
- latency
- timeout outcome
- token usage or cost where useful
- downstream conversion or engagement outcome where available

This creates the minimum traceability needed for product review, engineering debugging, and governance.

---

## Measuring AI Responses In Production

AI response measurement should be treated as a layered scorecard rather than a single quality metric.

Recommended measurement layers:

1. **response quality** - was the answer relevant, clear, and complete for the current journey state
2. **grounding and safety** - was the answer supported by retrieved sources and free from unsupported claims
3. **runtime reliability** - did the response arrive quickly enough and without frequent fallbacks
4. **business impact** - did the response help move the customer toward a qualified next step

This ensures the platform does not confuse "the model said something plausible" with "the AI experience improved conversion support."

### Recommended AI Response Scorecard

Each served response should be measurable against a small, reusable scorecard.

| Dimension | What to score | Example signals |
|---|---|---|
| relevance | whether the response matches the active journey and user question | user follow-on behavior, manual review, low dismissal rate |
| clarity | whether the explanation is understandable and actionable | explanation expansion rate, abandonment after response, review score |
| grounding | whether claims are tied to approved content or disclosures | grounding asset coverage, unsupported-claim defects |
| completeness | whether required fields and next steps are present | response contract validation, missing-field rate |
| next-step usefulness | whether the response supports a meaningful CTA or journey action | CTA click-through, quote-start rate after response |

For higher-risk scenarios such as eligibility guidance, provider comparison, or assisted-sales support, this scorecard should also be sampled through manual review.

### Response Acceptance And Defect Checks

Before the response is shown, the platform should evaluate:

- required output fields present
- one or more grounding asset IDs attached
- no unsupported eligibility, policy, or compliance claims
- disclosure fragments included when required
- response length and formatting within configured bounds

This allows the platform to classify each generation as:

- accepted
- rejected and replaced with deterministic copy
- accepted with escalation or follow-up handling

That classification becomes one of the most important production metrics because it shows whether the AI layer is actually usable.

### Priority Launch Metrics

The first production dashboard should emphasize the metrics that most directly indicate value and risk.

Recommended launch metrics:

- AI response acceptance rate
- grounded-response defect rate
- unsupported-claim or compliance defect rate
- latency and timeout rate
- fallback rate
- CTA progression rate after AI response
- journey-interpretation agreement with deterministic selection
- qualified conversion delta for AI-assisted versus non-AI experiences

These give a practical early read on whether the AI layer is safe, reliable, and commercially useful.

---

## Evaluation Approach

The AI layer should be evaluated on more than anecdotal quality.

Recommended evaluation dimensions:

- grounding quality
- explanation usefulness
- response acceptance rate
- fallback and rejection rate
- CTA progression after AI response
- journey-interpretation agreement with deterministic outcomes
- latency and timeout rate
- escalation or manual-review rate
- customer progression after AI-assisted responses versus non-AI responses

This helps teams identify where AI genuinely improves conversion support and where deterministic handling should remain dominant.

### Suggested Evaluation Workflow

Recommended evaluation approach:

1. define a reusable scenario set across key verticals and funnel stages
2. score responses automatically against the response contract and safety checks
3. review a sampled subset with a human rubric for relevance, clarity, and grounding
4. compare AI-assisted and non-AI experiences in production using experiment assignments
5. tie readouts back to prompt version, model version, grounding set, and content revision

This creates a repeatable operating model for improving prompts and models without losing traceability.

---

## Summary

The implementation layer should make AI inspectable, bounded, and operationally safe while still being useful enough to improve interpretation, explanation, and guided conversion behavior.

---

| <- Previous | Next -> |
|---|---|
| [AI and RAG Strategy](../08-ai-and-rag-strategy.md) | [Vector Search Design](../09-vector-search-design.md) |
