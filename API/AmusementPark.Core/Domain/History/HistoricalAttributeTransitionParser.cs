using System.Text.Json;

namespace AmusementPark.Core.Domain.History;

internal static class HistoricalAttributeTransitionParser
{
    internal static bool TryParse(
        HistoricalFact fact,
        out string? previousValue,
        out string? nextValue)
    {
        previousValue = null;
        nextValue = null;
        if (!fact.AttributeKind.HasValue || string.IsNullOrWhiteSpace(fact.StructuredValue))
        {
            return false;
        }

        string structuredValue = fact.StructuredValue.Trim();
        if (!structuredValue.StartsWith('{'))
        {
            nextValue = structuredValue;
            return true;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(structuredValue);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            (string previousKey, string nextKey) = ResolveKeys(fact.AttributeKind.Value);
            previousValue = ReadString(document.RootElement, previousKey)
                ?? ReadString(document.RootElement, "previous")
                ?? ReadString(document.RootElement, "previousId");
            nextValue = ReadString(document.RootElement, nextKey)
                ?? ReadString(document.RootElement, "next")
                ?? ReadString(document.RootElement, "nextId");
            return nextValue is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static (string PreviousKey, string NextKey) ResolveKeys(HistoricalAttributeKind kind)
    {
        return kind switch
        {
            HistoricalAttributeKind.Logo => ("previousImageId", "nextImageId"),
            HistoricalAttributeKind.Operator
                or HistoricalAttributeKind.Owner
                or HistoricalAttributeKind.Manufacturer
                or HistoricalAttributeKind.Zone => ("previousId", "nextId"),
            _ => ("previous", "next"),
        };
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        string? value = property.GetString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
