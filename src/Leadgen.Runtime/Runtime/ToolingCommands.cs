using System.Text.Json.Nodes;
using System.Diagnostics;

namespace Leadgen.Runtime;

/// <summary>
/// Implements the console tooling verbs for validation, dashboards, evaluation, and Cosmos state utilities.
/// </summary>
internal static class ToolingCommands
{
    public static async Task<int> RunAsync(string command, string[] args, RepositoryPaths paths)
    {
        var fixtures = new FixtureStore(paths);
        var runner = new ScenarioRunner(paths);

        return command switch
        {
            "validate" => await RunValidateAsync(args, fixtures, runner, dashboardMode: false),
            "dashboard" => await RunValidateAsync(args, fixtures, runner, dashboardMode: true),
            "evaluate" => await RunEvaluateAsync(args, fixtures, runner),
            "seed" => await RunSeedAsync(args, fixtures),
            "reset" => await RunResetAsync(args, fixtures),
            "inspect" => await RunInspectAsync(args, fixtures),
            _ => throw new ArgumentException($"Unknown command: {command}"),
        };
    }

    private static async Task<int> RunValidateAsync(
        string[] args,
        FixtureStore fixtures,
        ScenarioRunner runner,
        bool dashboardMode)
    {
        var options = ValidationOptions.Parse(args, fixtures.ListScenarios());
        var results = new List<ValidationScenarioResult>();

        foreach (var scenario in options.Scenarios)
        {
            ValidationScenarioResult result;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (options.Source == "cosmos" && ShouldClearBefore(options.CosmosClear))
                {
                    await ResetScenarioAsync(fixtures, scenario);
                }

                if (options.Source == "cosmos")
                {
                    await SeedScenarioAsync(fixtures, scenario);
                }

                var runResult = await runner.RunScenarioAsync(
                    new CliOptions(
                        scenario,
                        options.Source,
                        options.AiMode,
                        options.PromptSource,
                        false,
                        null),
                    printSummary: false,
                    writeOutputs: false);

                var mismatches = CompareOutputs(fixtures, scenario, runResult.Outputs, options.AiMode, options.PromptSource);
                result = new ValidationScenarioResult(
                    scenario,
                    mismatches.Count == 0 ? "PASS" : "FAIL",
                    options.Source,
                    options.AiMode,
                    stopwatch.Elapsed,
                    mismatches,
                    null);
            }
            catch (Exception ex)
            {
                result = new ValidationScenarioResult(
                    scenario,
                    "ERROR",
                    options.Source,
                    options.AiMode,
                    stopwatch.Elapsed,
                    Array.Empty<string>(),
                    ex.Message);
            }
            finally
            {
                if (options.Source == "cosmos" && ShouldClearAfter(options.CosmosClear))
                {
                    try
                    {
                        await ResetScenarioAsync(fixtures, scenario);
                    }
                    catch
                    {
                        // Preserve the primary validation result.
                    }
                }
            }

            results.Add(result);
            if (!dashboardMode)
            {
                Console.WriteLine(result.Status == "PASS"
                    ? $"PASS {result.Scenario}"
                    : result.Status == "FAIL"
                        ? $"FAIL {result.Scenario} mismatches: {string.Join(", ", result.Mismatches)}"
                        : $"ERROR {result.Scenario}: {result.Error}");
            }
        }

        if (dashboardMode)
        {
            var scenarioHeader = "Scenario";
            var statusHeader = "Status";
            var sourceHeader = "Source";
            var aiModeHeader = "AI mode";
            var durationHeader = "Duration";
            var detailsHeader = "Mismatch/Errors";
            var detailRows = results
                .Select(result => result.Error ?? (result.Mismatches.Count == 0
                    ? string.Empty
                    : string.Join(", ", result.Mismatches)))
                .ToArray();
            var scenarioWidth = Math.Max(
                scenarioHeader.Length,
                results.Max(static result => result.Scenario.Length));
            var statusWidth = Math.Max(
                statusHeader.Length,
                results.Max(static result => result.Status.Length));
            var sourceWidth = Math.Max(
                sourceHeader.Length,
                results.Max(static result => result.Source.Length));
            var aiModeWidth = Math.Max(
                aiModeHeader.Length,
                results.Max(static result => result.AiMode.Length));
            var durationRows = results
                .Select(static result => $"{result.Duration.TotalSeconds:F3}s")
                .ToArray();
            var durationWidth = Math.Max(
                durationHeader.Length,
                durationRows.Max(static value => value.Length));

            Console.WriteLine();
            Console.WriteLine(
                $"{scenarioHeader.PadRight(scenarioWidth)}  " +
                $"{statusHeader.PadRight(statusWidth)}  " +
                $"{sourceHeader.PadRight(sourceWidth)}  " +
                $"{aiModeHeader.PadRight(aiModeWidth)}  " +
                $"{durationHeader.PadRight(durationWidth)}  " +
                $"{detailsHeader}");
            Console.WriteLine(
                $"{new string('-', scenarioWidth)}  " +
                $"{new string('-', statusWidth)}  " +
                $"{new string('-', sourceWidth)}  " +
                $"{new string('-', aiModeWidth)}  " +
                $"{new string('-', durationWidth)}  " +
                $"{new string('-', detailsHeader.Length)}");
            for (var index = 0; index < results.Count; index++)
            {
                var result = results[index];
                Console.WriteLine(
                    $"{result.Scenario.PadRight(scenarioWidth)}  " +
                    $"{result.Status.PadRight(statusWidth)}  " +
                    $"{result.Source.PadRight(sourceWidth)}  " +
                    $"{result.AiMode.PadRight(aiModeWidth)}  " +
                    $"{durationRows[index].PadRight(durationWidth)}  " +
                    $"{detailRows[index]}");
            }
        }

        var passed = results.Count(static result => result.Status == "PASS");
        Console.WriteLine($"{passed}/{results.Count} scenarios passed");
        return results.All(static result => result.Status == "PASS") ? 0 : 1;
    }

    private static async Task<int> RunEvaluateAsync(string[] args, FixtureStore fixtures, ScenarioRunner runner)
    {
        var options = EvaluationOptions.Parse(args, fixtures.ListScenarios());
        var results = new List<EvaluationScenarioResult>();

        foreach (var scenario in options.Scenarios)
        {
            Console.WriteLine($"Running: {scenario}");
            try
            {
                var runResult = await runner.RunScenarioAsync(
                    new CliOptions(scenario, "fixtures", "ollama", options.PromptSource, false, null),
                    printSummary: false,
                    writeOutputs: false);

                var promptInput = runResult.Outputs["08-ai-prompt-input.json"];
                var aiOutput = runResult.Outputs["09-ai-output.json"];
                var response = aiOutput.RequireObjectProperty("response");
                var validation = PromptUtilities.ValidateResponse(response, promptInput);
                var expectedOutput = fixtures.LoadScenarioArtifact(scenario, "09-ai-expected-output.json");

                results.Add(new EvaluationScenarioResult(
                    scenario,
                    validation.AllPassed ? "PASS" : "FAIL",
                    expectedOutput.RequireObjectProperty("response").RequireStringProperty("summary"),
                    response.OptionalStringProperty("summary") ?? string.Empty,
                    validation.Checks,
                    null));
            }
            catch (Exception ex)
            {
                results.Add(new EvaluationScenarioResult(
                    scenario,
                    "ERROR",
                    string.Empty,
                    string.Empty,
                    new Dictionary<string, bool>(),
                    ex.Message));
            }
        }

        Console.WriteLine();
        Console.WriteLine("============================================================");
        Console.WriteLine("EVALUATION REPORT");
        Console.WriteLine("============================================================");
        foreach (var result in results)
        {
            var icon = result.Status == "PASS" ? "✓" : result.Status == "FAIL" ? "✗" : "!";
            Console.WriteLine();
            Console.WriteLine($"{icon} {result.Scenario}  [{result.Status}]");
            if (result.Error is not null)
            {
                Console.WriteLine($"  Error: {result.Error}");
                continue;
            }

            Console.WriteLine($"  Expected summary: {result.ExpectedSummary}");
            Console.WriteLine($"  Actual summary:   {result.ActualSummary}");
            Console.WriteLine();
            foreach (var check in result.ValidationChecks)
            {
                Console.WriteLine($"  {(check.Value ? "✓" : "✗")}  {check.Key}");
            }
        }

        var passed = results.Count(static result => result.Status == "PASS");
        Console.WriteLine();
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Result: {passed}/{results.Count} scenarios passed");
        Console.WriteLine("============================================================");
        Console.WriteLine();

        return results.All(static result => result.Status == "PASS") ? 0 : 1;
    }

    private static async Task<int> RunSeedAsync(string[] args, FixtureStore fixtures)
    {
        var scenario = ParseSingleScenario(args, fixtures.ListScenarios(), "seed");
        await SeedScenarioAsync(fixtures, scenario);
        Console.WriteLine($"Seeded scenario: {scenario}");
        return 0;
    }

    private static async Task<int> RunResetAsync(string[] args, FixtureStore fixtures)
    {
        var scenario = ParseSingleScenario(args, fixtures.ListScenarios(), "reset");
        await ResetScenarioAsync(fixtures, scenario);
        Console.WriteLine($"Reset scenario state: {scenario}");
        return 0;
    }

    private static async Task<int> RunInspectAsync(string[] args, FixtureStore fixtures)
    {
        var scenario = ParseSingleScenario(args, fixtures.ListScenarios(), "inspect");
        var inputs = fixtures.LoadScenarioInputs(scenario);
        var customerId = inputs.CustomerId;

        var config = CosmosConfig.FromEnvironment();
        await using var store = new CosmosRuntimeStore(config);
        var state = await store.InspectScenarioStateAsync(customerId);
        Console.WriteLine(state.ToJsonString());
        return 0;
    }

    private static IReadOnlyList<string> CompareOutputs(
        FixtureStore fixtures,
        string scenario,
        IReadOnlyDictionary<string, JsonObject> actualOutputs,
        string aiMode,
        string promptSource)
    {
        var expectedSelection = fixtures.LoadScenarioArtifact(scenario, "04-active-journey-selection.json");
        var contractMismatches = ValidateJourneyContracts(actualOutputs, expectedSelection);
        if (aiMode is "unavailable" or "invalid")
        {
            return contractMismatches
                .Concat(ValidateUnavailableAiFallback(actualOutputs, expectedSelection))
                .ToArray();
        }

        var artifactNames = promptSource == "fixture"
            ? new[] { "03", "04", "05", "06", "07", "08", "10", "11" }
            : new[] { "03", "04", "05", "06", "07", "10", "11" };

        var mismatches = new List<string>(contractMismatches);
        foreach (var artifact in artifactNames)
        {
            var expected = artifact == "03"
                ? ExpectedJourneyInterpretation(expectedSelection)
                : fixtures.LoadScenarioArtifact(scenario, ArtifactFileName(artifact));
            var actual = actualOutputs[ArtifactFileName(artifact)].DeepCloneObject();

            if (artifact == "03" && aiMode != "expected")
            {
                continue;
            }
            if (artifact == "04")
            {
                if (!SelectionMatches(expected, actual))
                {
                    mismatches.Add(artifact);
                }
                continue;
            }
            else if (artifact == "10")
            {
                expected = NormalizeFinalResponse(expected, aiMode);
                actual = NormalizeFinalResponse(actual, aiMode);
            }
            else if (artifact == "11")
            {
                expected = NormalizeEvents(expected, aiMode);
                actual = NormalizeEvents(actual, aiMode);
            }

            if (!JsonNodesEqual(expected, actual))
            {
                mismatches.Add(artifact);
            }
        }

        return mismatches;
    }

    private static JsonObject ExpectedJourneyInterpretation(JsonObject expectedSelection)
    {
        var expected = expectedSelection.RequireObjectProperty("ai_interpretation");
        return new JsonObject
        {
            ["status"] = "accepted",
            ["source"] = "fixture",
            ["model_version"] = "expected",
            ["latency_ms"] = 0,
            ["suggested_journey_id"] = expected.RequireProperty("suggested_journey_id").DeepClone(),
            ["confidence"] = expected.RequireProperty("confidence").DeepClone(),
            ["reason_summary"] = expected.RequireProperty("reason_summary").DeepClone(),
        };
    }

    private static bool SelectionMatches(JsonObject expected, JsonObject actual)
    {
        return expected.RequireStringProperty("selected_journey_id")
                == actual.RequireStringProperty("selected_journey_id")
            && expected.OptionalBoolProperty("deterministic_override")
                == actual.OptionalBoolProperty("deterministic_override")
            && expected.RequireObjectProperty("ai_interpretation").RequireStringProperty("suggested_journey_id")
                == actual.RequireObjectProperty("ai_interpretation").RequireStringProperty("suggested_journey_id");
    }

    private static IReadOnlyList<string> ValidateJourneyContracts(
        IReadOnlyDictionary<string, JsonObject> outputs,
        JsonObject expectedSelection)
    {
        var mismatches = new List<string>();
        var summaries = outputs["02-journey-summaries.json"].RequireArrayProperty("journeys");
        var summaryIds = summaries.OfType<JsonObject>()
            .Select(static summary => summary.RequireStringProperty("journey_id"))
            .ToHashSet(StringComparer.Ordinal);
        var expectedIds = expectedSelection.RequireArrayProperty("candidate_journeys")
            .OfType<JsonObject>()
            .Select(static candidate => candidate.RequireStringProperty("journey_id"))
            .ToHashSet(StringComparer.Ordinal);
        var allowedSummaryFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "journey_id", "service_category", "intent", "stage", "resume_candidate",
            "qualification_state", "behavior_summary", "journey_score",
            "last_meaningful_event_at", "ai_journey_summary",
        };

        if (!summaryIds.SetEquals(expectedIds)
            || summaries.OfType<JsonObject>().Any(summary =>
                !summary.Select(static property => property.Key)
                    .ToHashSet(StringComparer.Ordinal)
                    .SetEquals(allowedSummaryFields)))
        {
            mismatches.Add("journey-summary-contract");
        }

        var interpretation = outputs["03-ai-journey-interpretation.json"];
        var status = interpretation.RequireStringProperty("status");
        if (status == "accepted")
        {
            var suggestion = interpretation.RequireStringProperty("suggested_journey_id");
            var confidence = interpretation.RequireProperty("confidence").GetValue<double>();
            if (!summaryIds.Contains(suggestion)
                || confidence is < 0 or > 1
                || string.IsNullOrWhiteSpace(interpretation.RequireStringProperty("reason_summary")))
            {
                mismatches.Add("journey-interpretation-contract");
            }
        }
        else if (status != "unavailable" || string.IsNullOrWhiteSpace(interpretation.RequireStringProperty("fallback_reason")))
        {
            mismatches.Add("journey-interpretation-contract");
        }

        var selection = outputs["04-active-journey-selection.json"];
        var selectedId = selection.RequireStringProperty("selected_journey_id");
        if (!summaryIds.Contains(selectedId))
        {
            mismatches.Add("selection-contract");
        }
        else if (selection.OptionalBoolProperty("deterministic_override")
            && (status != "accepted"
                || selection.RequireObjectProperty("ai_interpretation").RequireStringProperty("suggested_journey_id") == selectedId))
        {
            mismatches.Add("selection-contract");
        }

        return mismatches;
    }

    private static IReadOnlyList<string> ValidateUnavailableAiFallback(
        IReadOnlyDictionary<string, JsonObject> outputs,
        JsonObject expectedSelection)
    {
        var mismatches = new List<string>();
        var interpretation = outputs["03-ai-journey-interpretation.json"];
        var selection = outputs["04-active-journey-selection.json"];
        var explanation = outputs["10-final-response.json"].RequireObjectProperty("explanation");
        if (interpretation.RequireStringProperty("status") != "unavailable"
            || interpretation.RequireStringProperty("fallback_reason") is not ("forced_unavailable" or "invalid_response")
            || selection.RequireStringProperty("selected_journey_id") != expectedSelection.RequireStringProperty("selected_journey_id")
            || explanation.RequireStringProperty("source") != "deterministic_fallback")
        {
            mismatches.Add("unavailable-ai-fallback");
        }

        return mismatches;
    }

    private static JsonObject NormalizeFinalResponse(JsonObject payload, string aiMode)
    {
        var normalized = payload.DeepCloneObject();
        if (aiMode == "ollama")
        {
            var explanation = normalized.RequireObjectProperty("explanation");
            explanation["ai_response_id"] = "__dynamic__";
            explanation["summary"] = "__dynamic__";
            explanation["cta_support_text"] = "__dynamic__";
            explanation["grounding_asset_ids"] = "__dynamic__";
        }

        return normalized;
    }

    private static JsonObject NormalizeEvents(JsonObject payload, string aiMode)
    {
        var normalized = payload.DeepCloneObject();
        if (aiMode == "ollama")
        {
            foreach (var eventNode in normalized.RequireArrayProperty("events").OfType<JsonObject>())
            {
                if (eventNode.RequireStringProperty("event_type") != "ai_response_accepted")
                {
                    continue;
                }

                var metadata = eventNode.RequireObjectProperty("metadata");
                metadata["response_id"] = "__dynamic__";
                metadata["latency_ms"] = "__dynamic__";
                metadata["grounding_asset_ids"] = "__dynamic__";
            }
        }

        return normalized;
    }

    private static string ArtifactFileName(string artifact) => artifact switch
    {
        "03" => "03-ai-journey-interpretation.json",
        "04" => "04-active-journey-selection.json",
        "05" => "05-candidate-retrieval.json",
        "06" => "06-ranking-request.json",
        "07" => "07-ranking-response.json",
        "08" => "08-ai-prompt-input.json",
        "10" => "10-final-response.json",
        "11" => "11-analytics-events.json",
        _ => throw new ArgumentException($"Unknown artifact: {artifact}"),
    };

    private static bool ShouldClearBefore(string cosmosClear) => cosmosClear is "before" or "both";

    private static bool ShouldClearAfter(string cosmosClear) => cosmosClear is "after" or "both";

    private static bool JsonNodesEqual(JsonNode expected, JsonNode actual)
    {
        return expected.ToJsonString() == actual.ToJsonString();
    }

    private static async Task SeedScenarioAsync(FixtureStore fixtures, string scenario)
    {
        var config = CosmosConfig.FromEnvironment();
        await using var store = new CosmosRuntimeStore(config);
        await store.SeedScenarioAsync(fixtures.LoadScenarioInputs(scenario));
    }

    private static async Task ResetScenarioAsync(FixtureStore fixtures, string scenario)
    {
        var config = CosmosConfig.FromEnvironment();
        var inputs = fixtures.LoadScenarioInputs(scenario);
        await using var store = new CosmosRuntimeStore(config);
        await store.ResetScenarioAsync(inputs.CustomerId);
    }

    private static string ParseSingleScenario(string[] args, IReadOnlyList<string> scenarios, string command)
    {
        if (args.Length != 1 || !scenarios.Contains(args[0], StringComparer.Ordinal))
        {
            throw new ArgumentException($"Usage: {command} <scenario>");
        }

        return args[0];
    }
}

/// <summary>
/// Captures the options shared by validate and dashboard runs.
/// </summary>
internal sealed record ValidationOptions(
    IReadOnlyList<string> Scenarios,
    string Source,
    string AiMode,
    string PromptSource,
    string CosmosClear)
{
    public static ValidationOptions Parse(string[] args, IReadOnlyList<string> availableScenarios)
    {
        var scenarios = new List<string>();
        var source = "fixtures";
        var aiMode = "expected";
        var promptSource = "fixture";
        var cosmosClear = "none";

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--source":
                    source = ReadValue(args, ref index, arg);
                    break;
                case "--ai-mode":
                    aiMode = ReadValue(args, ref index, arg);
                    break;
                case "--prompt-source":
                    promptSource = ReadValue(args, ref index, arg);
                    break;
                case "--cosmos-clear":
                    cosmosClear = ReadValue(args, ref index, arg);
                    break;
                default:
                    if (!availableScenarios.Contains(arg, StringComparer.Ordinal))
                    {
                        throw new ArgumentException($"Unknown scenario: {arg}");
                    }
                    scenarios.Add(arg);
                    break;
            }
        }

        if (aiMode is not ("expected" or "ollama" or "unavailable" or "invalid"))
        {
            throw new ArgumentException($"Unsupported --ai-mode value: {aiMode}");
        }

        return new ValidationOptions(
            scenarios.Count > 0 ? scenarios : availableScenarios,
            source,
            aiMode,
            promptSource,
            cosmosClear);
    }

    private static string ReadValue(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {flag}");
        }
        index += 1;
        return args[index];
    }
}

/// <summary>
/// Captures the options for Ollama evaluation runs.
/// </summary>
internal sealed record EvaluationOptions(
    IReadOnlyList<string> Scenarios,
    string PromptSource)
{
    public static EvaluationOptions Parse(string[] args, IReadOnlyList<string> availableScenarios)
    {
        var scenarios = new List<string>();
        var promptSource = "fixture";
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg == "--prompt-source")
            {
                if (index + 1 >= args.Length)
                {
                    throw new ArgumentException("Missing value for --prompt-source");
                }
                promptSource = args[++index];
                continue;
            }

            if (!availableScenarios.Contains(arg, StringComparer.Ordinal))
            {
                throw new ArgumentException($"Unknown scenario: {arg}");
            }
            scenarios.Add(arg);
        }

        return new EvaluationOptions(
            scenarios.Count > 0 ? scenarios : availableScenarios,
            promptSource);
    }
}

/// <summary>
/// Records the outcome of validating one scenario against the expected artifacts.
/// </summary>
internal sealed record ValidationScenarioResult(
    string Scenario,
    string Status,
    string Source,
    string AiMode,
    TimeSpan Duration,
    IReadOnlyList<string> Mismatches,
    string? Error);

/// <summary>
/// Records the outcome of evaluating one scenario's AI response quality checks.
/// </summary>
internal sealed record EvaluationScenarioResult(
    string Scenario,
    string Status,
    string ExpectedSummary,
    string ActualSummary,
    IReadOnlyDictionary<string, bool> ValidationChecks,
    string? Error);
