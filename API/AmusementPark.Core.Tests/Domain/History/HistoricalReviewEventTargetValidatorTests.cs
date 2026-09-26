using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalReviewEventTargetValidatorTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidateFactTarget_WhenPublishedEventMatchesRevision_ShouldAcceptEvent()
    {
        HistoricalFact fact = CreatePublishedFact();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            HistoricalReviewEventType.Published);

        HistoricalReviewEventTargetValidator.ValidateFactTarget(reviewEvent, fact);
    }

    [Fact]
    public void ValidateFactTarget_WhenTargetDoesNotExist_ShouldRejectEvent()
    {
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Fact,
            Guid.NewGuid(),
            1,
            HistoricalReviewEventType.Published);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalReviewEventTargetValidator.ValidateFactTarget(reviewEvent, null));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidReviewEvent, exception.ErrorCode);
    }

    [Fact]
    public void ValidateSourceTarget_WhenPublishedEventTargetsDraft_ShouldRejectEvent()
    {
        HistoricalSourceReference source = CreateDraftSource();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            HistoricalReviewEventType.Published);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalReviewEventTargetValidator.ValidateSourceTarget(reviewEvent, source));
    }

    private static HistoricalReviewEvent CreateEvent(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int resourceRevision,
        HistoricalReviewEventType eventType)
    {
        return new HistoricalReviewEvent(
            Guid.NewGuid(),
            resourceType,
            resourceId,
            resourceRevision,
            eventType,
            "admin-1",
            null,
            RecordedAtUtc.AddMinutes(1));
    }

    private static HistoricalFact CreatePublishedFact()
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
            null,
            new[] { new HistoricalSourceRevisionReference(Guid.NewGuid(), 1) },
            null,
            null,
            "history-opening-1998",
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            1,
            null,
            RecordedAtUtc);
    }

    private static HistoricalSourceReference CreateDraftSource()
    {
        return new HistoricalSourceReference(
            Guid.NewGuid(),
            1,
            HistoricalSourceType.OfficialWebsite,
            "Page officielle",
            "Parc exemple",
            "https://example.com/history",
            null,
            null,
            new DateOnly(2026, 9, 25),
            "fr",
            null,
            new[] { HistoricalSourceScope.Period },
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            RecordedAtUtc);
    }
}
