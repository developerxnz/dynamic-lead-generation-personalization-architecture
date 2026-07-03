# Mock Data

This folder contains structured mock data for the four POC scenarios defined in [`docs/delivery/12-poc-scope.md`](../docs/delivery/12-poc-scope.md).

The data is designed to be processed step-by-step through the platform's services and AI model to validate deterministic outputs.

---

## Folder Structure

```
mock-data/
  shared/
    activities-health-insurance.json   Normalized health insurance activity assets
    activities-broadband.json          Normalized broadband activity assets
    grounding-snippets.json            AI grounding snippets for both verticals
  scenarios/
    01-primary-returning-multi-journey/   Returning customer, two concurrent journeys
    02-secondary-new-customer/            First-time customer, single journey
    03-secondary-resume-quote/            Returning customer resuming a saved quote
    04-secondary-compliance-suppression/  State-restricted offer suppressed by compliance
```

---

## Scenarios

| Folder | Scenario | Customer | Verticals | Active journey |
|---|---|---|---|---|
| [`01-primary-returning-multi-journey`](./scenarios/01-primary-returning-multi-journey/README.md) | Returning customer with health + broadband journeys | `cust-20481` | health_insurance, broadband | broadband selected |
| [`02-secondary-new-customer`](./scenarios/02-secondary-new-customer/README.md) | First-time customer, single health insurance journey | `cust-99001` | health_insurance | health_insurance |
| [`03-secondary-resume-quote`](./scenarios/03-secondary-resume-quote/README.md) | Returning customer resuming a saved broadband quote | `cust-55312` | broadband | broadband |
| [`04-secondary-compliance-suppression`](./scenarios/04-secondary-compliance-suppression/README.md) | Returning customer in TAS with a state-restricted health bundle suppressed | `cust-66140` | health_insurance | health_insurance |

---

## Per-Scenario Files

Each scenario folder contains 11 numbered files representing the full end-to-end trace:

| File | Content | Role in the flow |
|---|---|---|
| `01-customer-profile.json` | Durable customer profile | Starting state loaded by the profile service |
| `02-journey-states.json` | One or more journey state entities | Journey history loaded alongside the profile |
| `03-session-request.json` | Incoming orchestrator request | Trigger for the decisioning flow |
| `04-active-journey-selection.json` | Active-journey selection result | Which journey should lead the session and why |
| `05-candidate-retrieval.json` | Retrieval query and candidate set | Broad candidates retrieved from activity metadata |
| `06-ranking-request.json` | Full ranking engine request | Input to the ranking engine |
| `07-ranking-response.json` | Ranking engine response | Ranked and suppressed candidates with reasons |
| `08-ai-prompt-input.json` | Assembled AI prompt context package | Input to the AI model |
| `09-ai-expected-output.json` | Expected AI model response | Deterministic target output for validation |
| `10-final-response.json` | Final orchestrated experience response | What the channel receives |
| `11-analytics-events.json` | Expected telemetry events | Events emitted during the session flow |

---

## Scenario Flow

```mermaid
flowchart TD
    A["01-customer-profile.json<br/>Customer profile"] --> D["04-active-journey-selection.json<br/>Active journey chosen"]
    B["02-journey-states.json<br/>Journey states"] --> D
    C["03-session-request.json<br/>Session trigger"] --> D
    D --> E["05-candidate-retrieval.json<br/>Broad candidate set"]
    E --> F["06-ranking-request.json<br/>Ranking engine input"]
    F --> G["07-ranking-response.json<br/>Ranked recommendations"]
    D --> H["08-ai-prompt-input.json<br/>AI grounding package"]
    G --> H
    H --> I["09-ai-expected-output.json<br/>Expected AI explanation"]
    G --> J["10-final-response.json<br/>Channel response"]
    I --> J
    J --> K["11-analytics-events.json<br/>Telemetry emitted"]
```

This mirrors the intended runtime flow: customer and journey state are loaded first, the active journey is selected, candidates are retrieved and ranked, AI produces a grounded explanation, the orchestrator assembles the final response, and analytics capture the resulting decision trace.

---

## Shared Assets

### `shared/activities-health-insurance.json`

Normalized health insurance activity assets conforming to the activity metadata model in [`docs/services/05-activity-metadata.md`](../docs/services/05-activity-metadata.md).

Includes:
- `offer-health-family-001` — family cover offer (Provider H)
- `offer-health-singles-001` — singles cover offer (Provider H)
- `offer-health-hospital-extras-bundle-001` — combined bundle (Provider K, NSW/VIC/QLD only)
- `action-health-compare-001` — comparison tool CTA
- `action-health-resume-compare-001` — resume comparison CTA
- `guide-health-switching-001` — switching guide
- `guide-health-families-001` — families audience guide

### `shared/activities-broadband.json`

Normalized broadband activity assets.

Includes:
- `offer-bbd-fast-family-001` — fast family nbn100 plan (Provider B)
- `offer-bbd-fibre-premium-001` — premium fibre nbn1000 plan (Provider A, NSW/VIC/QLD only)
- `action-bbd-address-check-001` — address serviceability check CTA
- `action-bbd-compare-plans-001` — plan comparison CTA
- `action-bbd-resume-quote-001` — resume saved quote CTA
- `guide-bbd-moving-home-001` — moving home broadband guide

### `shared/grounding-snippets.json`

AI grounding snippets (`GroundingSnippet` asset type) for both verticals. Each snippet is a bounded, approved piece of content that the AI model uses to ground its responses.

---

## How To Use

### Step-by-step processing

Each scenario is designed to be processed in file order:

1. Load `01-customer-profile.json` and `02-journey-states.json` into the profile service
2. Send `03-session-request.json` to the orchestrator
3. Validate the orchestrator produces output matching `04-active-journey-selection.json`
4. Validate the retrieval layer returns candidates matching `05-candidate-retrieval.json`
5. Send `06-ranking-request.json` to the ranking engine
6. Validate the ranking engine response matches `07-ranking-response.json`
7. Send `08-ai-prompt-input.json` to the AI model
8. Validate the AI response matches `09-ai-expected-output.json`
9. Validate the final orchestrated response matches `10-final-response.json`
10. Validate the telemetry events match `11-analytics-events.json`

### Validating AI outputs

`09-ai-expected-output.json` defines the target AI response for each scenario. It includes:

- `response` — the expected structured output matching the response contract
- `validation` — field-level validation checks (required fields, grounding coverage, claim safety, length bounds)
- `response_status` — `accepted` or `rejected`

When processing against a live AI model, compare the actual output to the expected response to evaluate:
- whether required fields are present
- whether grounding asset IDs are cited
- whether no unsupported claims are introduced
- whether summary and CTA text stay within configured length bounds

---

## Data Conventions

- IDs use kebab-case with a semantic prefix: `cust-`, `journey-`, `sess-`, `offer-`, `action-`, `guide-`, `gs-`, `air-`
- Timestamps use ISO 8601 format with UTC offset
- `metadata_revision` fields use the format `{assetId}@{version}` for traceability
- `ranking_policy_version` uses the format `{vertical}-v{n}`
- All service categories use snake_case: `health_insurance`, `broadband`
- All CTA types use snake_case: `get_quote`, `compare`, `check_eligibility`, `resume`, `read_guide`
