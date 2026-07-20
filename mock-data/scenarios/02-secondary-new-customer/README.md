# Scenario 02 Walkthrough: First-Time Health Insurance Visitor

This scenario shows a **new customer with no meaningful prior history** who arrives to research health insurance options.

The main job of the platform here is to avoid overcomplicating the experience. There is only one clear journey, so the platform should guide the customer into a comparison flow without pressure.

---

## In Plain English

The customer is a first-time visitor in VIC looking at health insurance options for a single household. Because there is no competing journey and no saved quote to resume, the platform should:

1. create or load one simple health discovery journey
2. select that journey without ambiguity
3. recommend comparison as the best next step
4. suppress family-oriented content that does not fit the customer
5. use AI to explain the comparison CTA in a friendly, low-pressure way

---

## Files In Order

| File | What goes in | What comes out | Why it matters |
|---|---|---|---|
| `01-customer-profile.json` | First-visit profile facts such as household type and region | A lightweight customer context record | Gives the platform enough context to avoid obviously poor recommendations |
| `02-journey-states.json` | One health insurance journey created from this session | A single active journey state | Shows the simplest decisioning path with no cross-journey conflict |
| `03-session-request.json` | Organic search entry, current URL, query text, and device context | Live session context | Tells the system what the visitor is trying to learn |
| `04-active-journey-selection.json` | Customer context plus the single journey | Health insurance selected as active journey | Confirms there is no ambiguity to resolve |
| `05-candidate-retrieval.json` | Health discovery journey + VIC web context | Broad health candidate set | Pulls in likely relevant options before ranking |
| `06-ranking-request.json` | Retrieved candidates plus decision context | Full scoring input | Packages the candidate set into rankable decision input |
| `07-ranking-response.json` | Ranking request | Ranked health recommendations plus one suppression | Shows why comparison is better than pushing a quote immediately |
| `08-ai-prompt-input.json` | Selected comparison action + grounded content snippets | AI explanation prompt package | Gives the model the facts needed to support the chosen CTA |
| `09-ai-expected-output.json` | Prompt package | Expected structured explanation | Defines how the explanation should look and behave |
| `10-final-response.json` | Ranked output plus AI explanation | Final channel payload | Represents the response the visitor would actually see |
| `11-analytics-events.json` | Decision output and response details | Telemetry events | Captures the journey creation and recommendation trace |

---

## Step-By-Step Breakdown

### Step 1: Load the customer profile

**Expected input:** `01-customer-profile.json`

**What it says in simple terms:** this is a first-time customer in VIC with a single-household profile.

**Expected output:** a basic customer context object.

**Why this step exists:** even a new customer has useful session-derived facts that should shape the experience.

### Step 2: Load or create the journey state

**Expected input:** `02-journey-states.json`

**What it says in simple terms:** the customer has one health insurance discovery journey and nothing else competing for attention.

**Expected output:** one active journey record.

**Why this step exists:** downstream steps need a structured journey object, even in a simple one-journey case.

### Step 3: Read the live session

**Expected input:** `03-session-request.json`

**What it says in simple terms:** the visitor came from organic search and is looking at health insurance options for singles.

**Expected output:** the current session signal package.

**Why this step exists:** this is the real-time evidence that the health discovery journey is the right one to drive the session.

### Step 4: Pick the active journey

**Expected input:** the single journey plus the current session

**Expected output:** `04-active-journey-selection.json`

**What should happen:** health insurance is selected immediately because it is the only journey.

**Why this step exists:** even when the answer is obvious, the system should still record how the active journey was chosen.

### Step 5: Retrieve broad candidates

**Expected input:** the active health journey and VIC web context

**Expected output:** `05-candidate-retrieval.json`

**What should happen:** the system retrieves:

- a comparison action
- a singles cover offer
- a switching guide
- a family guide that will later be suppressed

**Why this step exists:** retrieval should cast a reasonably wide net before ranking applies customer-fit logic.

### Step 6: Build the ranking request

**Expected input:** the retrieved candidates and customer context

**Expected output:** `06-ranking-request.json`

**What should happen:** the ranking engine gets structured input that includes household fit, stage, and intent.

**Why this step exists:** ranking works best when the scoring inputs are explicit and explainable.

### Step 7: Rank and suppress candidates

**Expected input:** `06-ranking-request.json`

**Expected output:** `07-ranking-response.json`

**What should happen:** `action-health-compare-001` ranks first, `offer-health-singles-001` ranks second, and the family guide is suppressed.

**Plain-language reason:** a brand-new visitor should compare options before being pushed directly into a quote, and family content is not appropriate for a single-household customer.

**Why this step exists:** this is where the platform turns a broad candidate set into a sensible, customer-fit recommendation.

### Step 8: Prepare the AI prompt

**Expected input:** the chosen comparison action and approved grounding snippets

**Expected output:** `08-ai-prompt-input.json`

**What should happen:** the AI sees only approved health-comparison and singles-cover facts.

**Why this step exists:** the explanation should feel helpful and human, but still stay within approved business content.

### Step 9: Validate the AI explanation target

**Expected input:** `08-ai-prompt-input.json`

**Expected output:** `09-ai-expected-output.json`

**What should happen:** the explanation should be welcoming, non-pushy, and grounded on the comparison and singles-cover assets.

**Why this step exists:** the team needs a concrete expected result to compare against live model output.

### Step 10: Assemble the final response

**Expected input:** ranked recommendation + accepted AI explanation

**Expected output:** `10-final-response.json`

**What should happen:** the customer receives:

- health insurance as the active journey
- the comparison CTA as the next best action
- a singles quote as supporting content
- no secondary journey prompt

**Why this step exists:** the delivery channel needs a clean response focused on one clear next step.

### Step 11: Emit analytics events

**Expected input:** the final response and decision trace

**Expected output:** `11-analytics-events.json`

**What should happen:** the system records that a new journey was created, the comparison CTA was served, and the AI explanation was accepted.

**Why this step exists:** the team needs to measure whether simple discovery experiences move first-time visitors toward qualified action.

---

## What Good Looks Like

This scenario is working as intended when:

1. the health journey is selected with no ambiguity
2. the comparison CTA ranks first
3. singles-specific support appears while family content is suppressed
4. the AI explanation remains welcoming and grounded
5. analytics clearly show that this was a new-journey, first-visit discovery flow
