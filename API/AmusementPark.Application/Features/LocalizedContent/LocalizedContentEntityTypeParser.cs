namespace AmusementPark.Application.Features.LocalizedContent;

/// <summary>
/// Parse les aliases d'entités localisables côté administration.
/// </summary>
public static class LocalizedContentEntityTypeParser
{
    private static readonly IReadOnlyDictionary<string, LocalizedContentEntityType> EntityTypesByAlias = new Dictionary<string, LocalizedContentEntityType>(StringComparer.OrdinalIgnoreCase)
    {
        ["park"] = LocalizedContentEntityType.Park,
        ["parks"] = LocalizedContentEntityType.Park,
        ["parkzone"] = LocalizedContentEntityType.ParkZone,
        ["zone"] = LocalizedContentEntityType.ParkZone,
        ["zones"] = LocalizedContentEntityType.ParkZone,
        ["parkitem"] = LocalizedContentEntityType.ParkItem,
        ["item"] = LocalizedContentEntityType.ParkItem,
        ["element"] = LocalizedContentEntityType.ParkItem,
        ["attraction"] = LocalizedContentEntityType.ParkItem,
        ["parkoperator"] = LocalizedContentEntityType.ParkOperator,
        ["operator"] = LocalizedContentEntityType.ParkOperator,
        ["exploitant"] = LocalizedContentEntityType.ParkOperator,
        ["parkfounder"] = LocalizedContentEntityType.ParkFounder,
        ["founder"] = LocalizedContentEntityType.ParkFounder,
        ["fondateur"] = LocalizedContentEntityType.ParkFounder,
        ["attractionmanufacturer"] = LocalizedContentEntityType.AttractionManufacturer,
        ["manufacturer"] = LocalizedContentEntityType.AttractionManufacturer,
        ["constructeur"] = LocalizedContentEntityType.AttractionManufacturer,
        ["image"] = LocalizedContentEntityType.Image,
        ["asset"] = LocalizedContentEntityType.Image,
        ["imagetag"] = LocalizedContentEntityType.ImageTag,
        ["tag"] = LocalizedContentEntityType.ImageTag,
        ["accessconditiontype"] = LocalizedContentEntityType.AccessConditionType,
        ["conditiontype"] = LocalizedContentEntityType.AccessConditionType,
        ["attractionaccessconditiontype"] = LocalizedContentEntityType.AccessConditionType,
        ["accesscondition"] = LocalizedContentEntityType.AccessConditionType,
        ["condition"] = LocalizedContentEntityType.AccessConditionType,
    };

    private static readonly IReadOnlyDictionary<LocalizedContentEntityType, string> ApiValuesByEntityType = new Dictionary<LocalizedContentEntityType, string>
    {
        [LocalizedContentEntityType.Park] = LocalizedContentEntityTypes.Park,
        [LocalizedContentEntityType.ParkZone] = LocalizedContentEntityTypes.ParkZone,
        [LocalizedContentEntityType.ParkItem] = LocalizedContentEntityTypes.ParkItem,
        [LocalizedContentEntityType.ParkOperator] = LocalizedContentEntityTypes.ParkOperator,
        [LocalizedContentEntityType.ParkFounder] = LocalizedContentEntityTypes.ParkFounder,
        [LocalizedContentEntityType.AttractionManufacturer] = LocalizedContentEntityTypes.AttractionManufacturer,
        [LocalizedContentEntityType.Image] = LocalizedContentEntityTypes.Image,
        [LocalizedContentEntityType.ImageTag] = LocalizedContentEntityTypes.ImageTag,
        [LocalizedContentEntityType.AccessConditionType] = LocalizedContentEntityTypes.AccessConditionType,
    };

    public static bool TryParse(string? value, out LocalizedContentEntityType entityType)
    {
        string normalized = Normalize(value);
        if (EntityTypesByAlias.TryGetValue(normalized, out LocalizedContentEntityType parsedEntityType))
        {
            entityType = parsedEntityType;
            return true;
        }

        entityType = default;
        return false;
    }

    public static string ToApiValue(LocalizedContentEntityType entityType)
    {
        return ApiValuesByEntityType.TryGetValue(entityType, out string? apiValue)
            ? apiValue
            : entityType.ToString();
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }
}
