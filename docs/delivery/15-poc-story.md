# POC Story And Presentation Narrative

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: POC Demo Flow <-](./14-poc-demo-flow.md)

## Overview

This document gives the concise story to tell when presenting the POC to product and engineering reviewers.

It is intentionally shorter and more presentation-oriented than the deeper docs. Its job is to help the presenter explain:

- the business problem
- why this POC slice was chosen
- what is real versus mocked
- what success should look like

For the detailed runtime walkthrough, see [POC Demo Flow](./14-poc-demo-flow.md).

---

## Why This POC Slice Was Chosen

The chosen slice is:

- one known customer
- two concurrent journeys
- one active-journey decision
- one deterministic retrieval and ranking path
- one bounded AI explanation step
- one visible next-best action
- one measurable event trail

This is the right slice because it proves the parts that matter most:

1. durable state exists across sessions
2. multiple journeys can be handled without confusion
3. deterministic controls still own protected decisions
4. AI adds value without becoming the policy engine
5. the outcome can be measured end to end

It is narrow enough to build and broad enough to feel real.

---

## The Demo Story In One Sentence

> a returning customer comes back with evidence of both an old health-insurance journey and a new broadband need, and the platform chooses the right journey to lead, returns a clear next step, explains it, and proves what happened through analytics

That sentence is usually enough to anchor the rest of the presentation.

It also gives the presenter a simple way to frame the business problem without repeating the broader platform introduction from the README and audience guides.

---

## The Story Arc To Present

### 1. Start With Customer Reality

Explain that real customers do not behave like a perfect single-funnel flow.

They:

- return
- switch context
- carry old intent and new intent at the same time
- need the platform to decide what matters now

### 2. Show Why Static Routing Is Not Enough

Explain that campaign routing or fixed page flows cannot reliably answer:

- should the old journey resume
- should the new journey lead
- should both be visible
- what next action is safest and most useful

### 3. Show The Platform Response

Then show the architecture doing four things:

1. load durable customer and journey state
2. select the active journey
3. retrieve and rank candidates with deterministic controls
4. add AI explanation and analytics around the result

### 4. End With What Was Proven

Close on what the POC demonstrated:

- the architecture is practical
- the decision is explainable
- the AI boundary is safe
- the telemetry proves what happened

---

## What Is Real Versus Mocked

Reviewers usually want this clarified early.

### Real In The POC

The POC should aim to make these elements real:

- customer and journey identifiers
- durable profile and journey summaries
- active-journey selection logic
- deterministic filtering and ranking path
- next-best-action response payload
- Segment.io event emission for the core flow

### Allowed To Be Mocked Or Simplified

These can be mocked, stubbed, or simplified without weakening the story:

- full provider coverage
- production-grade operational hardening
- large-scale analytics projections
- broad multi-vertical rollout
- advanced experiment orchestration
- richer conversational experiences

### Important Framing

The point is not to pretend the whole platform is finished.

The point is to show that the core architecture pattern is:

- coherent
- buildable
- measurable
- worth extending

---

## What Product Reviewers Should See

Product reviewers should leave believing:

- the platform can choose one journey without losing visibility of others
- next-best-action behavior is explainable
- rollout can be phased cleanly
- success criteria are concrete rather than vague

Good product reactions sound like:

- "I can see why this journey led"
- "I can see what we would tune next"
- "I can see how this becomes a roadmap, not a one-off demo"

---

## What Engineering Reviewers Should See

Engineering reviewers should leave believing:

- the state model is credible
- the request path is bounded
- deterministic and AI responsibilities are cleanly separated
- the output and telemetry contracts are inspectable

Good engineering reactions sound like:

- "I can see the service boundaries"
- "I can see what is synchronous versus asynchronous"
- "I can see how this would be implemented without hidden magic"

---

## What Success Looks Like

The POC should be called successful if reviewers can clearly see all of the following:

### Business And Product Success

- the chosen next-best action feels more relevant than a generic experience
- the returning-customer and multi-journey problem is explained clearly
- the demo makes the platform feel useful, not theoretical

### Technical Success

- the system can show profile and journey reads before the decision
- the active-journey choice is visible and explainable
- deterministic filtering and ranking can be inspected
- AI contribution is visible but bounded

### Measurement Success

- Segment.io events are emitted for the decision path
- the recommendation can be tied to journey and ranking context
- a simple Mixpanel or equivalent view can show the decision and progression trace

---

## Presenter Notes

If time is short, keep the spoken story simple:

1. "Here is the customer and their parallel journeys."
2. "Here is the current session context."
3. "Here is why broadband leads instead of health."
4. "Here is the next-best action returned."
5. "Here is the event trail that proves what happened."

That sequence keeps the presentation easy to follow without losing technical credibility.

---

## Summary

The POC story should make one thing clear:

the platform is not just showing content differently; it is making a better, explainable, measurable decision about which journey and next step should lead the session.

That is the reason this POC is worth building.

---

| <- Previous | Next -> |
|---|---|
| [POC Demo Flow](./14-poc-demo-flow.md) | [Documentation Home](../../README.md#documentation-structure) |
