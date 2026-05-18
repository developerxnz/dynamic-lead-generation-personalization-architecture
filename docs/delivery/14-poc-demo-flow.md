# POC Demo Flow

> **Navigation:** [Docs home](../../README.md#documentation-structure) | [Previous: Ownership And Operating Model <-](./13-ownership-and-operating-model.md) | [Next: POC Story And Presentation Narrative ->](./15-poc-story.md)

## Overview

This document turns the POC scope into a concrete end-to-end demo flow.

It is intended to answer the practical questions product and engineering readers often ask next:

- what exactly happens during the demo
- what payloads move between services
- what decision trace should be visible
- which analytics events prove the POC worked

This flow is intentionally narrow. For scenario framing and presenter narrative, use [POC Scope](./12-poc-scope.md), [Worked Example: Returning Customer With Multiple Journeys](../architecture/worked-example/01-returning-customer-multi-journey.md), and [POC Story And Presentation Narrative](./15-poc-story.md).

---

## End-To-End POC Flow

```text
Customer session starts
       ↓
Load customer profile and journey summaries
       ↓
Select active journey
       ↓
Retrieve candidate content and offers
       ↓
Apply deterministic suitability and suppression
       ↓
Rank remaining candidates
       ↓
Generate optional AI explanation
       ↓
Return next-best-action response
       ↓
Emit decision-trace and outcome events
```

---

## Step 0: Starting State

Before the live request, the customer profile service already contains:

- one durable customer profile
- at least two current journey summaries
- journey scores and resume indicators

### Example Customer Read

```json
{
  "customerId": "cust-20481",
  "profile": {
    "householdType": "family",
    "employmentType": "full_time",
    "location": "NSW"
  },
  "customerSummary": {
    "isReturningCustomer": true,
    "leadScore": 81,
    "repeatSessions30d": 4
  }
}
```

### Example Journey Read

```json
{
  "customerId": "cust-20481",
  "journeys": [
    {
      "journeyId": "journey-health-301",
      "serviceCategory": "health_insurance",
      "intent": "comparing_options",
      "stage": "compare",
      "resumeCandidate": true,
      "journeyScore": 0.68
    },
    {
      "journeyId": "journey-broadband-118",
      "serviceCategory": "broadband",
      "intent": "moving_home",
      "stage": "research",
      "resumeCandidate": false,
      "journeyScore": 0.74
    }
  ]
}
```

## Step 1: Session Enters The Orchestrator

The channel sends one request that includes customer identity plus current-session context.

### Example Request

```json
{
  "customerId": "cust-20481",
  "sessionId": "sess-77821",
  "channel": "web",
  "entryPoint": "paid_search",
  "campaignTheme": "move-home-broadband",
  "currentUrl": "/broadband/moving-home",
  "queryText": "best internet for new house",
  "region": "NSW"
}
```

## Step 2: Active-Journey Selection

The orchestrator combines:

- customer summary
- journey summaries
- campaign and page context
- current-session signals

to select one active journey.

### Example Decision Result

```json
{
  "selectedJourneyId": "journey-broadband-118",
  "selectedServiceCategory": "broadband",
  "reasonSummary": "broadband move-home signals are more current than the older health comparison journey"
}
```

## Step 3: Candidate Retrieval

The content adapter or retrieval layer returns a broad candidate set for the active journey plus limited secondary support.

### Example Candidate Query Shape

```json
{
  "activeJourney": {
    "serviceCategory": "broadband",
    "stage": "research",
    "intent": "moving_home"
  },
  "context": {
    "region": "NSW",
    "channel": "web"
  }
}
```

### Example Candidate Set

```json
[
  {
    "contentId": "bbd-cta-address-check",
    "serviceCategory": "broadband",
    "ctaType": "check_eligibility",
    "ctaDeepLink": "/broadband/address-check",
    "provider": "Provider A"
  },
  {
    "contentId": "bbd-offer-fast-family",
    "serviceCategory": "broadband",
    "ctaType": "compare",
    "ctaDeepLink": "/broadband/compare?family=true",
    "provider": "Provider B"
  },
  {
    "contentId": "health-resume-compare",
    "serviceCategory": "health_insurance",
    "ctaType": "resume",
    "ctaDeepLink": "/health-insurance/compare/resume",
    "provider": "Provider H"
  }
]
```

## Step 4: Deterministic Filtering And Ranking

The ranking engine receives the active journey, profile summary, and candidate set.

### Example Ranking Request

```json
{
  "customerProfile": {
    "customerId": "cust-20481",
    "leadScore": 81,
    "location": "NSW"
  },
  "activeJourney": {
    "journeyId": "journey-broadband-118",
    "serviceCategory": "broadband",
    "intent": "moving_home",
    "stage": "research"
  },
  "context": {
    "channel": "web",
    "campaignSource": "paid_search"
  },
  "candidates": [
    {
      "contentId": "bbd-cta-address-check",
      "serviceCategory": "broadband",
      "ctaType": "check_eligibility",
      "ctaDeepLink": "/broadband/address-check"
    },
    {
      "contentId": "bbd-offer-fast-family",
      "serviceCategory": "broadband",
      "ctaType": "compare",
      "ctaDeepLink": "/broadband/compare?family=true"
    },
    {
      "contentId": "health-resume-compare",
      "serviceCategory": "health_insurance",
      "ctaType": "resume",
      "ctaDeepLink": "/health-insurance/compare/resume"
    }
  ]
}
```

### Example Ranking Response

```json
{
  "rankedRecommendations": [
    {
      "contentId": "bbd-cta-address-check",
      "score": 34,
      "reasons": [
        "Active journey fit: broadband moving-home journey",
        "CTA alignment: check_eligibility",
        "Intent alignment: moving_home"
      ]
    },
    {
      "contentId": "bbd-offer-fast-family",
      "score": 28,
      "reasons": [
        "Active journey fit: broadband",
        "Behavioral relevance: family household"
      ]
    }
  ],
  "suppressedCandidates": [
    {
      "contentId": "health-resume-compare",
      "reason": "secondary_journey_not_primary"
    }
  ]
}
```

## Step 5: Optional AI Explanation

The AI layer can now generate support text for the selected result.

### Example AI Output

```json
{
  "summary": "Because you're moving soon, the fastest next step is to check which broadband options are actually available at your new address.",
  "ctaSupportText": "Start with address availability so you only compare plans that can be connected.",
  "groundingAssetIds": [
    "bbd-cta-address-check",
    "bbd-guide-move-home"
  ]
}
```

AI should remain visibly bounded:

- it did not select the active journey on its own
- it did not decide eligibility
- it did not override the ranking result

---

## Step 6: Final Experience Response

The orchestrator assembles the final payload returned to the channel.

### Example Response

```json
{
  "customerId": "cust-20481",
  "sessionId": "sess-77821",
  "activeJourney": {
    "journeyId": "journey-broadband-118",
    "serviceCategory": "broadband"
  },
  "nextBestAction": {
    "contentId": "bbd-cta-address-check",
    "ctaType": "check_eligibility",
    "label": "Check broadband options at your new address",
    "deepLink": "/broadband/address-check"
  },
  "supportingContent": [
    "bbd-offer-fast-family"
  ],
  "secondaryJourneyPrompt": {
    "journeyId": "journey-health-301",
    "label": "Resume your health cover comparison"
  },
  "explanation": {
    "source": "ai_assisted",
    "summary": "Because you're moving soon, the fastest next step is to check which broadband options are actually available at your new address."
  }
}
```

## Decision Trace The Demo Should Surface

### Minimum Decision Trace

| Layer | Example output |
|---|---|
| profile read | known customer, family household, NSW |
| journey read | health compare journey + broadband move-home journey |
| active-journey selection | broadband selected because session evidence is stronger |
| retrieval | 3 candidates returned |
| filtering | health resume prompt retained only as secondary support |
| ranking | address check ranked first |
| AI explanation | short grounded explanation generated |

### Example Trace Event

```json
{
  "eventType": "recommendation_served",
  "customerId": "cust-20481",
  "journeyId": "journey-broadband-118",
  "sessionId": "sess-77821",
  "metadata": {
    "activeJourney": "broadband",
    "topRecommendation": "bbd-cta-address-check",
    "rankingPolicyVersion": "broadband-v1",
    "contentRevision": "bbd-cta-address-check@4"
  }
}
```

---

## Analytics Events The Demo Should Emit

### Minimum Event Set

| Event | Why it matters |
|---|---|
| `active_journey_selected` | proves the platform made an explicit journey choice |
| `recommendation_served` | records the promoted result and policy version |
| `cta_impression` | shows the next-best action was actually rendered |
| `cta_clicked` | shows progression into the next step |
| `eligibility_checked` | shows qualified movement, not just engagement |
| `ai_response_accepted` | shows the AI layer was grounded and usable |

### Example Events

```json
{
  "eventType": "active_journey_selected",
  "customerId": "cust-20481",
  "journeyId": "journey-broadband-118",
  "sessionId": "sess-77821",
  "metadata": {
    "candidateJourneys": [
      "journey-health-301",
      "journey-broadband-118"
    ],
    "selectedServiceCategory": "broadband"
  }
}
```

```json
{
  "eventType": "ai_response_accepted",
  "customerId": "cust-20481",
  "journeyId": "journey-broadband-118",
  "sessionId": "sess-77821",
  "metadata": {
    "responseId": "air-101",
    "aiTaskType": "cta_explanation",
    "promptTemplateVersion": "poc-cta-explainer-v1",
    "groundingAssetIds": [
      "bbd-cta-address-check",
      "bbd-guide-move-home"
    ],
    "accepted": true
  }
}
```

The demo should ideally show these events in Segment and a simple decision/funnel view in Mixpanel.

---

## Summary

The POC demo flow should prove that the platform can turn durable state, active-journey selection, deterministic ranking, bounded AI support, and explicit analytics into one explainable end-to-end experience.

That is what makes the POC feel like a credible first slice of the platform rather than a disconnected prototype.

---

| <- Previous | Next -> |
|---|---|
| [Ownership And Operating Model](./13-ownership-and-operating-model.md) | [POC Story And Presentation Narrative](./15-poc-story.md) |
