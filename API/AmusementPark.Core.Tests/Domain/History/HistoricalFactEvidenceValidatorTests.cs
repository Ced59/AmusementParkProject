using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalFactEvidenceValidatorTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Validate_WhenExactPublishedEvidenceCoversFact_ShouldAcceptEvidence()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalFact fact = CreateFact(sourceId);
        HistoricalSourceReference source = CreateSource(sourceId);

        HistoricalFactEvidenceValidator.Validate(fact, new[] { source });
    }

    [Fact]
    public void Validate_WhenExactRevisionDoesNotExist_ShouldRejectEvidence()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalFact fact = CreateFact(sourceId);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalFactEvidenceValidator.Validate(fact, Array.Empty<HistoricalSourceReference>()));

        Assert.Equal(HistoricalPersistenceErrorCodes.MissingSource, exception.ErrorCode);
    }

    [Fact]
    public void Validate_WhenEvidenceIsStillDraft_ShouldRejectVerifiedFact()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalFact fact = CreateFact(sourceId);
        HistoricalSourceReference source = CreateSource(
            sourceId,
            HistoricalEditorialWorkflowState.StructuredValidation,
            HistoricalPublicationState.Draft);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalFactEvidenceValidator.Validate(fact, new[] { source }));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Fact]
    public void Validate_WhenEvidenceDoesNotCoverPeriod_ShouldRejectVerifiedFact()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalFact fact = CreateFact(sourceId);
        HistoricalSourceReference source = CreateSource(
            sourceId,
            scopes: new[]
            {
                HistoricalSourceScope.SubjectIdentity,
                HistoricalSourceScope.HistoricalLabel,
                HistoricalSourceScope.FactType,
            });

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalFactEvidenceValidator.Validate(fact, new[] { source }));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidSourceScope, exception.ErrorCode);
    }

    [Fact]
    public void Validate_WhenSameDaySequenceIsNotCovered_ShouldRejectEvidence()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalFact fact = CreateFact(sourceId, sequenceWithinDate: 1);
        HistoricalSourceReference source = CreateSource(sourceId);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalFactEvidenceValidator.Validate(fact, new[] { source }));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidSourceScope, exception.ErrorCode);
    }

    [Fact]
    public void Validate_WhenSameDaySequenceIsCovered_ShouldAcceptEvidence()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalFact fact = CreateFact(sourceId, sequenceWithinDate: 1);
        HistoricalSourceReference source = CreateSource(
            sourceId,
            scopes: new[]
            {
                HistoricalSourceScope.SubjectIdentity,
                HistoricalSourceScope.HistoricalLabel,
                HistoricalSourceScope.FactType,
                HistoricalSourceScope.Period,
                HistoricalSourceScope.SequenceWithinDate,
            });

        HistoricalFactEvidenceValidator.Validate(fact, new[] { source });
    }

    private static HistoricalFact CreateFact(Guid sourceId, int? sequenceWithinDate = null)
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
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            sequenceWithinDate,
            new[] { new HistoricalSourceRevisionReference(sourceId, 2) },
            null,
            null,
            null,
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            1,
            null,
            RecordedAtUtc);
    }

    private static HistoricalSourceReference CreateSource(
        Guid sourceId,
        HistoricalEditorialWorkflowState workflowState = HistoricalEditorialWorkflowState.Published,
        HistoricalPublicationState publicationState = HistoricalPublicationState.Published,
        IReadOnlyCollection<HistoricalSourceScope>? scopes = null)
    {
        return new HistoricalSourceReference(
            sourceId,
            2,
            HistoricalSourceType.OfficialWebsite,
            "Page historique officielle",
            "Parc exemple",
            "https://example.com/history",
            null,
            new DateOnly(1998, 5, 12),
            new DateOnly(2026, 9, 25),
            "fr",
            null,
            scopes ?? new[]
            {
                HistoricalSourceScope.SubjectIdentity,
                HistoricalSourceScope.HistoricalLabel,
                HistoricalSourceScope.FactType,
                HistoricalSourceScope.Period,
            },
            null,
            HistoricalSourceAccessibility.Accessible,
            workflowState,
            publicationState,
            RecordedAtUtc);
    }
}
