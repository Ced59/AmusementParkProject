using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripPlanDateServiceTests
{
    [Fact]
    public async Task SetDatesAsync_ShouldRejectAProposalThatInvalidatesTheExistingProgram()
    {
        DateTime nowUtc = new(2027, 1, 2, 10, 0, 0, DateTimeKind.Utc);
        DateOnly initialDate = new(2027, 7, 8);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.Fixed(initialDate),
            "Europe/Paris",
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate candidate = TripParkCandidate.Create(
            TripParkCandidateId.New(),
            trip.Id,
            "park-1",
            new[] { initialDate },
            TripParkCandidateSource.Manual,
            null,
            null,
            owner.Id,
            TripParkCandidate.SortPositionStep,
            nowUtc);
        candidate.ChangeState(TripParkCandidateState.Selected, nowUtc.AddMinutes(1));
        TripDayPlan day = TripDayPlan.Create(
            TripDayPlanId.New(),
            trip.Id,
            initialDate,
            candidate.Id,
            candidate.ParkId,
            null,
            null,
            Array.Empty<TripDayBlock>(),
            nowUtc.AddMinutes(2));
        TripChildMutationLease lease = new(
            "operation-1",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddHours(1));
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        Mock<ITripTimeZoneValidator> timeZones = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        trips.Setup(item => item.GetAccessibleAsync("user-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        candidates.Setup(item => item.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        days.Setup(item => item.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { day });
        leases.Setup(item => item.TryAcquireAccessibleAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(lease);
        leases.Setup(item => item.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .Returns(Task.CompletedTask);
        TripPlanDateService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            timeZones.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance));

        ApplicationResult<TripPlanResult> result = await service.SetDatesAsync(
            "user-1",
            trip.Id.Value,
            trip.Version,
            new TripPlanDatesInput(TripDateProposal.Fixed(new DateOnly(2027, 7, 9)), null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(TripPlanErrorCodes.InvalidCandidate, Assert.Single(result.Errors).Code);
        trips.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        timeZones.VerifyAll();
        leases.VerifyAll();
    }

    [Fact]
    public async Task SetDatesAsync_ShouldAdvanceTheRootUnderTheAcquiredChildLease()
    {
        DateTime nowUtc = new(2027, 1, 2, 10, 0, 0, DateTimeKind.Utc);
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
            nowUtc.AddHours(1));
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        Mock<ITripTimeZoneValidator> timeZones = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        timeZones.Setup(item => item.IsValidIanaTimeZone("Europe/Paris")).Returns(true);
        trips.Setup(item => item.GetAccessibleAsync("user-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        candidates.Setup(item => item.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripParkCandidate>());
        days.Setup(item => item.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripDayPlan>());
        leases.Setup(item => item.TryAcquireAccessibleAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(lease);
        leases.Setup(item => item.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .Returns(Task.CompletedTask);
        clock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(nowUtc.AddMinutes(1)));
        trips.Setup(item => item.ReplaceAccessibleUnderChildLeaseAsync(
                "user-1",
                It.Is<TripPlan>(value => value.ChildMutationEpoch == 2 && value.Version == 2),
                1,
                lease,
                CancellationToken.None))
            .ReturnsAsync((string _, TripPlan value, long _, TripChildMutationLease _, CancellationToken _) =>
                new TripPlanWriteResult(TripPlanWriteOutcome.Success, value.Version, value));
        TripPlanDateService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            timeZones.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            clock.Object);

        ApplicationResult<TripPlanResult> result = await service.SetDatesAsync(
            "user-1",
            trip.Id.Value,
            trip.Version,
            new TripPlanDatesInput(
                TripDateProposal.Fixed(new DateOnly(2027, 7, 9)),
                "Europe/Paris"),
            CancellationToken.None);

        Assert.True(
            result.IsSuccess,
            string.Join(" | ", result.Errors.Select(static error => $"{error.Code}: {error.Message}")));
        Assert.Equal(2, result.Value?.Version);
        trips.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        timeZones.VerifyAll();
        leases.VerifyAll();
        clock.VerifyAll();
    }
}
