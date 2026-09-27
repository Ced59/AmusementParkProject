using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.History;

public static class HistoricalParkItemClassificationPolicy
{
    public static bool IsAttractionClassification(string? value)
    {
        if (TryParseCategory(value, out ParkItemCategory category))
        {
            return category == ParkItemCategory.Attraction;
        }

        return TryResolveDetailedAttractionType(value, out ParkItemType _);
    }

    public static bool TryResolveDetailedAttractionType(
        string? value,
        out ParkItemType type)
    {
        type = default;
        if (TryParseCategory(value, out ParkItemCategory _)
            || !Enum.TryParse(value, true, out ParkItemType parsed)
            || !Enum.IsDefined(parsed)
            || parsed is ParkItemType.Attraction or ParkItemType.Other
            || !ParkItemAdministrationDefaults.IsTypeAllowedForCategory(
                ParkItemCategory.Attraction,
                parsed))
        {
            return false;
        }

        type = parsed;
        return true;
    }

    private static bool TryParseCategory(
        string? value,
        out ParkItemCategory category)
    {
        return Enum.TryParse(value, true, out category)
            && Enum.IsDefined(category);
    }
}
