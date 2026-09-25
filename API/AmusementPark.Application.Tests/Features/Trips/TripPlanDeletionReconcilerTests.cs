using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripPlanDeletionReconcilerTests
{
    [Fact]
    public async Task ReconcileAsync_ShouldPurgeChildrenBeforeFinalizingPendingDeletion()
    {
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            DateTime.UtcNow.AddMinutes(-2));
        trip.BeginDeletion(DateTime.UtcNow.AddMinutes(-1));
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        Mock<ITripNotificationSubscriptionRepository> notifications = new(MockBehavior.Strict);
        repository.Setup(item => item.ListPendingDeletionAsync(25, CancellationToken.None))
            .ReturnsAsync(new[] { trip });
        MockSequence sequence = new();
        repository.InSequence(sequence)
            .Setup(item => item.PurgeChildrenAsync(trip.Id, CancellationToken.None))
            .Returns(Task.CompletedTask);
        notifications.InSequence(sequence)
            .Setup(item => item.DeleteForTripAsync(trip.Id, CancellationToken.None))
            .Returns(Task.CompletedTask);
        repository.InSequence(sequence)
            .Setup(item => item.FinalizeDeletionOwnedAsync(trip, CancellationToken.None))
            .ReturnsAsync(new TripPlanWriteResult(TripPlanWriteOutcome.Success, trip.Version));
        TripPlanDeletionReconciler reconciler = new(repository.Object, notifications.Object);

        int completed = await reconciler.ReconcileAsync(25, CancellationToken.None);

        Assert.Equal(1, completed);
        repository.VerifyAll();
        notifications.VerifyAll();
    }
}
