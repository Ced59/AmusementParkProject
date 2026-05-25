namespace AmusementPark.Application.Features.LocalizedContent;

/// <summary>
/// Parse les aliases d'entités localisables côté administration.
/// </summary>
public static class LocalizedContentEntityTypeParser
{
    public static bool TryParse(string? value, out LocalizedContentEntityType entityType)
    {
        string normalized = Normalize(value);
        switch (normalized)
        {
            case "park":
            case "parks":
                entityType = LocalizedContentEntityType.Park;
                return true;
            case "parkzone":
            case "zone":
            case "zones":
                entityType = LocalizedContentEntityType.ParkZone;
                return true;
            case "parkitem":
            case "item":
            case "element":
            case "attraction":
                entityType = LocalizedContentEntityType.ParkItem;
                return true;
            case "parkoperator":
            case "operator":
            case "exploitant":
                entityType = LocalizedContentEntityType.ParkOperator;
                return true;
            case "parkfounder":
            case "founder":
            case "fondateur":
                entityType = LocalizedContentEntityType.ParkFounder;
                return true;
            case "attractionmanufacturer":
            case "manufacturer":
            case "constructeur":
                entityType = LocalizedContentEntityType.AttractionManufacturer;
                return true;
            case "image":
            case "asset":
                entityType = LocalizedContentEntityType.Image;
                return true;
            case "imagetag":
            case "tag":
                entityType = LocalizedContentEntityType.ImageTag;
                return true;
            case "accessconditiontype":
            case "conditiontype":
            case "attractionaccessconditiontype":
            case "accesscondition":
            case "condition":
                entityType = LocalizedContentEntityType.AccessConditionType;
                return true;
            default:
                entityType = default;
                return false;
        }
    }

    public static string ToApiValue(LocalizedContentEntityType entityType)
    {
        return entityType switch
        {
            LocalizedContentEntityType.Park => LocalizedContentEntityTypes.Park,
            LocalizedContentEntityType.ParkZone => LocalizedContentEntityTypes.ParkZone,
            LocalizedContentEntityType.ParkItem => LocalizedContentEntityTypes.ParkItem,
            LocalizedContentEntityType.ParkOperator => LocalizedContentEntityTypes.ParkOperator,
            LocalizedContentEntityType.ParkFounder => LocalizedContentEntityTypes.ParkFounder,
            LocalizedContentEntityType.AttractionManufacturer => LocalizedContentEntityTypes.AttractionManufacturer,
            LocalizedContentEntityType.Image => LocalizedContentEntityTypes.Image,
            LocalizedContentEntityType.ImageTag => LocalizedContentEntityTypes.ImageTag,
            LocalizedContentEntityType.AccessConditionType => LocalizedContentEntityTypes.AccessConditionType,
            _ => entityType.ToString(),
        };
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }
}
