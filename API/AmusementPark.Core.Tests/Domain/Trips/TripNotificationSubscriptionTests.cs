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
            new TripNotificationBoundary(12, new[] { "pending-1" }),
            nowUtc);

        Assert.True(subscription.IsEnabled);
        Assert.Equal(TripMemberId.Parse("member-1"), subscription.MemberId);
        Assert.Equal(12, subscription.SeenThroughSequence);
        Assert.Equal(new[] { "pending-1" }, subscription.PendingOperationKeys);
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
            new TripNotificationBoundary(4),
            nowUtc);

        subscription.SetEnabled(false, new TripNotificationBoundary(8), nowUtc.AddMinutes(1));
        subscription.SetEnabled(
            true,
            new TripNotificationBoundary(11, new[] { "pending-2" }),
            nowUtc.AddMinutes(2));

        Assert.True(subscription.IsEnabled);
        Assert.Equal(11, subscription.SeenThroughSequence);
        Assert.Equal(new[] { "pending-2" }, subscription.PendingOperationKeys);
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
            new TripNotificationBoundary(7),
            nowUtc);

        subscription.MarkSeenThrough(new TripNotificationBoundary(6), nowUtc.AddMinutes(1));
        subscription.MarkSeenThrough(new TripNotificationBoundary(10), nowUtc.AddMinutes(2));

        Assert.Equal(10, subscription.SeenThroughSequence);
        Assert.Equal(2, subscription.Version);
    }

    [Fact]
    public void MarkSeenThrough_ShouldPersistANewPendingFenceAtTheSameSequence()
    {
        DateTime nowUtc = new DateTime(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);
        TripNotificationSubscription subscription = TripNotificationSubscription.CreateEnabled(
            TripPlanId.Parse("trip-1"),
            TripMemberId.Parse("member-1"),
            "user-1",
            new TripNotificationBoundary(7, new[] { "pending-1" }),
            nowUtc);

        subscription.MarkSeenThrough(
            new TripNotificationBoundary(7, new[] { "pending-2" }),
            nowUtc.AddMinutes(1));

        Assert.Equal(7, subscription.SeenThroughSequence);
        Assert.Equal(new[] { "pending-2" }, subscription.PendingOperationKeys);
        Assert.Equal(2, subscription.Version);
    }

    [Fact]
    public void SetEnabled_ShouldClampTheInformativeTimestampWhenTheClockMovesBackward()
    {
        DateTime nowUtc = new DateTime(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);
        TripNotificationSubscription subscription = TripNotificationSubscription.CreateEnabled(
            TripPlanId.Parse("trip-1"),
            TripMemberId.Parse("member-1"),
            "user-1",
            new TripNotificationBoundary(7),
            nowUtc);

        subscription.SetEnabled(
            false,
            new TripNotificationBoundary(7),
            nowUtc.AddMinutes(-1));

        Assert.False(subscription.IsEnabled);
        Assert.Equal(nowUtc, subscription.UpdatedAtUtc);
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
