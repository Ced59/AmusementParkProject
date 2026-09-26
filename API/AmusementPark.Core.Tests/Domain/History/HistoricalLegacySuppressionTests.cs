using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalLegacySuppressionTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenLegacySubjectIsSuppressed_ShouldAcceptNonPublicFact()
    {
        HistoricalFact fact = CreateSuppressedLegacyFact();

        Assert.Equal(HistoricalPublicationState.Suppressed, fact.PublicationState);
        Assert.False(fact.IsDecisionEligible);
    }

    [Fact]
    public void Constructor_WhenOrdinaryRevisionClaimsSuppressedState_ShouldRejectFact()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => new HistoricalFact(
                Guid.NewGuid(),
                CreateSuppressedSubject(),
                HistoricalFactType.Announcement,
                HistoricalPeriod.Point(HistoricalDate.ForYear(1998)),
                HistoricalFactState.Unverified,
                HistoricalImportance.Standard,
                HistoricalEditorialWorkflowState.EditorialReview,
                HistoricalPublicationState.Suppressed,
                Array.Empty<HistoricalLocalizedText>(),
                null,
                null,
                null,
                null,
                Array.Empty<HistoricalSourceRevisionReference>(),
                null,
                null,
                "legacy-event-1",
                null,
                null,
                "hist-v1-legacy",
                2,
                1,
                RecordedAtUtc));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void ValidateFactTarget_WhenSuppressedLegacyFactIsMigrated_ShouldAcceptEvent()
    {
        HistoricalFact fact = CreateSuppressedLegacyFact();
        HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
            Guid.NewGuid(),
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            HistoricalReviewEventType.Migrated,
            "system:hist-04-migration",
            null,
            RecordedAtUtc);

        HistoricalReviewEventTargetValidator.ValidateFactTarget(reviewEvent, fact);
    }

    [Fact]
    public void CreateRetraction_WhenLegacyFactIsSuppressed_ShouldPreserveTheRevisionChain()
    {
        HistoricalFact predecessor = CreateSuppressedLegacyFact();

        HistoricalFact retraction = predecessor.CreateRetraction(RecordedAtUtc.AddMinutes(1));

        Assert.Equal(HistoricalFactState.Retracted, retraction.State);
        Assert.Equal(HistoricalPublicationState.Withdrawn, retraction.PublicationState);
        Assert.Equal(predecessor.Revision + 1, retraction.Revision);
        Assert.Equal(predecessor.Revision, retraction.SupersedesRevision);
        HistoricalFactRevisionValidator.ValidatePredecessor(retraction, predecessor);
    }

    private static HistoricalFact CreateSuppressedLegacyFact()
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            CreateSuppressedSubject(),
            HistoricalFactType.Announcement,
            HistoricalPeriod.Point(HistoricalDate.ForYear(1998)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Standard,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.Suppressed,
            Array.Empty<HistoricalLocalizedText>(),
            null,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            "legacy-event-1",
            null,
            null,
            "hist-v1-legacy",
            1,
            null,
            RecordedAtUtc,
            HistoricalRevisionOrigin.LegacyMigration);
    }

    private static HistoricalSubject CreateSuppressedSubject()
    {
        return new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-hidden",
            "Parc masqué",
            HistoricalSubjectPublicationPolicy.Suppressed);
    }
}
