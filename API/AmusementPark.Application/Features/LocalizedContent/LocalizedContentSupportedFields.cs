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
            LocalizedContentEntityType.Park => new[] { "name", "countryCode", "type", "founderId", "operatorId", "websiteUrl", "address", "position", "isVisible", "adminReviewStatus", "descriptions" },
            LocalizedContentEntityType.ParkZone => new[] { "parkId", "name", "slug", "sortOrder", "isVisible", "names", "descriptions", "position" },
            LocalizedContentEntityType.ParkItem => new[] { "parkId", "zoneId", "name", "category", "type", "subtype", "isVisible", "adminReviewStatus", "position", "descriptions", "attractionDetails", "accessConditions" },
            LocalizedContentEntityType.ParkOperator => new[] { "name", "legalName", "foundedYear", "closedYear", "contactDetails", "adminReviewStatus", "description" },
            LocalizedContentEntityType.ParkFounder => new[] { "name", "occupation", "birthDate", "deathDate", "birthPlace", "nationalityCountryCode", "websiteUrl", "biography" },
            LocalizedContentEntityType.AttractionManufacturer => new[] { "name", "legalName", "foundedYear", "closedYear", "contactDetails", "adminReviewStatus", "biography" },
            LocalizedContentEntityType.Image => new[] { "altTexts", "captions", "credits" },
            LocalizedContentEntityType.ImageTag => new[] { "slug", "isActive", "labels", "descriptions" },
            LocalizedContentEntityType.AccessConditionType => new[] { "key", "legacyType", "labels", "descriptions", "isActive", "sortOrder" },
            _ => Array.Empty<string>(),
        };
    }
}
