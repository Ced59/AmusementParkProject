namespace AmusementPark.Core.Domain.Parks;

public sealed record ParkItemDataCompletenessContext
{
    public bool ParentParkResolved { get; init; } = true;

    public bool ParentParkVisible { get; init; }

    public bool HasNoDuplicateInPark { get; init; } = true;

    public bool HasOfficialZoneContext { get; init; }

    public bool HasUsefulVisitGrouping { get; init; }

    public bool HasRepresentativeImage { get; init; }

    public bool HasResolvedImageOwner { get; init; }

    public bool HasLocalizedImageAltText { get; init; }

    public bool HasNonMisleadingImage { get; init; }

    public bool HasOriginalMedia { get; init; }

    public bool HasHistoricalImageContext { get; init; }

    public int HistoryEventCount { get; init; }

    public int ClosureOrChangeHistoryEventCount { get; init; }

    public bool HasTimelineConsistentWithParent { get; init; } = true;

    public int PublishedArticleCount { get; init; }

    public int HistoryEventsWithSourcesCount { get; init; }

    public bool HasReferenceDetailsOrDocumentedDebt { get; init; }

    public bool HasInternalLinks { get; init; }

    public bool HasNoDuplicateReferences { get; init; } = true;

    public bool HasSeoSignals { get; init; }

    public bool HasNoPlaceholderPublicPage { get; init; } = true;

    public bool HasStructuredDataSignals { get; init; }

    public bool HasHumanReviewOrDocumentedDebt { get; init; }

    public bool HasNoUnresolvedReferences { get; init; } = true;

    public bool HasNoForbiddenPublicText { get; init; } = true;
}
