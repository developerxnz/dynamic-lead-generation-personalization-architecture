# Copilot Instructions

## Build, test, and lint

This repository is documentation-only. There are no project manifests, build scripts, lint scripts, or test runners checked into the repo right now.

Because no automated test harness exists here, there is also no repo-defined command for running a single test.

## High-level architecture

The repository documents a **multi-vertical lead-generation platform** rather than an implemented application. Read `README.md` first, then follow the numbered docs in order.

The big-picture flow described across the docs is:

```text
Customer Session / Trigger
  -> Lead Profile Service
  -> Intent + Eligibility Evaluation
  -> CMS / Offer Catalog Query
  -> Ranking + Suitability Engine
  -> Next Best Action Selection
  -> Web / App / Assisted Sales Experience
```

Important architectural relationships are spread across multiple files:

- `docs/architecture/02-customer-state-model.md` defines the lead profile: service interests, household/employment attributes, intent, eligibility, urgency, renewal, and lead score.
- `docs/architecture/03-content-personalization-strategy.md` defines how offers/content/CTAs are selected: broad candidate retrieval first, then deterministic eligibility/suitability filtering, then ranking.
- `docs/services/05-contentful-integration.md` and `docs/architecture/04-system-architecture.md` make the system boundary explicit: the CMS owns managed content and offer metadata, while decisioning logic stays in backend services.
- `docs/services/06-customer-profile-service.md`, `docs/services/07-ranking-engine.md`, and `docs/operations/10-feedback-and-analytics.md` together describe the event-driven operational model: ingest structured events, build lead state projections, rank deterministically, and measure qualified conversion outcomes.
- `docs/ai/08-ai-and-rag-strategy.md` and `docs/ai/09-vector-search-design.md` define AI as an augmentation layer only. AI can help with intent interpretation, summarization, conversational guidance, and retrieval, but deterministic systems remain authoritative for ranking, eligibility, suitability, and business constraints.

## Key conventions

- Treat `README.md` as the documentation index and entry point. The numbered docs in `docs/` are intended to be read as a sequence, and each page includes top and bottom navigation links that should stay consistent when documents are added or renamed.
- Keep the repository framed as **multi-vertical lead generation**. Examples should use service categories like novated leasing, health insurance, broadband, and similar quote/application/callback-driven services rather than software-product examples.
- Optimize the documented decisioning model for **qualified conversion**, not generic engagement. Preferred metrics and outcomes are quote starts/completions, application progression, callback requests, provider handoff quality, and downstream activation proxies.
- Preserve the separation of concerns used throughout the docs: CMS/offer catalog content is metadata and managed copy; backend services own intent evaluation, eligibility, suitability, ranking, and business constraints.
- Prefer **broad candidate retrieval plus downstream filtering/ranking** over encoding heavy business logic in CMS queries. Hard constraints like unpublished content, expired offers, unsupported regions, or missing compliance approval can be filtered early.
- Keep **deterministic systems authoritative**. AI can assist with explanation, semantic retrieval, and conversational guidance, but should not be described as the owner of ranking, lead scoring, eligibility, suitability, or compliance enforcement.
- When extending schemas or examples, use the existing lead-gen vocabulary: `service_category`, `provider`, `region`, `funnel_stage`, `conversion_goal`, `cta_type`, `compliance_flags`, eligibility signals, renewal timing, and provider handoff outcomes.
