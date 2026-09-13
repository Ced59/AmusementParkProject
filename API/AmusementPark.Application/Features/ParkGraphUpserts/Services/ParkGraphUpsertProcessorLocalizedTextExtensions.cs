using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
internal static class ParkGraphUpsertProcessorLocalizedTextExtensions
{
    internal static List<LocalizedText> MergeLocalizedTexts(IReadOnlyCollection<LocalizedText> current, JsonElement? array, bool replace)
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
                string languageCode = ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(item.LanguageCode);
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

            string? languageCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "languageCode")?.ToLowerInvariant();
            string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(item, "value")?.Trim();
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

    internal static List<LocalizedText> PatchLocalizedTexts(IReadOnlyCollection<LocalizedText> current, JsonElement? array, bool replace, ParkGraphUpsertChange change, string fieldPrefix)
    {
        List<LocalizedText> merged = ParkGraphUpsertProcessorLocalizedTextExtensions.MergeLocalizedTexts(current, array, replace);
        ParkGraphUpsertProcessorLocalizedTextExtensions.AddLocalizedTextChanges(change, fieldPrefix, current, merged);
        return merged;
    }

    internal static void AddLocalizedTextChanges(ParkGraphUpsertChange change, string fieldPrefix, IReadOnlyCollection<LocalizedText> current, IReadOnlyCollection<LocalizedText> next)
    {
        Dictionary<string, string> currentValues = ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextMap(current);
        Dictionary<string, string> nextValues = ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextMap(next);
        List<string> languageCodes = currentValues.Keys.Concat(nextValues.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(static languageCode => languageCode, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (string languageCode in languageCodes)
        {
            currentValues.TryGetValue(languageCode, out string? oldValue);
            nextValues.TryGetValue(languageCode, out string? newValue);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, $"{fieldPrefix}.{languageCode}", oldValue, newValue);
        }
    }

    internal static Dictionary<string, string> ToLocalizedTextMap(IReadOnlyCollection<LocalizedText> texts)
    {
        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (LocalizedText text in texts)
        {
            string languageCode = ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(text.LanguageCode);
            string? value = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(text.Value);
            if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(value))
            {
                values[languageCode] = value;
            }
        }

        return values;
    }

    internal static List<LocalizedText> ReadLocalizedTexts(JsonElement? array)
    {
        return ParkGraphUpsertProcessorLocalizedTextExtensions.MergeLocalizedTexts(Array.Empty<LocalizedText>(), array, true);
    }

    internal static List<string> ReadStringArray(JsonElement? array)
    {
        if (array is null || array.Value.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        List<string> values = new List<string>();
        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            string? value = item.ValueKind == JsonValueKind.String ? ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(item.GetString()) : ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(item.ToString());
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values.Distinct(StringComparer.Ordinal).ToList();
    }

    internal static IReadOnlyCollection<LocalizedTextValue> ToLocalizedTextValues(IReadOnlyCollection<LocalizedText> values)
    {
        return values.Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode) && !string.IsNullOrWhiteSpace(value.Value)).Select(static value => new LocalizedTextValue(value.LanguageCode, value.Value ?? string.Empty)).ToList();
    }

    internal static string DescribeLocalized(IReadOnlyCollection<LocalizedText> texts)
    {
        return string.Join(", ", texts.Select(static text => text.LanguageCode).OrderBy(static value => value, StringComparer.OrdinalIgnoreCase));
    }

    internal static string? FormatPosition(GeoPoint? point)
    {
        if (point is null)
        {
            return null;
        }

        return $"{point.Latitude.ToString(CultureInfo.InvariantCulture)},{point.Longitude.ToString(CultureInfo.InvariantCulture)}";
    }

    internal static string? FormatGeoPointValue(GeoPointValue? point)
    {
        if (point is null)
        {
            return null;
        }

        return $"{point.Latitude.ToString(CultureInfo.InvariantCulture)},{point.Longitude.ToString(CultureInfo.InvariantCulture)}";
    }

    internal static string? FormatValue(object? value)
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

    internal static string NormalizeKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

}
