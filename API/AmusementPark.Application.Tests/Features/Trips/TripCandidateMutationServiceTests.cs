using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripCandidateMutationServiceTests
{
    [Fact]
    public async Task UpdateAsync_ShouldPersistAndRecordTheCandidateChangeUnderTheSharedLease()
    {
        DateTime nowUtc = new DateTime(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        DateOnly localDate = new DateOnly(2027, 7, 8);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage",
            TripDateProposal.Fixed(localDate),
            "Europe/Paris",
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate candidate = TripParkCandidate.Create(
            TripParkCandidateId.New(),
            trip.Id,
            "park-1",
            new[] { localDate },
            TripParkCandidateSource.Manual,
            null,
            null,
            owner.Id,
            TripParkCandidate.SortPositionStep,
            nowUtc);
        long expectedCandidateVersion = candidate.Version;
        Mock<ITripPlanRepository> trips = new Mock<ITripPlanRepository>(MockBehavior.Strict);
        trips.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates =
            new Mock<ITripParkCandidateRepository>(MockBehavior.Strict);
        candidates.Setup(repository => repository.GetAsync(
                trip.Id,
                candidate.Id,
                CancellationToken.None))
            .ReturnsAsync(candidate);
        candidates.Setup(repository => repository.ReplaceAsync(
                candidate,
                expectedCandidateVersion,
                It.IsAny<TripChildMutationLease>(),
                CancellationToken.None))
            .ReturnsAsync((TripParkCandidate persisted, long _, TripChildMutationLease _, CancellationToken _) =>
                new TripParkCandidateWriteResult(TripChildWriteOutcome.Success, persisted));
        Mock<ITripDayPlanRepository> days = new Mock<ITripDayPlanRepository>(MockBehavior.Strict);
        days.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripDayPlan>());
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(candidate.ParkId, true, CancellationToken.None))
            .ReturnsAsync(new Park { Id = candidate.ParkId, Name = "Parc test", IsVisible = true });
        Mock<ITripChildMutationLeaseRepository> leases =
            new Mock<ITripChildMutationLeaseRepository>(MockBehavior.Strict);
        TripChildMutationLease lease = new TripChildMutationLease(
            "candidate-update-operation",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddMinutes(5));
        leases.Setup(repository => repository.TryAcquireAccessibleAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(lease);
        leases.Setup(repository => repository.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<ITripAuditWriter> writer = new Mock<ITripAuditWriter>(MockBehavior.Strict);
        writer.Setup(port => port.AppendAsync(
                It.Is<TripActivityWrite>(activity =>
                    activity.TripPlanId == trip.Id
                    && activity.ActorMemberId == owner.Id
                    && activity.Kind == TripActivityKind.CandidateUpdated
                    && activity.OperationKey == "candidate-update:candidate-update-operation"),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<TimeProvider> clock = new Mock<TimeProvider>(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc.AddMinutes(1)));
        TripCandidateMutationService service = new TripCandidateMutationService(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            clock.Object,
            new TripActivityRecorder(writer.Object, clock.Object));

        AmusementPark.Application.Errors.ApplicationResult<TripParkCandidateResult> result =
            await service.UpdateAsync(
                trip.OwnerUserId,
                trip.Id.Value,
                trip.Version,
                candidate.Id.Value,
                expectedCandidateVersion,
                new TripParkCandidateDetailsInput(new[] { localDate }, "À faire en priorité"),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("À faire en priorité", result.Value!.CollectiveNote);
        trips.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        parks.VerifyAll();
        leases.VerifyAll();
        writer.VerifyAll();
        clock.VerifyAll();
    }
}
