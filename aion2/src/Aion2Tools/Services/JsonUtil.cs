using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aion2Tools.Services;

/// <summary>The one place JSON is read and written, so every file shares the same options.</summary>
public static class JsonUtil
{
    private static readonly JsonSerializerOptions OPTIONS = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
        IgnoreReadOnlyProperties = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, OPTIONS);
    }

    /// <summary>Null for malformed JSON as well as a literal null.</summary>
    public static T? DeserializeOrNull<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, OPTIONS);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
