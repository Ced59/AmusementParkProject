using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalReviewEventChronologyValidatorTests
{
    private static readonly DateTime OccurredAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Validate_WhenEventFollowsPredecessor_ShouldAcceptEvent()
    {
        Guid resourceId = Guid.NewGuid();
        HistoricalReviewEvent predecessor = CreateEvent(resourceId, 1, OccurredAtUtc);
        HistoricalReviewEvent reviewEvent = CreateEvent(
            resourceId,
            2,
            OccurredAtUtc.AddMinutes(1));

        HistoricalReviewEventChronologyValidator.Validate(reviewEvent, predecessor);
    }

    [Fact]
    public void Validate_WhenEventPredatesPredecessorAudit_ShouldRejectEvent()
    {
        Guid resourceId = Guid.NewGuid();
        HistoricalReviewEvent predecessor = CreateEvent(
            resourceId,
            1,
            OccurredAtUtc.AddMinutes(2));
        HistoricalReviewEvent reviewEvent = CreateEvent(
            resourceId,
            2,
            OccurredAtUtc.AddMinutes(1));

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalReviewEventChronologyValidator.Validate(reviewEvent, predecessor));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidReviewEvent, exception.ErrorCode);
    }

    [Fact]
    public void Validate_WhenSuccessorHasNoPredecessorAudit_ShouldRejectEvent()
    {
        HistoricalReviewEvent reviewEvent = CreateEvent(
            Guid.NewGuid(),
            2,
            OccurredAtUtc);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalReviewEventChronologyValidator.Validate(reviewEvent, null));
    }

    private static HistoricalReviewEvent CreateEvent(
        Guid resourceId,
        int revision,
        DateTime occurredAtUtc)
    {
        return new HistoricalReviewEvent(
            Guid.NewGuid(),
            HistoricalReviewResourceType.Fact,
            resourceId,
            revision,
            revision == 1
                ? HistoricalReviewEventType.Created
                : HistoricalReviewEventType.DraftUpdated,
            "admin-1",
            null,
            occurredAtUtc);
    }
}
