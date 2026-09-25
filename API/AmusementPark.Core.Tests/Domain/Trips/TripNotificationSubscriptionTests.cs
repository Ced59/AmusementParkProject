using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripNotificationSubscriptionTests
{
    [Fact]
    public void CreateEnabled_ShouldStartAtCurrentSequenceWithoutHistoricalUnreadEvents()
    {
        DateTime nowUtc = new DateTime(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);

        TripNotificationSubscription subscription = TripNotificationSubscription.CreateEnabled(
            TripPlanId.Parse("trip-1"),
            TripMemberId.Parse("member-1"),
            "user-1",
            12,
            nowUtc);

        Assert.True(subscription.IsEnabled);
        Assert.Equal(TripMemberId.Parse("member-1"), subscription.MemberId);
        Assert.Equal(12, subscription.SeenThroughSequence);
        Assert.Equal(1, subscription.Version);
    }

    [Fact]
    public void SetEnabled_ShouldResetCursorOnlyWhenReactivated()
    {
        DateTime nowUtc = new DateTime(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);
        TripNotificationSubscription subscription = TripNotificationSubscription.CreateEnabled(
            TripPlanId.Parse("trip-1"),
            TripMemberId.Parse("member-1"),
            "user-1",
            4,
            nowUtc);

        subscription.SetEnabled(false, 8, nowUtc.AddMinutes(1));
        subscription.SetEnabled(true, 11, nowUtc.AddMinutes(2));

        Assert.True(subscription.IsEnabled);
        Assert.Equal(11, subscription.SeenThroughSequence);
        Assert.Equal(3, subscription.Version);
    }

    [Fact]
    public void MarkSeenThrough_ShouldIgnoreAnOlderSequenceAndAdvanceMonotonically()
    {
        DateTime nowUtc = new DateTime(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);
        TripNotificationSubscription subscription = TripNotificationSubscription.CreateEnabled(
            TripPlanId.Parse("trip-1"),
            TripMemberId.Parse("member-1"),
            "user-1",
            7,
            nowUtc);

        subscription.MarkSeenThrough(6, nowUtc.AddMinutes(1));
        subscription.MarkSeenThrough(10, nowUtc.AddMinutes(2));

        Assert.Equal(10, subscription.SeenThroughSequence);
        Assert.Equal(2, subscription.Version);
    }

    [Theory]
    [InlineData(TripActivityKind.TripRenamed, true)]
    [InlineData(TripActivityKind.PreferencesUpdated, true)]
    [InlineData(TripActivityKind.TripCreated, false)]
    [InlineData(TripActivityKind.PlanExported, false)]
    public void NotificationPolicy_ShouldSeparateCollaborativeChangesFromPrivateNoise(
        TripActivityKind kind,
        bool expected)
    {
        Assert.Equal(expected, TripNotificationPolicy.IsImportant(kind));
    }
}
