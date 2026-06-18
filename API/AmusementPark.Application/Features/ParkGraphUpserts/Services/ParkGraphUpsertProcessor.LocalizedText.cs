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
    private static List<LocalizedText> MergeLocalizedTexts(IReadOnlyCollection<LocalizedText> current, JsonElement? array, bool replace)
    {
        if (array is null || array.Value.ValueKind != JsonValueKind.Array)
        {
            return replace ? new List<LocalizedText>() : current.ToList();
        }

        Dictionary<string, LocalizedText> values = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);
        if (!replace)
        {
            foreach (LocalizedText item in current)
            {
                string languageCode = NormalizeKey(item.LanguageCode);
                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    values[languageCode] = new LocalizedText(languageCode, item.Value);
                }
            }
        }

        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? languageCode = ReadString(item, "languageCode")?.ToLowerInvariant();
            string? value = ReadStringAllowNull(item, "value")?.Trim();
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                values.Remove(languageCode);
            }
            else
            {
                values[languageCode] = new LocalizedText(languageCode, value);
            }
        }

        return values.Values.ToList();
    }
    private static List<LocalizedText> PatchLocalizedTexts(IReadOnlyCollection<LocalizedText> current, JsonElement? array, bool replace, ParkGraphUpsertChange change, string fieldPrefix)
    {
        List<LocalizedText> merged = MergeLocalizedTexts(current, array, replace);
        AddLocalizedTextChanges(change, fieldPrefix, current, merged);
        return merged;
    }
    private static void AddLocalizedTextChanges(ParkGraphUpsertChange change, string fieldPrefix, IReadOnlyCollection<LocalizedText> current, IReadOnlyCollection<LocalizedText> next)
    {
        Dictionary<string, string> currentValues = ToLocalizedTextMap(current);
        Dictionary<string, string> nextValues = ToLocalizedTextMap(next);
        List<string> languageCodes = currentValues.Keys
            .Concat(nextValues.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static languageCode => languageCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (string languageCode in languageCodes)
        {
            currentValues.TryGetValue(languageCode, out string? oldValue);
            nextValues.TryGetValue(languageCode, out string? newValue);
            AddChange(change, $"{fieldPrefix}.{languageCode}", oldValue, newValue);
        }
    }
    private static Dictionary<string, string> ToLocalizedTextMap(IReadOnlyCollection<LocalizedText> texts)
    {
        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (LocalizedText text in texts)
        {
            string languageCode = NormalizeKey(text.LanguageCode);
            string? value = NormalizeString(text.Value);
            if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(value))
            {
                values[languageCode] = value;
            }
        }

        return values;
    }
    private static List<LocalizedText> ReadLocalizedTexts(JsonElement? array)
    {
        return MergeLocalizedTexts(Array.Empty<LocalizedText>(), array, true);
    }
    private static List<string> ReadStringArray(JsonElement? array)
    {
        if (array is null || array.Value.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        List<string> values = new List<string>();
        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            string? value = item.ValueKind == JsonValueKind.String ? NormalizeString(item.GetString()) : NormalizeString(item.ToString());
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values.Distinct(StringComparer.Ordinal).ToList();
    }
    private static IReadOnlyCollection<LocalizedTextValue> ToLocalizedTextValues(IReadOnlyCollection<LocalizedText> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode) && !string.IsNullOrWhiteSpace(value.Value))
            .Select(static value => new LocalizedTextValue(value.LanguageCode, value.Value ?? string.Empty))
            .ToList();
    }
    private static string DescribeLocalized(IReadOnlyCollection<LocalizedText> texts)
    {
        return string.Join(", ", texts.Select(static text => text.LanguageCode).OrderBy(static value => value, StringComparer.OrdinalIgnoreCase));
    }
    private static string? FormatPosition(GeoPoint? point)
    {
        if (point is null)
        {
            return null;
        }

        return $"{point.Latitude.ToString(CultureInfo.InvariantCulture)},{point.Longitude.ToString(CultureInfo.InvariantCulture)}";
    }
    private static string? FormatGeoPointValue(GeoPointValue? point)
    {
        if (point is null)
        {
            return null;
        }

        return $"{point.Latitude.ToString(CultureInfo.InvariantCulture)},{point.Longitude.ToString(CultureInfo.InvariantCulture)}";
    }
    private static string? FormatValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        if (value is DateTime dateValue)
        {
            return dateValue.ToString("O", CultureInfo.InvariantCulture);
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }
    private static string NormalizeKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
