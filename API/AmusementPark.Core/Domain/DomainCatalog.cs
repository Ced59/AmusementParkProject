namespace AmusementPark.Core.Domain;

/// <summary>
/// Catalogue des agrégats et objets métier extraits dans le Core pur.
/// </summary>
public static class DomainCatalog
{
    /// <summary>
    /// Types métier extraits pendant la phase 3.
    /// </summary>
    public static IReadOnlyList<string> ExtractedTypes { get; } = new[]
    {
        "Country",
        "Park",
        "ParkZone",
        "ParkItem",
        "ParkFounder",
        "ParkOperator",
        "AttractionManufacturer",
        "TechnicalPage",
        "AttractionDetails",
        "AttractionLocations",
        "AttractionAccessCondition",
        "Image",
        "ImageExifMetadata",
        "ImageTag",
        "Video",
        "VideoExternalMetadata",
        "VideoTag",
        "User",
        "ExternalLogin",
        "UserRating",
        "RatingAggregate",
        "RatingScoreCalculator",
    };
}
