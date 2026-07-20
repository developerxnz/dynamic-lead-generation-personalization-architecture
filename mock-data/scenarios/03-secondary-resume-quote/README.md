# Scenario 03 Walkthrough: Returning Customer Resuming A Saved Quote

This scenario shows a returning broadband customer who already started a quote and abandoned it part-way through.

The main job of the platform here is to **remove friction**. Instead of making the customer start over, the platform should recognize the saved quote and make resuming it the easiest next action.

---

## In Plain English

The customer comes back directly to the broadband section six days after leaving a quote unfinished. Serviceability is already confirmed and the quote is still saved. The platform should therefore:

1. recognize the returning customer
2. load the in-progress broadband journey
3. confirm that resume is the right path
4. rank the resume CTA clearly above comparison alternatives
5. use AI to reassure the customer that their progress is still there

---

## Files In Order

| File | What goes in | What comes out | Why it matters |
|---|---|---|---|
| `01-customer-profile.json` | Returning-customer facts such as household type and region | Durable customer context | Keeps the experience tied to a known customer rather than a blank slate |
| `02-journey-states.json` | One broadband journey with `resume_candidate` true | In-progress journey state | Shows that the customer already did meaningful work in a prior session |
| `03-session-request.json` | Direct return visit to the broadband section | Live session context | Confirms the customer is back to continue a broadband task |
| `04-active-journey-selection.json` | Stored journey + current session | Broadband resume journey selected | Confirms the right path is continuation, not rediscovery |
| `05-candidate-retrieval.json` | Quote-stage broadband journey + QLD web context | Resume-led candidate set | Retrieves the resume action and fallback alternatives |
| `06-ranking-request.json` | Retrieved candidates plus scoring context | Structured ranking input | Gives the ranking engine the evidence needed to apply resume bias safely |
| `07-ranking-response.json` | Ranking request | Ranked recommendations | Shows why resume should win by a large margin |
| `08-ai-prompt-input.json` | Selected resume action + approved snippets | AI explanation prompt package | Gives AI the facts needed to reassure the customer without inventing anything |
| `09-ai-expected-output.json` | Prompt package | Expected structured resume explanation | Defines the target explanation for validation |
| `10-final-response.json` | Ranked output plus AI explanation | Final customer-facing response | Represents the resumable experience the channel should render |
| `11-analytics-events.json` | Decision output and event payloads | Telemetry events | Proves that resume logic and prior eligibility state were used correctly |

---

## Step-By-Step Breakdown

### Step 1: Load the customer profile

**Expected input:** `01-customer-profile.json`

**What it says in simple terms:** this is a known returning customer in QLD with a couple-household profile and healthy purchase intent.

**Expected output:** a reusable customer context object.

**Why this step exists:** the platform should recognize returning customers and use what it already knows about them.

### Step 2: Load the in-progress journey

**Expected input:** `02-journey-states.json`

**What it says in simple terms:** the customer started a broadband quote six days ago, got 60% through it, and stopped before finishing.

**Expected output:** one broadband journey marked as `resume_candidate: true`.

**Why this step exists:** the system needs explicit state showing that there is something meaningful to resume.

### Step 3: Read the live session

**Expected input:** `03-session-request.json`

**What it says in simple terms:** the customer came back directly, without a campaign, to the broadband section.

**Expected output:** live session signals for the orchestrator.

**Why this step exists:** the current visit confirms that the customer is likely trying to continue what they started before.

### Step 4: Pick the active journey

**Expected input:** the stored broadband journey plus current session signals

**Expected output:** `04-active-journey-selection.json`

**What should happen:** the quote-in-progress broadband journey is selected immediately.

**Why this step exists:** the platform should formally confirm the session driver before building recommendations.

### Step 5: Retrieve quote-stage candidates

**Expected input:** the active journey and QLD web context

**Expected output:** `05-candidate-retrieval.json`

**What should happen:** the system retrieves:

- the resume quote action
- a broadband offer as a fallback
- a compare-plans action as a fallback

The moving-home guide is excluded because it belongs to an earlier funnel stage.

**Why this step exists:** retrieval should bring back the most relevant quote-stage options, not generic research content.

### Step 6: Build the ranking request

**Expected input:** candidate set plus resume-related context

**Expected output:** `06-ranking-request.json`

**What should happen:** the ranking engine receives structured evidence that the customer is far down the funnel and should not be sent backward unnecessarily.

**Why this step exists:** resume bias should be explicit and explainable, not hidden inside ad hoc logic.

### Step 7: Rank the candidates

**Expected input:** `06-ranking-request.json`

**Expected output:** `07-ranking-response.json`

**What should happen:** `action-bbd-resume-quote-001` ranks first by a clear margin.

**Plain-language reason:** the customer already did the hard part, so the best next step is to let them continue where they left off.

**Why this step exists:** ranking converts stored state and session signals into the lowest-friction next action.

### Step 8: Prepare the AI prompt

**Expected input:** the chosen resume action and approved grounding snippets

**Expected output:** `08-ai-prompt-input.json`

**What should happen:** the AI receives facts about the saved quote, what is restored on resume, and the fact that pricing or availability is not changed by resuming.

**Why this step exists:** the AI explanation should reassure the customer without making unsupported promises.

### Step 9: Validate the AI explanation target

**Expected input:** `08-ai-prompt-input.json`

**Expected output:** `09-ai-expected-output.json`

**What should happen:** the explanation should be calm, low-friction, and grounded on the resume asset.

**Why this step exists:** the POC needs a concrete standard for what a good resume explanation looks like.

### Step 10: Assemble the final response

**Expected input:** ranked resume recommendation + accepted AI explanation

**Expected output:** `10-final-response.json`

**What should happen:** the response contains:

- broadband as the active journey
- resume as the next best action
- one fallback broadband offer as supporting content
- no secondary journey prompt

**Why this step exists:** the channel should get one focused payload optimized for completion, not restart.

### Step 11: Emit analytics events

**Expected input:** the final response plus stored qualification context

**Expected output:** `11-analytics-events.json`

**What should happen:** analytics record the resume decision, accepted AI response, CTA impression, and the fact that serviceability was already confirmed from the prior session.

**Why this step exists:** the business needs to prove that resume logic reduces friction and preserves prior work.

---

## What Good Looks Like

This scenario is working as intended when:

1. the broadband quote-in-progress journey is selected immediately
2. the resume CTA ranks well above comparison fallbacks
3. no research-stage content distracts the customer
4. the AI explanation reassures the customer that their saved progress remains available
5. analytics show that resume bias and stored qualification state were both applied correctly
