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
    public void Constructor_WhenRevisionSkipsLatestPredecessor_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => CreateFact(
                revision: 3,
                supersedesRevision: 1));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
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
                1,
                null,
                RecordedAtUtc));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    private static HistoricalFact CreateFact(
        HistoricalFactState state = HistoricalFactState.Verified,
        IReadOnlyCollection<HistoricalSourceRevisionReference>? sourceReferences = null,
        IReadOnlyCollection<HistoricalLocalizedText>? explanations = null,
        HistoricalSubject? subject = null,
        HistoricalPeriod? period = null,
        int? sequenceWithinDate = null,
        int revision = 1,
        int? supersedesRevision = null)
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            subject ?? new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            period ?? HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
            state,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            explanations ?? (state == HistoricalFactState.Verified
                ? Array.Empty<HistoricalLocalizedText>()
                : CreateCompleteExplanations()),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            sequenceWithinDate,
            sourceReferences ?? new[] { new HistoricalSourceRevisionReference(Guid.NewGuid(), 1) },
            null,
            null,
            "history-opening-1998",
            state == HistoricalFactState.Verified ? RecordedAtUtc.AddMinutes(-2) : null,
            RecordedAtUtc.AddMinutes(-1),
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
