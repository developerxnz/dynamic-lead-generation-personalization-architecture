using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Produces the bounded journey view allowed into live decisioning and AI prompts.
/// </summary>
internal static class JourneySummaryBuilder
{
    public static JsonArray Build(JsonObject journeysPayload)
    {
        return new JsonArray(journeysPayload.RequireArrayProperty("journeys")
            .OfType<JsonObject>()
            .Select(BuildJourneySummary)
            .Cast<JsonNode>()
            .ToArray());
    }

    private static JsonObject BuildJourneySummary(JsonObject journey)
    {
        return new JsonObject
        {
            ["journey_id"] = journey.RequireProperty("journey_id").DeepClone(),
            ["service_category"] = journey.RequireProperty("service_category").DeepClone(),
            ["intent"] = journey.RequireProperty("intent").DeepClone(),
            ["stage"] = journey.RequireProperty("stage").DeepClone(),
            ["resume_candidate"] = journey.RequireProperty("resume_candidate").DeepClone(),
            ["qualification_state"] = journey.RequireObjectProperty("qualification_state").DeepCloneObject(),
            ["behavior_summary"] = journey.RequireObjectProperty("behavior_summary").DeepCloneObject(),
            ["journey_score"] = journey.RequireObjectProperty("decision_support")
                .RequireProperty("journey_score")
                .DeepClone(),
            ["last_meaningful_event_at"] = journey.RequireProperty("last_meaningful_event_at").DeepClone(),
            ["ai_journey_summary"] = journey.RequireObjectProperty("decision_support")
                .RequireProperty("ai_journey_summary")
                .DeepClone(),
        };
    }
}
