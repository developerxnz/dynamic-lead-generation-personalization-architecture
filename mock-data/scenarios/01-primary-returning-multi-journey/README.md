# Scenario 01 Walkthrough: Returning Customer With Two Journeys

This scenario shows a returning customer who has **two active interests at the same time**:

- a health insurance comparison journey already in progress
- a newer broadband moving-home journey

The main job of the platform here is to decide **which journey should lead this session right now** and then choose the best next action for that journey.

---

## In Plain English

The customer comes back through a broadband moving-home search. Even though they also have a health insurance journey on file, the current visit is clearly about getting internet sorted for a new house. The platform should therefore:

1. recognize the customer
2. load both journeys
3. choose broadband as the active journey
4. recommend an address check before plan comparison
5. use AI to explain that recommendation in simple language
6. keep the health journey visible only as a secondary prompt

---

## Files In Order

| File | What goes in | What comes out | Why it matters |
|---|---|---|---|
| `01-customer-profile.json` | Known-customer facts like household, location, and lead score | A reusable customer context record | Gives the platform stable customer facts before it reacts to this session |
| `02-journey-states.json` | Two existing journeys: health compare and broadband moving home | Journey-level intent, stage, urgency, and qualification state | Shows that one customer can have multiple concurrent journeys |
| `03-session-request.json` | Current visit signals such as URL, search theme, region, and query text | The live session context | Tells the platform what the customer appears to want right now |
| `04-active-journey-selection.json` | Profile + journeys + session signals | Broadband selected as the active journey | Prevents the system from mixing health and broadband into one confused experience |
| `05-candidate-retrieval.json` | Active journey plus a limited secondary-journey allowance | A broad candidate set led by broadband | Pulls in possible actions before ranking decides which one should win |
| `06-ranking-request.json` | Ranked-input package built from the retrieved candidates | Full scoring input for the ranking engine | Moves from "what could we show?" to "what should we show first?" |
| `07-ranking-response.json` | Ranking request | Ranked broadband recommendations plus one suppressed health candidate | Explains why the address check is better than plan comparison at this moment |
| `08-ai-prompt-input.json` | Selected action, customer context, journey context, and grounded snippets | A safe AI prompt package | Gives AI only the approved facts needed to explain the recommendation |
| `09-ai-expected-output.json` | The grounded prompt package | Expected structured AI explanation | Defines the target output shape and content constraints |
| `10-final-response.json` | Ranked recommendation plus accepted AI explanation | Final web response | Represents what the channel actually receives |
| `11-analytics-events.json` | Decision and response details | Telemetry events | Makes the entire decision trace measurable and auditable |

---

## Step-By-Step Breakdown

### Step 1: Load the customer profile

**Expected input:** `01-customer-profile.json`

**What it says in simple terms:** this is a known returning customer in NSW with a family household and a strong lead score.

**Expected output:** a customer context object that downstream services can reuse.

**Why this step exists:** the platform should not treat every visit like a brand-new visitor when it already knows useful facts about the customer.

### Step 2: Load all current journeys

**Expected input:** `02-journey-states.json`

**What it says in simple terms:** the customer has two live threads:

- an older health insurance comparison journey
- a newer broadband moving-home journey

**Expected output:** a list of journey states with scores, stages, and recent behavior.

**Why this step exists:** personalization has to work across multiple possible intents, not assume there is only one path forever.

### Step 3: Read the live session

**Expected input:** `03-session-request.json`

**What it says in simple terms:** the customer arrived from a paid broadband moving-home search and is currently on the moving-home broadband page.

**Expected output:** a session signal package that can be compared against the loaded journeys.

**Why this step exists:** past behavior matters, but the current visit is often the strongest clue about what should happen now.

### Step 4: Pick the active journey

**Expected input:** profile + journeys + live session

**Expected output:** `04-active-journey-selection.json`

**What should happen:** broadband wins because it is more recent and aligns strongly with the current visit, while health remains valid but secondary.

**Why this step exists:** the system needs one clear primary context for the live experience, even when several journeys exist.

### Step 5: Retrieve a broad candidate set

**Expected input:** the selected broadband journey and current context

**Expected output:** `05-candidate-retrieval.json`

**What should happen:** broadband candidates are retrieved first, including:

- address check action
- family broadband offer
- moving-home guide

One health resume action is also retained as limited secondary support.

**Why this step exists:** retrieval should gather plausible options first instead of hard-coding the final decision too early.

### Step 6: Build the ranking request

**Expected input:** retrieved candidates plus customer and journey context

**Expected output:** `06-ranking-request.json`

**What should happen:** the ranking engine receives all the information it needs to score each candidate safely and consistently.

**Why this step exists:** ranking should be driven by structured decision inputs, not by free-form interpretation at the last moment.

### Step 7: Rank the candidates

**Expected input:** `06-ranking-request.json`

**Expected output:** `07-ranking-response.json`

**What should happen:** the top result is `action-bbd-address-check-001`.

**Plain-language reason:** before showing broadband plans, the system needs to confirm what is actually available at the new address.

**Why this step exists:** a high-converting experience still has to respect real-world constraints like serviceability.

### Step 8: Prepare the AI prompt

**Expected input:** chosen action + grounded snippets

**Expected output:** `08-ai-prompt-input.json`

**What should happen:** the AI receives only approved broadband facts, including:

- why address-based serviceability matters
- why moving-home timing matters

**Why this step exists:** AI should help explain the recommendation, not invent the decision or introduce unsupported claims.

### Step 9: Validate the AI explanation target

**Expected input:** `08-ai-prompt-input.json`

**Expected output:** `09-ai-expected-output.json`

**What should happen:** the AI explanation supports the address-check CTA, cites valid grounding assets, and stays within the response contract.

**Why this step exists:** the POC needs a concrete acceptance target, not just a vague "sounds good" response.

### Step 10: Assemble the final response

**Expected input:** ranked recommendation + accepted AI explanation

**Expected output:** `10-final-response.json`

**What should happen:** the web response contains:

- broadband as the active journey
- address check as the next best action
- a family broadband offer as supporting content
- the health journey as a secondary prompt only

**Why this step exists:** the channel needs one clean, customer-facing payload rather than raw internal decision data.

### Step 11: Emit analytics events

**Expected input:** the final decision and rendered response

**Expected output:** `11-analytics-events.json`

**What should happen:** the platform logs journey selection, recommendation served, CTA impression, AI response acceptance, and eventual CTA click.

**Why this step exists:** without telemetry, the team cannot prove whether the decisioning logic is helping qualified conversion.

---

## What Good Looks Like

This scenario is working as intended when:

1. broadband is selected as the active journey
2. the address-check CTA ranks above plan comparison
3. health remains visible only as a secondary prompt
4. the AI explanation stays grounded on the approved broadband assets
5. analytics clearly record both the decision path and the customer-facing outcome
