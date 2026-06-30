using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static string MatchMode(string? id, string? name)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            return "id";
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return "name";
        }

        return "none";
    }
    private static bool HasProperty(JsonElement? element, string propertyName)
    {
        return element is not null && element.Value.ValueKind == JsonValueKind.Object && element.Value.TryGetProperty(propertyName, out _);
    }
    private static bool HasNull(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.Null;
    }
    private static JsonElement? GetObject(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return property;
    }
    private static JsonElement? GetArray(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return property;
    }
    private static string? ReadString(JsonElement? element, string propertyName)
    {
        return NormalizeString(ReadStringAllowNull(element, propertyName));
    }
    private static string? ReadStringAllowNull(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return property.ToString();
    }
    private static string? NormalizeString(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
    private static bool? ReadBool(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out bool value))
        {
            return value;
        }

        return null;
    }
    private static int? ReadInt(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        return null;
    }
    private static double? ReadDouble(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object || !element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out double number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return parsed;
        }

        return null;
    }
    private static DateTime? ReadDate(JsonElement? element, string propertyName)
    {
        string? value = ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!StartsWithCompleteIsoDate(value))
        {
            return null;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsed)
            ? parsed
            : null;
    }

    private static bool StartsWithCompleteIsoDate(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length >= 10
            && char.IsDigit(trimmed[0])
            && char.IsDigit(trimmed[1])
            && char.IsDigit(trimmed[2])
            && char.IsDigit(trimmed[3])
            && trimmed[4] == '-'
            && char.IsDigit(trimmed[5])
            && char.IsDigit(trimmed[6])
            && trimmed[7] == '-'
            && char.IsDigit(trimmed[8])
            && char.IsDigit(trimmed[9])
            && (trimmed.Length == 10 || trimmed[10] == 'T' || trimmed[10] == ' ');
    }
    private static T ReadEnum<T>(JsonElement? element, string propertyName, T fallback)
        where T : struct, Enum
    {
        T? value = ReadEnumNullable<T>(element, propertyName);
        return value ?? fallback;
    }
    private static T? ReadEnumNullable<T>(JsonElement? element, string propertyName)
        where T : struct, Enum
    {
        string? value = ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TryReadEnum(value, out T parsed) ? parsed : null;
    }
    private static T ReadEnumFromText<T>(string? value, T fallback)
        where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return TryReadEnum(value, out T parsed) ? parsed : fallback;
    }
    private static bool TryReadEnum<T>(string value, out T parsed)
        where T : struct, Enum
    {
        if (Enum.TryParse(value, true, out parsed))
        {
            return true;
        }

        string normalized = NormalizeEnumToken(value);
        foreach (string name in Enum.GetNames<T>())
        {
            if (string.Equals(NormalizeEnumToken(name), normalized, StringComparison.OrdinalIgnoreCase))
            {
                parsed = Enum.Parse<T>(name);
                return true;
            }
        }

        parsed = default;
        return false;
    }
    private static string NormalizeEnumToken(string value)
    {
        return value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
    }
}
