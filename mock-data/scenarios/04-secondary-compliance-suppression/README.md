# Scenario 04 Walkthrough: Compliance Suppression For A State-Restricted Offer

This scenario shows a returning health insurance customer in **Tasmania** where one attractive bundle candidate must be **suppressed for compliance reasons** before the experience is assembled.

The main job of the platform here is to prove that deterministic controls stay in charge even when a product looks commercially strong.

---

## In Plain English

The customer is actively comparing health insurance and appears open to switching providers. A hospital-and-extras bundle from Provider K looks relevant on intent and household fit, but it is only approved for NSW, VIC, and QLD. The platform should therefore:

1. recognize the returning customer
2. load the active health comparison journey
3. retrieve a broad compare-stage candidate set
4. suppress the state-restricted bundle deterministically
5. recommend the health comparison CTA instead
6. use AI to explain the comparison CTA without mentioning the suppressed product

---

## Files In Order

| File | What goes in | What comes out | Why it matters |
|---|---|---|---|
| `01-customer-profile.json` | Known-customer facts such as household and location | Reusable customer context | Provides the stable customer facts used by downstream decisions |
| `02-journey-states.json` | One active health comparison journey | Compare-stage journey state | Shows the customer is already evaluating options rather than discovering the category |
| `03-session-request.json` | Current compare-page visit in TAS | Live session context | Confirms this session is about active health comparison |
| `04-active-journey-selection.json` | Customer context plus the single journey | Health insurance selected as active journey | Records the active-session driver even though there is no journey ambiguity |
| `05-candidate-retrieval.json` | Compare-stage health journey + TAS web context | Broad compare-stage candidate set | Demonstrates broad retrieval before compliance suppression is applied |
| `06-ranking-request.json` | Candidate set plus decision context and compliance flags | Structured ranking input | Makes the compliance guardrail explicit rather than hidden |
| `07-ranking-response.json` | Ranking request | Ranked recommendations plus one compliance suppression | Proves that a seemingly relevant bundle can still be blocked deterministically |
| `08-ai-prompt-input.json` | Selected comparison action + approved snippets | AI explanation prompt package | Ensures the model explains the chosen CTA, not the suppressed offer |
| `09-ai-expected-output.json` | Prompt package | Expected structured explanation | Defines the acceptance target for the model response |
| `10-final-response.json` | Ranked output plus AI explanation | Final channel payload | Represents the compliant experience the customer would actually see |
| `11-analytics-events.json` | Decision output and suppression trace | Telemetry events | Captures both the recommendation and the compliance guardrail application |

---

## Step-By-Step Breakdown

### Step 1: Load the customer profile

**Expected input:** `01-customer-profile.json`

**What it says in simple terms:** this is a known customer in TAS with a couple-household profile and active switching intent.

**Expected output:** a reusable customer context object.

**Why this step exists:** deterministic decisions need stable customer facts such as household and region before candidate evaluation starts.

### Step 2: Load the current journey state

**Expected input:** `02-journey-states.json`

**What it says in simple terms:** the customer is already comparing health cover options and has been looking at switching-related content.

**Expected output:** one active compare-stage journey.

**Why this step exists:** the platform needs explicit journey state to decide whether to compare, quote, resume, or educate.

### Step 3: Read the live session

**Expected input:** `03-session-request.json`

**What it says in simple terms:** the customer is currently on the health comparison page in Tasmania.

**Expected output:** the live session signal package.

**Why this step exists:** the current session confirms that comparison is still the right frame for the visit.

### Step 4: Pick the active journey

**Expected input:** the single health journey plus the current session

**Expected output:** `04-active-journey-selection.json`

**What should happen:** health insurance is selected immediately because it is the only journey.

**Why this step exists:** even in simple cases, the system should record which journey is driving the session.

### Step 5: Retrieve broad candidates

**Expected input:** the active health journey and TAS web context

**Expected output:** `05-candidate-retrieval.json`

**What should happen:** the system retrieves:

- the comparison action
- the Provider K bundle offer
- the switching guide

**Why this step exists:** retrieval should first gather plausible options rather than hard-coding the final answer too early.

### Step 6: Build the ranking request

**Expected input:** the retrieved candidates and customer context

**Expected output:** `06-ranking-request.json`

**What should happen:** the ranking engine receives the compliance flags needed to enforce state restrictions explicitly.

**Why this step exists:** compliance guardrails should be visible, deterministic, and auditable.

### Step 7: Rank and suppress candidates

**Expected input:** `06-ranking-request.json`

**Expected output:** `07-ranking-response.json`

**What should happen:** `action-health-compare-001` ranks first, `guide-health-switching-001` ranks second, and `offer-health-hospital-extras-bundle-001` is suppressed.

**Plain-language reason:** the bundle looks relevant, but it cannot be promoted in Tasmania because it is state-restricted to NSW, VIC, and QLD.

**Why this step exists:** this is the clearest proof that deterministic compliance controls remain authoritative.

### Step 8: Prepare the AI prompt

**Expected input:** the chosen comparison action and approved grounding snippets

**Expected output:** `08-ai-prompt-input.json`

**What should happen:** the AI sees only approved comparison and switching content.

**Why this step exists:** the explanation should help the customer move forward without surfacing suppressed or unavailable products.

### Step 9: Validate the AI explanation target

**Expected input:** `08-ai-prompt-input.json`

**Expected output:** `09-ai-expected-output.json`

**What should happen:** the explanation should support comparison, stay grounded, and avoid unsupported product claims.

**Why this step exists:** the POC still needs a deterministic target for AI-assisted copy.

### Step 10: Assemble the final response

**Expected input:** ranked recommendation + accepted AI explanation

**Expected output:** `10-final-response.json`

**What should happen:** the customer receives:

- health insurance as the active journey
- the comparison CTA as the next best action
- the switching guide as supporting content
- no suppressed bundle in the final experience

**Why this step exists:** the channel needs the final payload to reflect the compliant decision, not just the raw candidate set.

### Step 11: Emit analytics events

**Expected input:** the final response and suppression trace

**Expected output:** `11-analytics-events.json`

**What should happen:** the system records that the journey was selected, the bundle was suppressed for compliance, the comparison CTA was served, and the AI explanation was accepted.

**Why this step exists:** the business needs to prove not only what was shown, but also what was intentionally withheld and why.

---

## What Good Looks Like

This scenario is working as intended when:

1. the health comparison journey is selected with no ambiguity
2. the comparison CTA becomes the primary action
3. the Provider K bundle is suppressed for a deterministic compliance reason
4. the AI explanation stays grounded on approved compare and switching assets
5. analytics clearly record the suppression event as well as the final recommendation
