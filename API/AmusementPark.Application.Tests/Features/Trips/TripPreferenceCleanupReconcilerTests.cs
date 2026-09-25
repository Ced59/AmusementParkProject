using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripPreferenceCleanupReconcilerTests
{
    [Fact]
    public async Task ReconcileAsync_WhenNotificationDeletionFails_ShouldKeepDurableMarker()
    {
        TripDepartureCleanup cleanup = new(TripPlanId.Parse("trip-1"), "user-2");
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        preferences.Setup(repository => repository.ListPendingDepartureCleanupAsync(
                25,
                CancellationToken.None))
            .ReturnsAsync(new[] { cleanup });
        preferences.Setup(repository => repository.CompleteDepartureCleanupAsync(
                cleanup.TripPlanId,
                cleanup.UserId,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<ITripNotificationSubscriptionRepository> notifications = new(MockBehavior.Strict);
        notifications.Setup(repository => repository.DeleteForMemberAsync(
                cleanup.TripPlanId,
                cleanup.UserId,
                CancellationToken.None))
            .ThrowsAsync(new IOException("Transient notification cleanup failure."));
        TripPreferenceCleanupReconciler reconciler = new(
            preferences.Object,
            notifications.Object);

        await Assert.ThrowsAsync<IOException>(() => reconciler.ReconcileAsync(
            25,
            CancellationToken.None));

        preferences.Verify(repository => repository.CompleteDepartureCleanupMarkerAsync(
                It.IsAny<TripPlanId>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        preferences.VerifyAll();
        notifications.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldClearMarkerOnlyAfterAllPrivateDataIsDeleted()
    {
        TripDepartureCleanup cleanup = new(TripPlanId.Parse("trip-1"), "user-2");
        List<string> operations = new();
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        preferences.Setup(repository => repository.ListPendingDepartureCleanupAsync(
                25,
                CancellationToken.None))
            .ReturnsAsync(new[] { cleanup });
        preferences.Setup(repository => repository.CompleteDepartureCleanupAsync(
                cleanup.TripPlanId,
                cleanup.UserId,
                CancellationToken.None))
            .Callback(() => operations.Add("preferences"))
            .ReturnsAsync(true);
        preferences.Setup(repository => repository.CompleteDepartureCleanupMarkerAsync(
                cleanup.TripPlanId,
                cleanup.UserId,
                CancellationToken.None))
            .Callback(() => operations.Add("marker"))
            .Returns(Task.CompletedTask);
        Mock<ITripNotificationSubscriptionRepository> notifications = new(MockBehavior.Strict);
        notifications.Setup(repository => repository.DeleteForMemberAsync(
                cleanup.TripPlanId,
                cleanup.UserId,
                CancellationToken.None))
            .Callback(() => operations.Add("notification"))
            .Returns(Task.CompletedTask);
        TripPreferenceCleanupReconciler reconciler = new(
            preferences.Object,
            notifications.Object);

        int completed = await reconciler.ReconcileAsync(25, CancellationToken.None);

        Assert.Equal(1, completed);
        Assert.Equal(new[] { "preferences", "notification", "marker" }, operations);
        preferences.VerifyAll();
        notifications.VerifyAll();
    }
}
