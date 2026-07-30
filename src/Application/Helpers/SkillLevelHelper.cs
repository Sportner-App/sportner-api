using System.Text.Json;

namespace Sportner.Application.Helpers;

public static class SkillLevelHelper
{
    public static string? ResolveSkillLevel(string? skillLevelsJson, string sportType)
    {
        if (string.IsNullOrWhiteSpace(skillLevelsJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(skillLevelsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (document.RootElement.TryGetProperty(sportType, out var exact))
            {
                return exact.ValueKind == JsonValueKind.String
                    ? exact.GetString()
                    : exact.ToString();
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, sportType, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString()
                        : property.Value.ToString();
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    /// <summary>
    /// Accepts skill levels either as a JSON object ({"football":"advanced"})
    /// or as a JSON-encoded string, and normalizes it for the jsonb column.
    /// </summary>
    public static string? ToJsonbString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => string.IsNullOrWhiteSpace(element.GetString()) ? null : element.GetString(),
        _ => element.GetRawText()
    };

    public static JsonElement? ParseJsonbString(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Npgsql only accepts UTC DateTime for 'timestamp with time zone' columns.
    /// Date-only input such as "1999-03-28" arrives as Unspecified and is treated as UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
