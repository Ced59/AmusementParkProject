using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Commands;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;

public sealed partial class ApplyLocalizedContentJsonCommandHandler
{
    private static void ApplyString(JsonElement value, Action<string?> setter, List<string> updatedFields, string fieldName)
    {
        setter(NormalizeOptionalText(ReadString(value)));
        updatedFields.Add(fieldName);
    }
    private static void ApplyBool(JsonElement value, Action<bool> setter, List<string> updatedFields, string fieldName)
    {
        bool? parsed = ReadBoolean(value);
        if (parsed.HasValue)
        {
            setter(parsed.Value);
            updatedFields.Add(fieldName);
        }
    }
    private static void ApplyInt(JsonElement value, Action<int?> setter, List<string> updatedFields, string fieldName)
    {
        setter(ReadInt32(value));
        updatedFields.Add(fieldName);
    }
    private static void ApplyDouble(JsonElement value, Action<double?> setter, List<string> updatedFields, string fieldName)
    {
        setter(ReadDouble(value));
        updatedFields.Add(fieldName);
    }
    private static void ApplyDate(JsonElement value, Action<DateTime?> setter, List<string> updatedFields, string fieldName)
    {
        setter(ReadDateTime(value));
        updatedFields.Add(fieldName);
    }
    private static void ApplyEnum<TEnum>(JsonElement value, Action<TEnum> setter, List<string> updatedFields, string fieldName)
        where TEnum : struct
    {
        TEnum? parsed = ReadEnum<TEnum>(value);
        if (parsed.HasValue)
        {
            setter(parsed.Value);
            updatedFields.Add(fieldName);
        }
    }
    private static TEnum? ReadEnum<TEnum>(JsonElement value)
        where TEnum : struct
    {
        string? text = ReadString(value);
        return !string.IsNullOrWhiteSpace(text) && Enum.TryParse(text, true, out TEnum parsed) ? parsed : null;
    }
    private static DateTime? ReadDateTime(JsonElement value)
    {
        string? text = ReadString(value);
        return !string.IsNullOrWhiteSpace(text) && DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime parsed)
            ? parsed
            : null;
    }
    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    private static (double? Latitude, double? Longitude) ReadPosition(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        double? latitude = null;
        double? longitude = null;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            string normalizedField = NormalizeField(property.Name);
            if (normalizedField is "latitude" or "lat")
            {
                latitude = ReadDouble(property.Value);
            }
            else if (normalizedField is "longitude" or "lng" or "lon")
            {
                longitude = ReadDouble(property.Value);
            }
        }

        return (latitude, longitude);
    }
    private static void ApplyPosition(GeolocatedEntityBase entity, double? latitude, double? longitude, List<string> updatedFields)
    {
        double? resolvedLatitude = latitude ?? entity.Position?.Latitude;
        double? resolvedLongitude = longitude ?? entity.Position?.Longitude;
        if (!resolvedLatitude.HasValue || !resolvedLongitude.HasValue)
        {
            return;
        }

        try
        {
            entity.SetPosition(resolvedLatitude.Value, resolvedLongitude.Value);
            updatedFields.Add("position");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }
    private static ApplicationResult<LocalizedContentApplyResult> Success(LocalizedContentEntityType entityType, string entityId, IReadOnlyCollection<string> updatedFields, int updatedValueCount)
    {
        return ApplicationResult<LocalizedContentApplyResult>.Success(new LocalizedContentApplyResult(
            LocalizedContentEntityTypeParser.ToApiValue(entityType),
            entityId,
            updatedFields.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            updatedValueCount));
    }
    private static ApplicationResult<LocalizedContentApplyResult> UnsupportedField(LocalizedContentEntityType entityType, string fieldName)
    {
        return ApplicationResult<LocalizedContentApplyResult>.Failure(LocalizedContentApplicationErrors.UnsupportedField(entityType, fieldName));
    }
    private static List<LocalizedText> Merge(IEnumerable<LocalizedText>? existing, IEnumerable<LocalizedText> incoming, bool replaceExisting)
    {
        Dictionary<string, LocalizedText> values = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);
        if (!replaceExisting)
        {
            foreach (LocalizedText value in existing ?? Array.Empty<LocalizedText>())
            {
                string languageCode = NormalizeLanguageCode(value.LanguageCode);
                if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(value.Value))
                {
                    values[languageCode] = new LocalizedText(languageCode, value.Value.Trim());
                }
            }
        }

        foreach (LocalizedText value in incoming)
        {
            string languageCode = NormalizeLanguageCode(value.LanguageCode);
            if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(value.Value))
            {
                values[languageCode] = new LocalizedText(languageCode, value.Value.Trim());
            }
        }

        return values.Values.OrderBy(static value => value.LanguageCode, StringComparer.OrdinalIgnoreCase).ToList();
    }
    private static IReadOnlyCollection<LocalizedTextValue> ToValues(IEnumerable<LocalizedText> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode) && !string.IsNullOrWhiteSpace(value.Value))
            .Select(static value => new LocalizedTextValue(NormalizeLanguageCode(value.LanguageCode), value.Value!.Trim()))
            .ToList();
    }
}
