using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripNotificationCleanupReconcilerTests
{
    private static readonly DateTime NowUtc =
        new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReconcileAsync_ShouldDeleteOnlyInaccessibleSubscriptionsAndAdvanceCursor()
    {
        TripPlan accessibleTrip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            NowUtc);
        TripNotificationSubscription accessible = Subscription(
            "subscription-a",
            accessibleTrip.Id,
            Assert.Single(accessibleTrip.Members).Id,
            "user-1");
        TripNotificationSubscription orphan = Subscription(
            "subscription-b",
            TripPlanId.Parse("trip-2"),
            TripMemberId.Parse("departed-member"),
            "user-2");
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListForCleanupAsync(
                null,
                2,
                CancellationToken.None))
            .ReturnsAsync(new[] { accessible, orphan });
        subscriptions.Setup(repository => repository.DeleteIfCurrentAsync(
                orphan,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                accessible.UserId,
                accessible.TripPlanId,
                CancellationToken.None))
            .ReturnsAsync(accessibleTrip);
        plans.Setup(repository => repository.GetAccessibleAsync(
                orphan.UserId,
                orphan.TripPlanId,
                CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        TripNotificationCleanupReconciler reconciler = new(
            plans.Object,
            subscriptions.Object);

        TripNotificationCleanupBatchResult result = await reconciler.ReconcileAsync(
            null,
            2,
            CancellationToken.None);

        Assert.Equal(2, result.ScannedCount);
        Assert.Equal(1, result.DeletedCount);
        Assert.Equal(orphan.Id, result.NextCursor);
        plans.VerifyAll();
        subscriptions.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenDeletionFails_ShouldRetryTheSamePageLater()
    {
        TripNotificationSubscription orphan = Subscription(
            "subscription-a",
            TripPlanId.Parse("trip-2"),
            TripMemberId.Parse("departed-member"),
            "user-2");
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListForCleanupAsync(
                "previous-cursor",
                25,
                CancellationToken.None))
            .ReturnsAsync(new[] { orphan });
        subscriptions.Setup(repository => repository.DeleteIfCurrentAsync(
                orphan,
                CancellationToken.None))
            .ThrowsAsync(new IOException("Transient cleanup failure."));
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                orphan.UserId,
                orphan.TripPlanId,
                CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        TripNotificationCleanupReconciler reconciler = new(
            plans.Object,
            subscriptions.Object);

        await Assert.ThrowsAsync<IOException>(() => reconciler.ReconcileAsync(
            "previous-cursor",
            25,
            CancellationToken.None));

        plans.VerifyAll();
        subscriptions.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldDeleteSubscriptionFromAPreviousMembership()
    {
        TripPlan currentTrip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            NowUtc);
        TripNotificationSubscription stale = Subscription(
            "subscription-a",
            currentTrip.Id,
            TripMemberId.Parse("previous-membership"),
            currentTrip.OwnerUserId);
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListForCleanupAsync(
                null,
                25,
                CancellationToken.None))
            .ReturnsAsync(new[] { stale });
        subscriptions.Setup(repository => repository.DeleteIfCurrentAsync(
                stale,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                stale.UserId,
                stale.TripPlanId,
                CancellationToken.None))
            .ReturnsAsync(currentTrip);
        TripNotificationCleanupReconciler reconciler = new(
            plans.Object,
            subscriptions.Object);

        TripNotificationCleanupBatchResult result = await reconciler.ReconcileAsync(
            null,
            25,
            CancellationToken.None);

        Assert.Equal(1, result.DeletedCount);
        Assert.Null(result.NextCursor);
        plans.VerifyAll();
        subscriptions.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_WhenSubscriptionWasReplaced_ShouldNotDeleteOrCountIt()
    {
        TripNotificationSubscription stale = Subscription(
            "subscription-a",
            TripPlanId.Parse("trip-2"),
            TripMemberId.Parse("departed-member"),
            "user-2");
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListForCleanupAsync(
                null,
                25,
                CancellationToken.None))
            .ReturnsAsync(new[] { stale });
        subscriptions.Setup(repository => repository.DeleteIfCurrentAsync(
                stale,
                CancellationToken.None))
            .ReturnsAsync(false);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                stale.UserId,
                stale.TripPlanId,
                CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        TripNotificationCleanupReconciler reconciler = new(
            plans.Object,
            subscriptions.Object);

        TripNotificationCleanupBatchResult result = await reconciler.ReconcileAsync(
            null,
            25,
            CancellationToken.None);

        Assert.Equal(1, result.ScannedCount);
        Assert.Equal(0, result.DeletedCount);
        plans.VerifyAll();
        subscriptions.VerifyAll();
    }

    private static TripNotificationSubscription Subscription(
        string id,
        TripPlanId tripPlanId,
        TripMemberId memberId,
        string userId)
    {
        return TripNotificationSubscription.Restore(
            id,
            tripPlanId,
            memberId,
            userId,
            true,
            0,
            NowUtc,
            NowUtc,
            1);
    }
}
