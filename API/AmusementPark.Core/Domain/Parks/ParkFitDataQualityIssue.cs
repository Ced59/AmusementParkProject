namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Code stable d'une lacune empêchant ou limitant l'utilisation d'un parc par FIT.
/// </summary>
public enum ParkFitDataQualityIssue
{
    NotPubliclyDiscoverable,
    MissingCoordinates,
    MissingParkType,
    MissingSupportedLanguageContent,
    MissingOpeningCalendar,
    OpeningCalendarStale,
    NoVisibleAttractions,
    MissingPreciseAttractionType,
    MissingIndoorOutdoorClassification,
    MissingAccessibilitySource,
    MissingAccessConditions,
    MissingAuthoritativeSource,
    MissingEvidenceTimestamp,
    StaleEvidence,
    AmbiguousRestriction,
    IncompleteRestrictionCoverage,
}
