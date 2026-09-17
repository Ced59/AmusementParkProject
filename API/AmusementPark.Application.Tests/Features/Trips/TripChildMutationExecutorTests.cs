using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripChildMutationExecutorTests
{
    [Fact]
    public async Task ExecuteOwnedAsync_WhenTheMutationReturns_ShouldReleaseTheAcquiredLease()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripChildMutationLease lease = new(
            "operation-1",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddSeconds(20));
        Mock<ITripChildMutationLeaseRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.TryAcquireOwnedAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        repository.Setup(item => item.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .Returns(Task.CompletedTask);
        TripChildMutationExecutor executor = new(
            repository.Object,
            NullLogger<TripChildMutationExecutor>.Instance);

        ApplicationResult result = await executor.ExecuteOwnedAsync(
            trip,
            "operation-1",
            _ => Task.FromResult(ApplicationResult.Failure(
                TripPlanApplicationErrors.ChildMutationUnavailable())),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteOwnedAsync_WhenLeaseCannotBeAcquired_ShouldNotInvokeTheMutation()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        Mock<ITripChildMutationLeaseRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.TryAcquireOwnedAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TripChildMutationLease?)null);
        TripChildMutationExecutor executor = new(
            repository.Object,
            NullLogger<TripChildMutationExecutor>.Instance);
        bool invoked = false;

        ApplicationResult result = await executor.ExecuteOwnedAsync(
            trip,
            "operation-1",
            _ =>
            {
                invoked = true;
                return Task.FromResult(ApplicationResult.Success());
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(invoked);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteOwnedAsync_WhenReleaseFails_ShouldKeepTheSuccessfulBusinessResult()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripChildMutationLease lease = new(
            "operation-1",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddSeconds(20));
        Mock<ITripChildMutationLeaseRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.TryAcquireOwnedAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        repository.Setup(item => item.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Transient MongoDB failure."));
        TripChildMutationExecutor executor = new(
            repository.Object,
            NullLogger<TripChildMutationExecutor>.Instance);

        ApplicationResult result = await executor.ExecuteOwnedAsync(
            trip,
            "operation-1",
            _ => Task.FromResult(ApplicationResult.Success()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteOwnedAsync_WhenTheMutationOutcomeIsAmbiguous_ShouldKeepTheLeaseUntilExpiry()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripChildMutationLease lease = new(
            "operation-1",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddSeconds(20));
        Mock<ITripChildMutationLeaseRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.TryAcquireOwnedAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        TripChildMutationExecutor executor = new(
            repository.Object,
            NullLogger<TripChildMutationExecutor>.Instance);

        await Assert.ThrowsAsync<TimeoutException>(() => executor.ExecuteOwnedAsync<string>(
            trip,
            "operation-1",
            _ => Task.FromException<ApplicationResult<string>>(
                new TimeoutException("The MongoDB outcome is unknown.")),
            CancellationToken.None));

        repository.Verify(
            item => item.ReleaseAsync(
                It.IsAny<TripPlanId>(),
                It.IsAny<TripChildMutationLease>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        repository.VerifyAll();
    }
}
