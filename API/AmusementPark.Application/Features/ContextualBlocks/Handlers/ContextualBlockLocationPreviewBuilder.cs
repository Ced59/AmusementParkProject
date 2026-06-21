using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.ContextualBlocks.Results;

namespace AmusementPark.Application.Features.ContextualBlocks.Handlers;

internal static class ContextualBlockLocationPreviewBuilder
{
    public static List<ContextualBlockPreviewChange> PreviewLocationBlock(
        string entityType,
        string entityId,
        string? displayName,
        double? currentLatitude,
        double? currentLongitude,
        JsonElement block,
        ContextualBlockPreviewResult result)
    {
        List<ContextualBlockPreviewChange> changes = new List<ContextualBlockPreviewChange>();

        bool hasLatitude = TryReadRequiredLocationNumber(block, "latitude", -90, 90, result, out double? newLatitude);
        bool hasLongitude = TryReadRequiredLocationNumber(block, "longitude", -180, 180, result, out double? newLongitude);
        if (!hasLatitude || !hasLongitude)
        {
            return changes;
        }

        if (newLatitude.HasValue != newLongitude.HasValue)
        {
            result.Errors.Add("block.latitude et block.longitude doivent etre fournis ensemble ou null ensemble.");
            return changes;
        }

        if (currentLatitude != newLatitude)
        {
            changes.Add(BuildChange(entityType, entityId, displayName, "latitude", FormatNumber(currentLatitude), FormatNumber(newLatitude)));
        }

        if (currentLongitude != newLongitude)
        {
            changes.Add(BuildChange(entityType, entityId, displayName, "longitude", FormatNumber(currentLongitude), FormatNumber(newLongitude)));
        }

        return result.Errors.Count > 0 ? new List<ContextualBlockPreviewChange>() : changes;
    }

    private static bool TryReadRequiredLocationNumber(
        JsonElement block,
        string fieldName,
        double minValue,
        double maxValue,
        ContextualBlockPreviewResult result,
        out double? value)
    {
        value = null;

        if (!block.TryGetProperty(fieldName, out JsonElement valueElement))
        {
            result.Errors.Add($"block.{fieldName} est requis.");
            return false;
        }

        if (valueElement.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (valueElement.ValueKind != JsonValueKind.Number)
        {
            result.Errors.Add($"block.{fieldName} doit etre un nombre ou null.");
            return false;
        }

        double parsedValue = valueElement.GetDouble();
        if (parsedValue < minValue || parsedValue > maxValue)
        {
            result.Errors.Add($"block.{fieldName} doit etre compris entre {FormatNumber(minValue)} et {FormatNumber(maxValue)}.");
            return false;
        }

        value = parsedValue;
        return true;
    }

    private static ContextualBlockPreviewChange BuildChange(
        string entityType,
        string entityId,
        string? displayName,
        string fieldName,
        string? oldValue,
        string? newValue)
    {
        return new ContextualBlockPreviewChange
        {
            EntityType = entityType,
            EntityId = entityId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? entityId : displayName,
            Field = fieldName,
            ChangeType = "Updated",
            OldValue = oldValue,
            NewValue = newValue,
        };
    }

    private static string? FormatNumber(double? value)
    {
        return value?.ToString("G17", CultureInfo.InvariantCulture);
    }
}
