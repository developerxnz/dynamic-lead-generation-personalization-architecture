using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Builds prompt text and validates AI responses against the checked-in response contract.
/// </summary>
internal static class PromptUtilities
{
    public static string AssembleUserMessage(JsonObject promptInput)
    {
        var parts = new[]
        {
            promptInput.RequireStringProperty("task_prompt"),
            "",
            "## Journey context",
            Serialize(promptInput.RequireObjectProperty("journey_context")),
            "",
            "## Customer context",
            Serialize(promptInput.RequireObjectProperty("customer_context")),
            "",
            "## Selected action",
            Serialize(promptInput.RequireObjectProperty("selected_action")),
            "",
            "## Grounding context",
            FormatGroundingContext(promptInput),
            "",
            "## Response contract",
            "You must return a JSON object with exactly these fields:",
            Serialize(promptInput.RequireObjectProperty("response_contract").RequireArrayProperty("required_fields")),
            $"- summary: max {promptInput.RequireObjectProperty("response_contract").RequireProperty("summary_max_words").GetValue<int>()} words",
            $"- key_points: max {promptInput.RequireObjectProperty("response_contract").RequireProperty("key_points_max_count").GetValue<int>()} items",
            $"- cta_support_text: max {promptInput.RequireObjectProperty("response_contract").RequireProperty("cta_support_text_max_words").GetValue<int>()} words",
            "- grounding_asset_ids: list of asset IDs from the grounding context you used",
            "",
            "Return only the JSON object. No explanation or prose outside the JSON.",
        };

        return string.Join(Environment.NewLine, parts);
    }

    public static JsonObject NormalizeGroundingAssetIds(JsonObject responseObject, JsonObject promptInput)
    {
        var normalized = responseObject.DeepCloneObject();
        if (normalized["grounding_asset_ids"] is not JsonArray groundingIds)
        {
            return normalized;
        }

        var snippetToAsset = promptInput
            .RequireArrayProperty("grounding_context")
            .OfType<JsonObject>()
            .ToDictionary(
                static item => item.RequireStringProperty("snippet_id"),
                static item => item.RequireStringProperty("asset_id"),
                StringComparer.Ordinal);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var normalizedIds = new JsonArray();
        foreach (var groundingNode in groundingIds)
        {
            if (groundingNode is null)
            {
                continue;
            }

            var groundingId = groundingNode.GetValue<string>();
            var canonicalId = snippetToAsset.GetValueOrDefault(groundingId, groundingId);
            if (seen.Add(canonicalId))
            {
                normalizedIds.Add(canonicalId);
            }
        }

        normalized["grounding_asset_ids"] = normalizedIds;
        return normalized;
    }

    public static ValidationResult ValidateResponse(JsonObject responseObject, JsonObject promptInput)
    {
        var contract = promptInput.RequireObjectProperty("response_contract");
        var requiredFields = contract.RequireArrayProperty("required_fields")
            .Select(static field => field?.GetValue<string>() ?? string.Empty)
            .Where(static field => field.Length > 0)
            .ToArray();

        var checks = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var field in requiredFields)
        {
            checks[$"{field}_present"] = responseObject[field] is not null;
        }

        var summary = responseObject.OptionalStringProperty("summary") ?? string.Empty;
        var keyPointsCount = responseObject["key_points"] is JsonArray keyPoints ? keyPoints.Count : 0;
        var ctaText = responseObject.OptionalStringProperty("cta_support_text") ?? string.Empty;
        var groundingIds = responseObject["grounding_asset_ids"] as JsonArray;
        var groundingContextIds = promptInput.RequireArrayProperty("grounding_context")
            .OfType<JsonObject>()
            .Select(static item => item.RequireStringProperty("asset_id"))
            .ToHashSet(StringComparer.Ordinal);

        checks["summary_within_length"] = CountWords(summary) <= contract.RequireProperty("summary_max_words").GetValue<int>();
        checks["key_points_within_count"] = keyPointsCount <= contract.RequireProperty("key_points_max_count").GetValue<int>();
        checks["cta_text_within_length"] = CountWords(ctaText) <= contract.RequireProperty("cta_support_text_max_words").GetValue<int>();
        checks["grounding_assets_cited"] = groundingIds is { Count: > 0 };
        checks["grounding_assets_valid"] = groundingIds is not null && groundingIds.All(id => id is not null && groundingContextIds.Contains(id.GetValue<string>()));

        return new ValidationResult(checks);
    }

    public static int CountWords(string value)
    {
        return value.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string FormatGroundingContext(JsonObject promptInput)
    {
        var builder = new StringBuilder()
            .AppendLine("Use only the following grounded assets.")
            .AppendLine("If you cite grounding_asset_ids, copy the asset_id values exactly as written.")
            .AppendLine("Do not return snippet_id values.")
            .AppendLine();

        foreach (var item in promptInput.RequireArrayProperty("grounding_context").OfType<JsonObject>())
        {
            builder.AppendLine($"- asset_id: {item.RequireStringProperty("asset_id")}");
            builder.AppendLine($"  snippet_id: {item.RequireStringProperty("snippet_id")}");
            builder.AppendLine($"  content: {item.RequireStringProperty("content")}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string Serialize(JsonNode node)
    {
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}

/// <summary>
/// Captures per-check validation results for one AI response.
/// </summary>
internal sealed class ValidationResult
{
    public ValidationResult(IReadOnlyDictionary<string, bool> checks)
    {
        Checks = checks;
    }

    public IReadOnlyDictionary<string, bool> Checks { get; }

    public bool AllPassed => Checks.Values.All(static value => value);
}
