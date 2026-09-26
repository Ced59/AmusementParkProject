using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalFactTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_ShouldAcceptPublishedVerifiedFactWithEvidence()
    {
        HistoricalFact fact = CreateFact();

        Assert.True(fact.IsDecisionEligible);
        Assert.Equal(HistoricalPublicationState.Published, fact.PublicationState);
        HistoricalSourceRevisionReference sourceReference = Assert.Single(fact.SourceReferences);
        Assert.Equal(1, sourceReference.Revision);
    }

    [Fact]
    public void Constructor_WhenVerifiedFactHasNoEvidence_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                sourceReferences: Array.Empty<HistoricalSourceRevisionReference>()));

        Assert.Equal(HistoricalPersistenceErrorCodes.MissingSource, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenPublishedProbableFactMissesTranslations_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                state: HistoricalFactState.Probable,
                explanations: new[] { new HistoricalLocalizedText("fr", "Date probable.") }));

        Assert.Equal(HistoricalPersistenceErrorCodes.MissingUncertaintyExplanation, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenSuppressedSubjectWouldBePublished_ShouldRejectFact()
    {
        HistoricalSubject suppressedSubject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc exemple",
            HistoricalSubjectPublicationPolicy.Suppressed);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                subject: suppressedSubject));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenSequenceTargetsPartialDate_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                period: HistoricalPeriod.Point(HistoricalDate.ForYear(1998)),
                sequenceWithinDate: 1));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidSequence, exception.ErrorCode);
    }

    [Theory]
    [InlineData(HistoricalFactType.Opening, LifecycleBoundaryMeaning.FirstClosedDay)]
    [InlineData(HistoricalFactType.Reopening, LifecycleBoundaryMeaning.LastOperatingDay)]
    [InlineData(HistoricalFactType.Closure, LifecycleBoundaryMeaning.FirstOperatingDay)]
    [InlineData(HistoricalFactType.TemporaryClosure, LifecycleBoundaryMeaning.FirstOperatingDay)]
    [InlineData(HistoricalFactType.DefinitiveClosure, LifecycleBoundaryMeaning.FirstOperatingDay)]
    public void Constructor_WhenBoundaryMeaningContradictsTransition_ShouldRejectFact(
        HistoricalFactType type,
        LifecycleBoundaryMeaning boundaryMeaning)
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                type: type,
                lifecycleBoundaryMeaning: boundaryMeaning));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenLifecycleTransitionUsesOpenPeriod_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                period: HistoricalPeriod.From(HistoricalDate.ForDay(1998, 5, 12))));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Theory]
    [InlineData(HistoricalFactType.Renaming, HistoricalAttributeKind.Name)]
    [InlineData(HistoricalFactType.ZoneRenaming, HistoricalAttributeKind.Name)]
    [InlineData(HistoricalFactType.OperatorChange, HistoricalAttributeKind.Operator)]
    [InlineData(HistoricalFactType.OwnerChange, HistoricalAttributeKind.Owner)]
    [InlineData(HistoricalFactType.PositioningChange, HistoricalAttributeKind.Location)]
    [InlineData(HistoricalFactType.Relocation, HistoricalAttributeKind.Location)]
    [InlineData(HistoricalFactType.Retheming, HistoricalAttributeKind.Theme)]
    [InlineData(HistoricalFactType.ManufacturerChange, HistoricalAttributeKind.Manufacturer)]
    [InlineData(HistoricalFactType.ZoneMove, HistoricalAttributeKind.Zone)]
    public void Constructor_WhenAttributeTransitionUsesOpenPeriod_ShouldRejectFact(
        HistoricalFactType type,
        HistoricalAttributeKind attributeKind)
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                CreateAttributeTransitionFact(
                    type,
                    attributeKind,
                    HistoricalPeriod.From(HistoricalDate.ForDay(1998, 5, 12))));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Theory]
    [InlineData(HistoricalSubjectType.Park, HistoricalFactType.Dismantling)]
    [InlineData(HistoricalSubjectType.ParkItem, HistoricalFactType.ZoneCreation)]
    [InlineData(HistoricalSubjectType.StandaloneAttraction, HistoricalFactType.OwnerChange)]
    [InlineData(HistoricalSubjectType.ParkZone, HistoricalFactType.ManufacturerChange)]
    [InlineData(HistoricalSubjectType.ParkOperator, HistoricalFactType.ZoneMove)]
    [InlineData(HistoricalSubjectType.AttractionManufacturer, HistoricalFactType.Retheming)]
    public void Constructor_WhenFactTypeIsIncompatibleWithSubject_ShouldRejectFact(
        HistoricalSubjectType subjectType,
        HistoricalFactType factType)
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                CreateSubjectSpecificFact(subjectType, factType));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Theory]
    [InlineData(HistoricalSubjectType.Park, HistoricalFactType.ZoneCreation)]
    [InlineData(HistoricalSubjectType.ParkItem, HistoricalFactType.Dismantling)]
    [InlineData(HistoricalSubjectType.StandaloneAttraction, HistoricalFactType.ManufacturerChange)]
    [InlineData(HistoricalSubjectType.ParkZone, HistoricalFactType.Retheming)]
    [InlineData(HistoricalSubjectType.ParkOperator, HistoricalFactType.OwnerChange)]
    [InlineData(HistoricalSubjectType.AttractionManufacturer, HistoricalFactType.PositioningChange)]
    public void Constructor_WhenFactTypeMatchesSubject_ShouldAcceptFact(
        HistoricalSubjectType subjectType,
        HistoricalFactType factType)
    {
        HistoricalFact fact = CreateSubjectSpecificFact(subjectType, factType);

        Assert.Equal(subjectType, fact.Subject.Type);
        Assert.Equal(factType, fact.Type);
    }

    [Fact]
    public void Constructor_WhenRevisionDoesNotLinkEarlierRevision_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                revision: 2,
                supersedesRevision: null));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenRevisionIsNotPositive_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                revision: 0,
                supersedesRevision: -1));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenRevisionSkipsLatestPredecessor_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                revision: 3,
                supersedesRevision: 1));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenFirstRevisionClaimsCorrection_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                revision: 1,
                supersedesRevision: null,
                workflowState: HistoricalEditorialWorkflowState.Corrected));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenOrdinaryFirstRevisionIsPublished_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                revision: 1,
                supersedesRevision: null));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenLegacyMigrationUsesDedicatedInitialState_ShouldAcceptFact()
    {
        HistoricalFact fact = new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            CreateCompleteExplanations(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            "history-opening-1998",
            null,
            null,
            "hist-migration-v1",
            1,
            null,
            RecordedAtUtc,
            HistoricalRevisionOrigin.LegacyMigration);

        Assert.Equal(HistoricalRevisionOrigin.LegacyMigration, fact.RevisionOrigin);
    }

    [Fact]
    public void Constructor_WhenPublicationPrecedesVerification_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                verifiedAtUtc: RecordedAtUtc.AddMinutes(-1),
                publishedAtUtc: RecordedAtUtc.AddMinutes(-2)));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidTimestamp, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenRetractedStateUsesDraftLifecycle_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => new HistoricalFact(
                Guid.NewGuid(),
                new HistoricalSubject(
                    HistoricalSubjectType.Park,
                    "park-1",
                    "Parc exemple",
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
                HistoricalFactType.Opening,
                HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
                HistoricalFactState.Retracted,
                HistoricalImportance.Major,
                HistoricalEditorialWorkflowState.Draft,
                HistoricalPublicationState.Draft,
                Array.Empty<HistoricalLocalizedText>(),
                LifecycleBoundaryMeaning.FirstOperatingDay,
                null,
                null,
                null,
                Array.Empty<HistoricalSourceRevisionReference>(),
                null,
                null,
                null,
                null,
                null,
                null,
                2,
                1,
                RecordedAtUtc));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenTwoRevisionsOfSameSourceAreAttached_ShouldRejectFact()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                sourceReferences: new[]
                {
                    new HistoricalSourceRevisionReference(sourceId, 1),
                    new HistoricalSourceRevisionReference(sourceId, 2),
                }));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_ShouldExposeValidatedCollectionsAsReadOnly()
    {
        HistoricalFact fact = CreateFact(state: HistoricalFactState.Probable);

        IList<HistoricalLocalizedText> explanations = Assert.IsAssignableFrom<IList<HistoricalLocalizedText>>(
            fact.PublicUncertaintyExplanation);
        IList<HistoricalSourceRevisionReference> sources = Assert.IsAssignableFrom<IList<HistoricalSourceRevisionReference>>(
            fact.SourceReferences);

        Assert.Throws<NotSupportedException>(() => explanations[0] = new HistoricalLocalizedText("de", "Geändert."));
        Assert.Throws<NotSupportedException>(() => sources[0] = new HistoricalSourceRevisionReference(Guid.NewGuid(), 1));
    }

    [Fact]
    public void Constructor_WhenAttributeTransitionHasNoNewValue_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => new HistoricalFact(
                Guid.NewGuid(),
                new HistoricalSubject(
                    HistoricalSubjectType.Park,
                    "park-1",
                    "Parc exemple",
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
                HistoricalFactType.Renaming,
                HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
                HistoricalFactState.Verified,
                HistoricalImportance.Major,
                HistoricalEditorialWorkflowState.Published,
                HistoricalPublicationState.Published,
                Array.Empty<HistoricalLocalizedText>(),
                null,
                HistoricalAttributeKind.Name,
                AttributeBoundaryMeaning.FirstDayOfNewValue,
                null,
                new[] { new HistoricalSourceRevisionReference(Guid.NewGuid(), 1) },
                null,
                null,
                null,
                RecordedAtUtc.AddMinutes(-2),
                RecordedAtUtc.AddMinutes(-1),
                "hist-v1",
                2,
                1,
                RecordedAtUtc));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Theory]
    [InlineData(HistoricalEditorialWorkflowState.Draft)]
    [InlineData(HistoricalEditorialWorkflowState.SourcesAttached)]
    [InlineData(HistoricalEditorialWorkflowState.EditorialReview)]
    public void Constructor_WhenFactIsVerifiedBeforeStructuredValidation_ShouldRejectFact(
        HistoricalEditorialWorkflowState workflowState)
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                CreateVerifiedPrePublicationFact(workflowState));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Fact]
    public void Constructor_WhenFactIsVerifiedAtStructuredValidation_ShouldAcceptFact()
    {
        HistoricalFact fact = CreateVerifiedPrePublicationFact(
            HistoricalEditorialWorkflowState.StructuredValidation);

        Assert.Equal(HistoricalFactState.Verified, fact.State);
        Assert.Equal(HistoricalEditorialWorkflowState.StructuredValidation, fact.WorkflowState);
    }

    private static HistoricalFact CreateVerifiedPrePublicationFact(
        HistoricalEditorialWorkflowState workflowState)
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
            HistoricalFactState.Verified,
            HistoricalImportance.Major,
            workflowState,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            new[] { new HistoricalSourceRevisionReference(Guid.NewGuid(), 1) },
            null,
            null,
            "history-opening-1998",
            RecordedAtUtc.AddMinutes(-2),
            null,
            null,
            2,
            1,
            RecordedAtUtc);
    }

    private static HistoricalFact CreateAttributeTransitionFact(
        HistoricalFactType type,
        HistoricalAttributeKind attributeKind,
        HistoricalPeriod period)
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                type is HistoricalFactType.Relocation
                    or HistoricalFactType.Retheming
                    or HistoricalFactType.ManufacturerChange
                    or HistoricalFactType.ZoneMove
                    ? HistoricalSubjectType.ParkItem
                    : HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            type,
            period,
            HistoricalFactState.Verified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            null,
            attributeKind,
            AttributeBoundaryMeaning.FirstDayOfNewValue,
            null,
            new[] { new HistoricalSourceRevisionReference(Guid.NewGuid(), 1) },
            "new-value",
            null,
            "history-attribute-transition",
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            2,
            1,
            RecordedAtUtc);
    }

    private static HistoricalFact CreateSubjectSpecificFact(
        HistoricalSubjectType subjectType,
        HistoricalFactType factType)
    {
        HistoricalAttributeKind? attributeKind = factType switch
        {
            HistoricalFactType.OwnerChange => HistoricalAttributeKind.Owner,
            HistoricalFactType.PositioningChange => HistoricalAttributeKind.Location,
            HistoricalFactType.Retheming => HistoricalAttributeKind.Theme,
            HistoricalFactType.ManufacturerChange => HistoricalAttributeKind.Manufacturer,
            _ => null,
        };
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                subjectType,
                "subject-1",
                "Sujet exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            factType,
            HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
            HistoricalFactState.Verified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            null,
            attributeKind,
            attributeKind.HasValue ? AttributeBoundaryMeaning.FirstDayOfNewValue : null,
            null,
            new[] { new HistoricalSourceRevisionReference(Guid.NewGuid(), 1) },
            attributeKind.HasValue ? "new-value" : null,
            null,
            "history-subject-specific",
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            2,
            1,
            RecordedAtUtc);
    }

    private static HistoricalFact CreateFact(
        HistoricalFactState state = HistoricalFactState.Verified,
        IReadOnlyCollection<HistoricalSourceRevisionReference>? sourceReferences = null,
        IReadOnlyCollection<HistoricalLocalizedText>? explanations = null,
        HistoricalSubject? subject = null,
        HistoricalPeriod? period = null,
        int? sequenceWithinDate = null,
        int revision = 5,
        int? supersedesRevision = 4,
        HistoricalEditorialWorkflowState workflowState = HistoricalEditorialWorkflowState.Published,
        DateTime? verifiedAtUtc = null,
        DateTime? publishedAtUtc = null,
        HistoricalFactType type = HistoricalFactType.Opening,
        LifecycleBoundaryMeaning lifecycleBoundaryMeaning = LifecycleBoundaryMeaning.FirstOperatingDay)
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            subject ?? new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            type,
            period ?? HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
            state,
            HistoricalImportance.Major,
            workflowState,
            HistoricalPublicationState.Published,
            explanations ?? (state == HistoricalFactState.Verified
                ? Array.Empty<HistoricalLocalizedText>()
                : CreateCompleteExplanations()),
            lifecycleBoundaryMeaning,
            null,
            null,
            sequenceWithinDate,
            sourceReferences ?? new[] { new HistoricalSourceRevisionReference(Guid.NewGuid(), 1) },
            null,
            null,
            "history-opening-1998",
            verifiedAtUtc ?? (state == HistoricalFactState.Verified ? RecordedAtUtc.AddMinutes(-2) : null),
            publishedAtUtc ?? RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            revision,
            supersedesRevision,
            RecordedAtUtc);
    }

    private static IReadOnlyCollection<HistoricalLocalizedText> CreateCompleteExplanations()
    {
        return HistoricalLocalizationPolicy.SupportedLanguageCodes
            .Select(languageCode => new HistoricalLocalizedText(languageCode, "Uncertainty explained."))
            .ToArray();
    }
}
