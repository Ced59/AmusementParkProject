using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalReviewEventTests
{
    [Fact]
    public void Constructor_ShouldNormalizeActorAndKeepImmutableTarget()
    {
        Guid resourceId = Guid.NewGuid();
        HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
            Guid.NewGuid(),
            HistoricalReviewResourceType.Fact,
            resourceId,
            3,
            HistoricalReviewEventType.Corrected,
            " admin-1 ",
            " Correction sourcée. ",
            new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal(resourceId, reviewEvent.ResourceId);
        Assert.Equal(3, reviewEvent.ResourceRevision);
        Assert.Equal("admin-1", reviewEvent.ActorUserId);
        Assert.Equal("Correction sourcée.", reviewEvent.PrivateNote);
    }

    [Fact]
    public void Constructor_WhenTimestampIsNotUtc_ShouldRejectReviewEvent()
    {
        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() => new HistoricalReviewEvent(
                Guid.NewGuid(),
                HistoricalReviewResourceType.Fact,
                Guid.NewGuid(),
                1,
                HistoricalReviewEventType.Created,
                "admin-1",
                null,
                new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Local)));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidTimestamp, exception.ErrorCode);
    }
}
