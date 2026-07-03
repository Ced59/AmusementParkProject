using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ContextualBlocks.Handlers;

internal static class ContextualBlockPracticalPreviewBuilder
{
    public static List<ContextualBlockPreviewChange> PreviewPracticalBlock(Park park, JsonElement block, ContextualBlockPreviewResult result)
    {
        List<ContextualBlockPreviewChange> changes = new List<ContextualBlockPreviewChange>();

        PreviewCountryCodeField(park, block, result, changes);
        PreviewStringField(park, block, "city", park.City, result, changes);
        PreviewStringField(park, block, "street", park.Street, result, changes);
        PreviewStringField(park, block, "postalCode", park.PostalCode, result, changes);
        PreviewStringField(park, block, "websiteUrl", park.WebsiteUrl, result, changes);
        PreviewStringField(park, block, "founderId", park.FounderId, result, changes);
        PreviewStringField(park, block, "operatorId", park.OperatorId, result, changes);
        PreviewPracticalPosition(park, block, result, changes);

        return result.Errors.Count > 0 ? new List<ContextualBlockPreviewChange>() : changes;
    }

    private static void PreviewCountryCodeField(
        Park park,
        JsonElement block,
        ContextualBlockPreviewResult result,
        List<ContextualBlockPreviewChange> changes)
    {
        if (!block.TryGetProperty("countryCode", out JsonElement value))
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.String && value.ValueKind != JsonValueKind.Null)
        {
            result.Errors.Add("block.countryCode doit etre une chaine ou null.");
            return;
        }

        string? newValue = NormalizeCountryCode(value.ValueKind == JsonValueKind.Null ? null : value.GetString());
        if (!string.Equals(park.CountryCode, newValue, StringComparison.Ordinal))
        {
            changes.Add(BuildChange(park, "countryCode", null, park.CountryCode, newValue));
        }
    }

    private static void PreviewStringField(
        Park park,
        JsonElement block,
        string fieldName,
        string? currentValue,
        ContextualBlockPreviewResult result,
        List<ContextualBlockPreviewChange> changes)
    {
        if (!block.TryGetProperty(fieldName, out JsonElement value))
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.String && value.ValueKind != JsonValueKind.Null)
        {
            result.Errors.Add($"block.{fieldName} doit etre une chaine ou null.");
            return;
        }

        string? newValue = value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        if (!string.Equals(currentValue, newValue, StringComparison.Ordinal))
        {
            changes.Add(BuildChange(park, fieldName, null, currentValue, newValue));
        }
    }

    private static void PreviewPracticalPosition(
        Park park,
        JsonElement block,
        ContextualBlockPreviewResult result,
        List<ContextualBlockPreviewChange> changes)
    {
        bool hasLatitude = block.TryGetProperty("latitude", out JsonElement latitudeElement);
        bool hasLongitude = block.TryGetProperty("longitude", out JsonElement longitudeElement);
        if (!hasLatitude && !hasLongitude)
        {
            return;
        }

        if (hasLatitude && hasLongitude && (IsJsonNull(latitudeElement) != IsJsonNull(longitudeElement)))
        {
            result.Errors.Add("block.latitude et block.longitude doivent etre tous les deux renseignes ou tous les deux null.");
            return;
        }

        if (hasLatitude != hasLongitude)
        {
            JsonElement providedElement = hasLatitude ? latitudeElement : longitudeElement;
            string missingFieldName = hasLatitude ? "longitude" : "latitude";
            if (IsJsonNull(providedElement) || park.Position is null)
            {
                result.Errors.Add($"block.{missingFieldName} est requis pour modifier ou vider une position pratique incomplete.");
                return;
            }
        }

        PreviewNumberField(park, block, "latitude", park.Position?.Latitude, result, changes);
        PreviewNumberField(park, block, "longitude", park.Position?.Longitude, result, changes);
    }

    private static void PreviewNumberField(
        Park park,
        JsonElement block,
        string fieldName,
        double? currentValue,
        ContextualBlockPreviewResult result,
        List<ContextualBlockPreviewChange> changes)
    {
        if (!block.TryGetProperty(fieldName, out JsonElement value))
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.Number && value.ValueKind != JsonValueKind.Null)
        {
            result.Errors.Add($"block.{fieldName} doit etre un nombre ou null.");
            return;
        }

        double? newValue = value.ValueKind == JsonValueKind.Null ? null : value.GetDouble();
        if (newValue.HasValue && string.Equals(fieldName, "latitude", StringComparison.Ordinal) && (newValue.Value < -90 || newValue.Value > 90))
        {
            result.Errors.Add("block.latitude doit etre compris entre -90 et 90.");
            return;
        }

        if (newValue.HasValue && string.Equals(fieldName, "longitude", StringComparison.Ordinal) && (newValue.Value < -180 || newValue.Value > 180))
        {
            result.Errors.Add("block.longitude doit etre compris entre -180 et 180.");
            return;
        }

        if (currentValue != newValue)
        {
            changes.Add(BuildChange(park, fieldName, null, FormatNumber(currentValue), FormatNumber(newValue)));
        }
    }

    private static ContextualBlockPreviewChange BuildChange(Park park, string fieldName, string? languageCode, string? oldValue, string? newValue)
    {
        return new ContextualBlockPreviewChange
        {
            EntityType = nameof(Park),
            EntityId = park.Id,
            DisplayName = string.IsNullOrWhiteSpace(park.Name) ? park.Id : park.Name,
            Field = fieldName,
            LanguageCode = languageCode,
            ChangeType = "Updated",
            OldValue = oldValue,
            NewValue = newValue,
        };
    }

    private static string? NormalizeCountryCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static bool IsJsonNull(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Null;
    }

    private static string? FormatNumber(double? value)
    {
        return value?.ToString("G17", CultureInfo.InvariantCulture);
    }
}
