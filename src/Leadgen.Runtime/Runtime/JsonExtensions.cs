using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leadgen.Runtime;

/// <summary>
/// Provides the shared JSON parsing, validation, cloning, and file-writing helpers used across the runtime.
/// </summary>
internal static class JsonExtensions
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static JsonObject RequireObject(this JsonNode? node, string name)
    {
        return node as JsonObject ?? throw new InvalidDataException($"Expected object: {name}");
    }

    public static JsonArray RequireArray(this JsonNode? node, string name)
    {
        return node as JsonArray ?? throw new InvalidDataException($"Expected array: {name}");
    }

    public static JsonNode RequireProperty(this JsonObject node, string name)
    {
        return node[name] ?? throw new InvalidDataException($"Missing property: {name}");
    }

    public static JsonObject RequireObjectProperty(this JsonObject node, string name)
    {
        return node.RequireProperty(name).RequireObject(name);
    }

    public static JsonObject RequireCustomerAttributes(this JsonObject customerProfile)
    {
        if (customerProfile["attributes"] is JsonObject attributes)
        {
            return attributes;
        }

        if (customerProfile["Attributes"] is JsonObject legacyAttributes)
        {
            return legacyAttributes;
        }

        if (customerProfile["profile"] is JsonObject legacyProfile)
        {
            return legacyProfile;
        }

        throw new InvalidDataException("Missing customer attributes object: attributes");
    }

    public static JsonObject RequireAttributes(this ScenarioInputs inputs)
    {
        if (inputs.Profile is not null)
        {
            return inputs.Profile.RequireCustomerAttributes();
        }

        if (inputs.Session["attributes"] is JsonObject attributes)
        {
            return attributes;
        }

        throw new InvalidDataException("Missing attributes in customer profile or session request.");
    }

    public static JsonObject RequireCustomerSummary(this ScenarioInputs inputs)
    {
        if (inputs.Profile?["customer_summary"] is JsonObject summary)
        {
            return summary;
        }

        if (inputs.Session["customer_summary"] is JsonObject sessionSummary)
        {
            return sessionSummary;
        }

        throw new InvalidDataException("Missing customer_summary in customer profile or session request.");
    }

    public static JsonArray RequireArrayProperty(this JsonObject node, string name)
    {
        return node.RequireProperty(name).RequireArray(name);
    }

    public static string RequireStringProperty(this JsonObject node, string name)
    {
        return node.RequireProperty(name).GetValue<string>();
    }

    public static string? OptionalStringProperty(this JsonObject node, string name)
    {
        return node[name]?.GetValue<string>();
    }

    public static bool OptionalBoolProperty(this JsonObject node, string name)
    {
        return node[name]?.GetValue<bool>() ?? false;
    }

    public static JsonObject DeepCloneObject(this JsonObject node)
    {
        return node.DeepClone().RequireObject("clone");
    }

    public static JsonArray DeepCloneArray(this JsonArray node)
    {
        return node.DeepClone().RequireArray("clone");
    }

    public static JsonObject LoadJsonObject(string path)
    {
        var node = JsonNode.Parse(File.ReadAllText(path));
        return node.RequireObject(path);
    }

    public static void WriteIndentedJson(string path, JsonNode payload)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
        payload.WriteTo(writer);
        writer.Flush();
        stream.WriteByte((byte)'\n');
    }
}
