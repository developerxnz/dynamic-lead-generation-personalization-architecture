# Dynamic Lead Generation Personalization Platform

## Overview

This repository documents a presentation-ready architecture for an **AI-powered, multi-vertical lead-generation platform**.

It is designed for service businesses that need one decisioning system across categories such as:

- novated leasing
- health insurance
- broadband and utilities
- adjacent quote-led, callback-led, or application-led services

The platform's core job is to decide which journey should lead the current session, which offer and CTA combination best fits that context, and how to improve **qualified conversion** without surrendering deterministic control over eligibility, suitability, ranking, or compliance.

---

## Read By Audience

| Audience | Best starting point | What to expect |
|---|---|---|
| **Marketing and growth** | [Marketing and Growth Guide](./docs/audiences/marketing-and-growth.md) | business outcomes, campaign implications, content strategy, and success metrics |
| **Product and delivery** | [Product and Delivery Guide](./docs/audiences/product-and-delivery.md) | journey design, prioritization, roadmap, measurement, and operating-model choices |
| **Engineering and architecture** | [Engineering and Architecture Guide](./docs/audiences/engineering-and-architecture.md) | service boundaries, state models, event flows, contracts, and implementation detail |

---

## Core Definitions

These definitions are intended to make the rest of the documentation easier to read across marketing, product, engineering, and data audiences.

| Term | Plain-language meaning | Why it matters |
|---|---|---|
| **Journey** | A service-specific path a customer is currently exploring, progressing, resuming, or renewing. | A customer can have more than one journey at the same time, so the platform cannot assume one fixed path forever. |
| **Active journey** | The journey that should drive the current session experience right now. | The platform may track multiple journeys, but it still needs one primary context for live decisioning. |
| **Journey summary** | A compact, decision-ready snapshot of a journey state, such as current intent, stage, qualification status, recent behavior, and score. | AI, ranking, and orchestration should read a bounded summary instead of raw event history. |
| **Customer summary** | A compact snapshot of cross-journey customer facts such as return status, lead score, and recent meaningful activity. | This gives downstream systems reusable customer context without overloading them with full profile history. |
| **Next best action** | The most appropriate next thing the platform wants the customer to do, such as start a quote, compare options, check eligibility, request a callback, or resume a flow. | The platform is not just choosing content; it is choosing the best conversion-supporting action for the moment. |
| **Eligibility** | Whether the customer can proceed based on hard requirements, serviceability, region, employer support, or other factual constraints. | Ineligible actions should not be promoted, even if they look commercially attractive. |
| **Suitability** | Whether an option is appropriate to show after considering intent, stage, context, and policy constraints. | Something can be technically available but still be the wrong thing to promote in the current journey. |
| **Retrieval** | The step that finds a broad candidate set of offers, content, tools, and CTAs that could be relevant. | Retrieval discovers possibilities; it should not be the final decision layer. |
| **Ranking** | The step that orders the remaining candidates after deterministic filters and constraints are applied. | Ranking decides what is safest and most commercially useful to show from the candidate set. |
| **AI support signals** | AI-generated or AI-assisted signals that help interpretation, retrieval, explanation, or summarization without becoming authoritative policy decisions. | AI can improve relevance and clarity, but deterministic systems remain responsible for protected decisions. |

These definitions are expanded further in the architecture, customer-state, ranking, and AI sections.

---

## ID Glossary

These IDs appear throughout the mock scenarios, AI prompts, ranking responses, and analytics events.

| ID type | Example | Meaning | Typical use |
|---|---|---|---|
| `customer_id` | `cust-20481` | The durable customer/profile record. | Joins profile, journey, and analytics data across sessions. |
| `session_id` | `sess-77821` | One live visit or interaction window. | Ties together the events and decisions for a single session. |
| `journey_id` | `journey-broadband-118` | One service-specific journey state for a customer. | Used for active-journey selection, ranking context, and event attribution. |
| `content_id` | `action-bbd-address-check-001` | The content or action chosen for presentation. | Used by ranking, orchestration, and the channel response payload. |
| `asset_id` | `guide-bbd-moving-home-001` | The canonical underlying asset behind a grounded content item. | Used in AI grounding citations and traceability. |
| `snippet_id` | `gs-bbd-moving-home-001` | A specific snippet extracted from an asset for prompt grounding. | Prompt-packaging only; not the canonical ID the AI should return. |
| `ai_response_id` | `air-101` | One accepted AI-generated explanation record. | Used for audit, telemetry, and debugging of AI-assisted responses. |
| `metadata_revision` | `action-bbd-address-check-001@5` | A versioned metadata record for a content asset. | Shows exactly which metadata revision was used when serving a recommendation. |

In short: `content_id` is what the platform shows, `asset_id` is the canonical asset behind it, and `snippet_id` is a prompt-level slice of that asset used to ground the AI response.

---

## High-Level Platform Flow

At a high level, the platform works as a loop:

1. a customer session or trigger enters the platform
2. the profile layer loads current customer and journey context
3. AI interpretation and deterministic controls help shape the candidate set
4. retrieval and ranking choose the best next action
5. the experience is delivered in channel
6. behavior and outcome events feed back into the profile and analytics layers

This flow is deliberately built around a feedback loop. A customer is not evaluated once and then forgotten. The system should continuously improve its view of both:

- **what the customer is trying to do now**
- **what AI can infer about the best journey, content, and support for that customer**
- **what tends to convert well for similar customers over time**

---

## Recommended Technology Stack

| Area | Technology |
|---|---|
| Experience and decisioning services | .NET |
| Operational profile storage | Cosmos DB |
| Activity metadata source | Existing activities plus a metadata adapter |
| AI services | Azure OpenAI |
| Semantic search | Azure AI Search |
| APIs | GraphQL + REST |
| Hosting and integration | Azure |

These choices reflect the current reference architecture, not a product requirement that every deployment must follow unchanged.

---

## Local AI Evaluation In The Devcontainer

The devcontainer includes the GitHub CLI in the app container and an `ollama` sidecar container for running the POC AI scenarios locally.

When the devcontainer is created, it will:

1. install the Python dependencies from `requirements.txt`
2. wait for Ollama to become reachable at `http://ollama:11434`
3. pull the default model configured by `OLLAMA_MODEL` if it is not already present

The default model is `llama3.1:8b`. You can override it before running the evaluation script:

```bash
OLLAMA_MODEL=phi4:14b ./scripts/setup-ollama.sh
MODEL=phi4:14b python scripts/evaluate.py
```

The first container create can take a while because the configured Ollama model is downloaded into the persistent `ollama_data` volume.

To re-pull or switch models manually at any time:

```bash
./scripts/pull-model.sh
./scripts/pull-model.sh phi4:14b
python scripts/evaluate.py
```

---

## Documentation Structure

The documents below now support both a **linear architecture narrative** and **audience-based reading paths**.

---

## Core Narrative

These documents move from business framing into implementation guidance.

## Architecture

| Document | Description |
|---|---|
| [01-overview.md](./docs/architecture/01-overview.md) | Platform framing, business goals, end-to-end flow, and core principles |
| [02-customer-state-model.md](./docs/architecture/02-customer-state-model.md) | Durable profile, journey state, journey summaries, and active-journey selection |
| [03-content-personalization-strategy.md](./docs/architecture/03-content-personalization-strategy.md) | Overview of offer, content, CTA, and next-best-action decisioning |
| [04-system-architecture.md](./docs/architecture/04-system-architecture.md) | Technical architecture, runtime topology, service boundaries, and entry-point contracts |

### Architecture Deep Dives

| Document | Description |
|---|---|
| [worked-example/01-returning-customer-multi-journey.md](./docs/architecture/worked-example/01-returning-customer-multi-journey.md) | Concrete end-to-end scenario showing active-journey selection, retrieval, deterministic ranking, AI-assisted explanation, and telemetry |
| [content-personalization-strategy/01-runtime-decisioning.md](./docs/architecture/content-personalization-strategy/01-runtime-decisioning.md) | Runtime personalization model covering active-journey selection, candidate retrieval, and ranking flow |
| [content-personalization-strategy/02-metadata-and-rules.md](./docs/architecture/content-personalization-strategy/02-metadata-and-rules.md) | Content metadata model, personalization dimensions, and deterministic promotion rules |
| [content-personalization-strategy/03-ai-edge-cases-and-performance.md](./docs/architecture/content-personalization-strategy/03-ai-edge-cases-and-performance.md) | AI usage boundaries, conflict handling, fallback behavior, and performance guidance for personalization |
| [content-personalization-strategy/04-illustrative-examples.md](./docs/architecture/content-personalization-strategy/04-illustrative-examples.md) | Novated-leasing examples and visuals showing how personalization changes through the funnel |

---

## Services

| Document | Description |
|---|---|
| [05-activity-metadata.md](./docs/services/05-activity-metadata.md) | Activity metadata design, governance, and operating model |
| [06-customer-profile-service.md](./docs/services/06-customer-profile-service.md) | Service overview, audience summary, and links into detailed state, persistence, and API design |
| [07-ranking-engine.md](./docs/services/07-ranking-engine.md) | Decisioning overview, audience summary, and links into scoring, policy, and runtime details |

### Service Deep Dives

| Document | Description |
|---|---|
| [customer-profile-service/01-state-and-persistence.md](./docs/services/customer-profile-service/01-state-and-persistence.md) | Customer and journey state model, scoring responsibilities, persistence split, and storage choices |
| [customer-profile-service/02-event-processing-and-apis.md](./docs/services/customer-profile-service/02-event-processing-and-apis.md) | Event model, processing guarantees, API contracts, and query/read guidance |
| [ranking-engine/01-scoring-model-and-policy.md](./docs/services/ranking-engine/01-scoring-model-and-policy.md) | Ranking inputs, weighted scoring model, policy controls, diversity rules, and configuration shape |
| [ranking-engine/02-runtime-and-contracts.md](./docs/services/ranking-engine/02-runtime-and-contracts.md) | Request and response contracts, runtime algorithm, explainability, and performance boundaries |

---

## AI

| Document | Description |
|---|---|
| [08-ai-and-rag-strategy.md](./docs/ai/08-ai-and-rag-strategy.md) | AI boundaries, plain-language limits, RAG strategy, and assistive use cases |
| [09-vector-search-design.md](./docs/ai/09-vector-search-design.md) | Semantic retrieval for offers, guidance, and lead-support journeys |

### AI Deep Dives

| Document | Description |
|---|---|
| [ai-and-rag-strategy/01-runtime-and-implementation.md](./docs/ai/ai-and-rag-strategy/01-runtime-and-implementation.md) | Runtime components, journey-interpretation contracts, prompt assembly, fallbacks, observability, and evaluation |
| [vector-search-design/01-index-and-retrieval-implementation.md](./docs/ai/vector-search-design/01-index-and-retrieval-implementation.md) | Index schema, ingestion pipeline, query contracts, tuning, caching, and retrieval evaluation |

---

## Operations

| Document | Description |
|---|---|
| [10-feedback-and-analytics.md](./docs/operations/10-feedback-and-analytics.md) | Analytics overview, measurement boundary, and links into success measurement and telemetry design |

### Operations Deep Dives

| Document | Description |
|---|---|
| [feedback-and-analytics/01-success-measurement.md](./docs/operations/feedback-and-analytics/01-success-measurement.md) | Outcome model, guardrails, dashboard ownership, and measurement-enablement changes |
| [feedback-and-analytics/02-event-model-and-dashboards.md](./docs/operations/feedback-and-analytics/02-event-model-and-dashboards.md) | Event taxonomy, Segment-to-Mixpanel flow, dashboard definitions, projections, and experimentation support |

---

## Delivery

| Document | Description |
|---|---|
| [11-roadmap.md](./docs/delivery/11-roadmap.md) | Phased rollout across deterministic, behavioral, and AI-assisted capabilities |
| [12-poc-scope.md](./docs/delivery/12-poc-scope.md) | Focused proof-of-concept scope, success criteria, demo audience, and live-versus-described implementation cut |
| [13-ownership-and-operating-model.md](./docs/delivery/13-ownership-and-operating-model.md) | Cross-functional ownership matrix for decisioning, content, telemetry, analytics, AI, and operational review |
| [14-poc-demo-flow.md](./docs/delivery/14-poc-demo-flow.md) | Step-by-step POC walkthrough with example requests, responses, decision trace, and analytics events |
| [15-poc-story.md](./docs/delivery/15-poc-story.md) | Concise presentation narrative for the POC: chosen slice, presenter framing, reviewer takeaways, and real-versus-mocked guidance |
| [16-mock-data-scenarios.md](./docs/delivery/16-mock-data-scenarios.md) | Mock data for the three POC scenarios: full end-to-end trace files for validating decisioning and AI model outputs |

---

## Recommended Delivery Approach

### Phase 1 - AI-Assisted Decisioning Foundation

Initial implementation should focus on:

- customer profiles and journey states
- service metadata and offer taxonomy
- deterministic eligibility and suitability rules
- AI-assisted intent interpretation and semantic retrieval
- vertical-aware ranking and next-best-action selection
- basic analytics for lead quality and provider handoff

This establishes an AI-enabled operating model while keeping the first release controlled and explainable.

### Phase 2 - AI-Optimized Personalization

Enhance personalization using:

- richer behavioral tracking
- intent refinement
- returning-customer recognition and re-entry logic
- AI-assisted recommendation tuning and explanation generation
- lead-quality analytics by channel, vertical, and provider
- configurable weighting of ranking signals by vertical

This phase should improve both conversion efficiency and journey relevance at higher scale.

### Phase 3 - AI-Native Guidance And Orchestration

Introduce:

- conversational guidance
- proactive AI journey orchestration
- deeper semantic retrieval and answer generation
- personalized summaries and recommendation explanations
- RAG-based support for more complex research and assisted-sales journeys

At this stage, AI becomes a major part of how the platform interacts with customers and internal teams, while deterministic systems remain authoritative for critical controls.
