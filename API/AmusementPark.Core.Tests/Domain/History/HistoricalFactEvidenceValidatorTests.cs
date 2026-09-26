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
    public void Validate_WhenPartialSourcesOnlyCoverCoreAssertionTogether_ShouldRejectEvidence()
    {
        Guid identitySourceId = Guid.NewGuid();
        Guid eventSourceId = Guid.NewGuid();
        HistoricalFact fact = CreateFact(
            new[] { identitySourceId, eventSourceId },
            referenceScopesFactory: sourceId => sourceId == identitySourceId
                ? new[]
                {
                    HistoricalSourceScope.SubjectIdentity,
                    HistoricalSourceScope.HistoricalLabel,
                }
                : new[]
                {
                    HistoricalSourceScope.FactType,
                    HistoricalSourceScope.Period,
                });
        HistoricalSourceReference identitySource = CreateSource(
            identitySourceId,
            scopes: new[]
            {
                HistoricalSourceScope.SubjectIdentity,
                HistoricalSourceScope.HistoricalLabel,
            });
        HistoricalSourceReference eventSource = CreateSource(
            eventSourceId,
            scopes: new[]
            {
                HistoricalSourceScope.FactType,
                HistoricalSourceScope.Period,
            });

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalFactEvidenceValidator.Validate(fact, new[] { identitySource, eventSource }));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidSourceScope, exception.ErrorCode);
    }

    [Fact]
    public void Validate_WhenCitationIsBoundToAnotherSubject_ShouldRejectEvidence()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalFact fact = CreateFact(new[] { sourceId }, referenceSubjectId: "park-2");
        HistoricalSourceReference source = CreateSource(sourceId);

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
        return CreateFact(new[] { sourceId }, sequenceWithinDate);
    }

    private static HistoricalFact CreateFact(
        IReadOnlyCollection<Guid> sourceIds,
        int? sequenceWithinDate = null,
        string referenceSubjectId = "park-1",
        Func<Guid, IReadOnlyCollection<HistoricalSourceScope>>? referenceScopesFactory = null)
    {
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12));
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            period,
            HistoricalFactState.Verified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            sequenceWithinDate,
            sourceIds
                .Select(sourceId =>
                {
                    IReadOnlyCollection<HistoricalSourceScope> referenceScopes =
                        referenceScopesFactory?.Invoke(sourceId)
                        ?? (sequenceWithinDate.HasValue
                            ? new[]
                            {
                                HistoricalSourceScope.SubjectIdentity,
                                HistoricalSourceScope.HistoricalLabel,
                                HistoricalSourceScope.FactType,
                                HistoricalSourceScope.Period,
                                HistoricalSourceScope.SequenceWithinDate,
                            }
                            : new[]
                            {
                                HistoricalSourceScope.SubjectIdentity,
                                HistoricalSourceScope.HistoricalLabel,
                                HistoricalSourceScope.FactType,
                                HistoricalSourceScope.Period,
                            });
                    return new HistoricalSourceRevisionReference(
                        sourceId,
                        2,
                        HistoricalSubjectType.Park,
                        referenceSubjectId,
                        HistoricalFactType.Opening,
                        period,
                        referenceScopes);
                })
                .ToArray(),
            null,
            null,
            null,
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            5,
            4,
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
