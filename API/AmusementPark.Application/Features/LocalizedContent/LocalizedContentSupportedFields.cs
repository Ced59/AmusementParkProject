namespace AmusementPark.Application.Features.LocalizedContent;

/// <summary>
/// Catalogue applicatif des champs localisables supportés par type d'entité.
/// </summary>
public static class LocalizedContentSupportedFields
{
    public static IReadOnlyCollection<string> For(LocalizedContentEntityType entityType)
    {
        return entityType switch
        {
            LocalizedContentEntityType.Park => new[] { "descriptions" },
            LocalizedContentEntityType.ParkZone => new[] { "names", "descriptions" },
            LocalizedContentEntityType.ParkItem => new[] { "descriptions", "accessConditions" },
            LocalizedContentEntityType.ParkOperator => new[] { "description" },
            LocalizedContentEntityType.ParkFounder => new[] { "biography" },
            LocalizedContentEntityType.AttractionManufacturer => new[] { "biography" },
            LocalizedContentEntityType.Image => new[] { "altTexts", "captions", "credits" },
            LocalizedContentEntityType.ImageTag => new[] { "labels", "descriptions" },
            _ => Array.Empty<string>(),
        };
    }
}
