namespace AmusementPark.Core.Domain.Parks;

public sealed record ParkDataCompletenessContext
{
    public bool ProjectForPublication { get; init; }

    public int ParkItemsTotalCount { get; init; }

    public int ParkItemsVisibleCount { get; init; }

    public int DistinctParkItemCategoryCount { get; init; }

    public int ClosedImportantParkItemsCount { get; init; }

    public int ParkItemsWithKnownStatusOrDatesCount { get; init; }

    public int AttractionManufacturerIdsCount { get; init; }

    public int AttractionsWithAccessConditionsCount { get; init; }

    public bool HasOfficialZones { get; init; }

    public int ZonesTotalCount { get; init; }

    public int ZonesWithDescriptionsCount { get; init; }

    public int ParkItemsAttachedToZonesCount { get; init; }

    public int ParkItemsWithDescriptionsCount { get; init; }

    public int CommercialOrServiceItemsWithDescriptionsCount { get; init; }

    public int ParkPublishedImageCount { get; init; }

    public int ParkImagesWithResolvedOwnerCount { get; init; }

    public int ParkImagesWithLocalizedAltTextCount { get; init; }

    public int ParkItemPublishedImageCount { get; init; }

    public bool HasPublishedCurrentLogo { get; init; }

    public bool HasOriginalMedia { get; init; }

    public bool HasOpeningHours { get; init; }

    public ParkOpeningHoursAdminStatus OpeningHoursStatus { get; init; } = ParkOpeningHoursAdminStatus.NotConfigured;

    public bool HasOpeningHoursSource { get; init; }

    public bool HasOpeningHoursTimeZone { get; init; }

    public bool HasOpeningHoursExceptions { get; init; }

    public bool HasOpeningHoursRecentVerification { get; init; }

    public int ParkHistoryEventCount { get; init; }

    public int MajorHistoryEventCount { get; init; }

    public int ParkItemHistoryEventCount { get; init; }

    public int PublishedArticleCount { get; init; }

    public int StructuredArticleCount { get; init; }

    public int LocalizedHistoryContentCount { get; init; }

    public int HistoryEventsWithSourcesCount { get; init; }

    public int HistoryEventsWithMediaCount { get; init; }

    public int ImportantReferencesWithDescriptionsCount { get; init; }

    public int ReferencesWithUsefulDetailsCount { get; init; }

    public bool HasNoProbableDuplicate { get; init; } = true;

    public bool HasCleanLegacyDataOrDocumentedDebt { get; init; } = true;

    public bool HasResolvedAttachmentKeys { get; init; } = true;

    public bool HasNoKnownBlockingWarnings { get; init; } = true;

    public bool HasCriticalSources { get; init; }

    public bool HasNoInventedDates { get; init; } = true;

    public bool HasStructuredTechnicalDataOnly { get; init; } = true;

    public bool HasNoForbiddenPublicText { get; init; } = true;

    public bool HasNoFormulaicPublicText { get; init; } = true;

    public bool HasDocumentedRemainingDebt { get; init; }

    public bool HasPublicSeoSignals { get; init; }
}
