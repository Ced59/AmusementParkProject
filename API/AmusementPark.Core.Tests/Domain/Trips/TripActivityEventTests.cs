using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripActivityEventTests
{
    [Fact]
    public void Constructor_ShouldAllowAnInvitationActorWithoutMembershipAndKeepUtcEvidence()
    {
        DateTime occurredAtUtc = new DateTime(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);

        TripActivityEvent activity = new TripActivityEvent(
            "activity-1",
            TripPlanId.Parse("trip-1"),
            null,
            null,
            TripActivityKind.InvitationDeclined,
            "invitation:decline:operation-1",
            7,
            1,
            occurredAtUtc);

        Assert.Null(activity.ActorMemberId);
        Assert.Equal(7, activity.Sequence);
        Assert.Equal(DateTimeKind.Utc, activity.OccurredAtUtc.Kind);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, TripActivityEvent.MaximumAffectedCount + 1)]
    public void Constructor_ShouldRejectAnInvalidSequenceOrAffectedCount(long sequence, int affectedCount)
    {
        Assert.Throws<TripPlanValidationException>(() => new TripActivityEvent(
            "activity-1",
            TripPlanId.Parse("trip-1"),
            TripMemberId.New(),
            TripEffectiveRole.Owner,
            TripActivityKind.TripRenamed,
            "root:rename:2",
            sequence,
            affectedCount,
            new DateTime(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc)));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Constructor_ShouldRejectAPartialActorIdentity(bool hasMember, bool hasRole)
    {
        Assert.Throws<TripPlanValidationException>(() => new TripActivityEvent(
            "activity-1",
            TripPlanId.Parse("trip-1"),
            hasMember ? TripMemberId.New() : null,
            hasRole ? TripEffectiveRole.Owner : null,
            TripActivityKind.TripRenamed,
            "root:rename:2",
            1,
            1,
            new DateTime(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc)));
    }
}
