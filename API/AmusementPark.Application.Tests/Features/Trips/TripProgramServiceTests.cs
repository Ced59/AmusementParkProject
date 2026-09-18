using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripProgramServiceTests
{
    [Fact]
    public async Task DeleteAsync_ShouldRemoveTheRequestedDayUnderTheTripLease()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        DateOnly localDate = new(2027, 7, 8);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage",
            TripDateProposal.Fixed(localDate),
            "Europe/Paris",
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.Setup(item => item.GetAccessibleAsync("owner-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        TripChildMutationLease lease = new(
            "delete-day",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddMinutes(5));
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
        days.Setup(item => item.DeleteAsync(
                trip.Id,
                localDate,
                3,
                lease,
                CancellationToken.None))
            .ReturnsAsync(new TripDayPlanWriteResult(TripChildWriteOutcome.Success));
        TripDayProgramService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance));

        AmusementPark.Application.Errors.ApplicationResult result = await service.DeleteAsync(
            "owner-1",
            trip.Id.Value,
            trip.Version,
            localDate,
            3,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        trips.VerifyAll();
        days.VerifyAll();
        leases.VerifyAll();
        candidates.VerifyNoOtherCalls();
        parks.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PutDayAsync_WhenAnIdenticalCreateIsRetried_ShouldReplayWithoutASecondWrite()
    {
        DateOnly localDate = new(2027, 7, 8);
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
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
        candidate.ChangeState(TripParkCandidateState.Selected, nowUtc.AddMinutes(1));
        TripDayBlockId blockId = TripDayBlockId.New();
        TripDayBlock block = new(
            blockId,
            TripDayBlockType.Meal,
            "Déjeuner",
            null,
            new TimeOnly(12, 30),
            TripDayBlock.SortPositionStep);
        TripDayPlan existing = TripDayPlan.Create(
            TripDayPlanId.New(),
            trip.Id,
            localDate,
            candidate.Id,
            candidate.ParkId,
            new TimeOnly(9, 0),
            "Rendez-vous à l'entrée",
            new[] { block },
            nowUtc.AddMinutes(2));
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.Setup(item => item.GetAccessibleAsync("owner-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(item => item.GetAsync(trip.Id, candidate.Id, CancellationToken.None))
            .ReturnsAsync(candidate);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        days.Setup(item => item.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { existing });
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(item => item.GetByIdAsync("park-1", true, CancellationToken.None))
            .ReturnsAsync(new Park
            {
                Id = "park-1",
                Name = "Parc test",
                IsVisible = true,
            });
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        TripChildMutationLease lease = new(
            "put-day",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddMinutes(5));
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
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(nowUtc.AddMinutes(3)));
        TripDayProgramService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            clock.Object);
        TripDayPlanInput input = new(
            candidate.Id.Value,
            new TimeOnly(9, 0),
            "Rendez-vous à l'entrée",
            new[]
            {
                new TripDayBlockInput(
                    blockId.Value,
                    TripDayBlockType.Meal,
                    "Déjeuner",
                    null,
                    new TimeOnly(12, 30),
                    TripDayBlock.SortPositionStep),
            });

        AmusementPark.Application.Errors.ApplicationResult<TripDayPlanResult> result =
            await service.PutAsync(
                "owner-1",
                trip.Id.Value,
                trip.Version,
                localDate,
                null,
                input,
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(existing.Version, result.Value.Version);
        Assert.Equal("Parc test", result.Value.ParkName);
        days.Verify(item => item.PutAsync(
            It.IsAny<TripDayPlan>(),
            It.IsAny<long?>(),
            It.IsAny<TripChildMutationLease>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        trips.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        parks.VerifyAll();
        leases.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task PutDayAsync_WhenAStaleRetryContainsDifferentContent_ShouldReportThePersistedVersion()
    {
        DateOnly localDate = new(2027, 7, 8);
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
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
        candidate.ChangeState(TripParkCandidateState.Selected, nowUtc.AddMinutes(1));
        TripDayPlan existing = TripDayPlan.Create(
            TripDayPlanId.New(),
            trip.Id,
            localDate,
            candidate.Id,
            candidate.ParkId,
            new TimeOnly(9, 0),
            "Version initiale",
            Array.Empty<TripDayBlock>(),
            nowUtc.AddMinutes(2));
        existing.Update(
            candidate.Id,
            candidate.ParkId,
            new TimeOnly(10, 0),
            "Version persistée",
            Array.Empty<TripDayBlock>(),
            nowUtc.AddMinutes(3));
        long persistedVersion = existing.Version;
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.Setup(item => item.GetAccessibleAsync("owner-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(item => item.GetAsync(trip.Id, candidate.Id, CancellationToken.None))
            .ReturnsAsync(candidate);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        days.Setup(item => item.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { existing });
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        TripChildMutationLease lease = new(
            "put-day",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddMinutes(5));
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
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(nowUtc.AddMinutes(4)));
        TripDayProgramService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            clock.Object);

        AmusementPark.Application.Errors.ApplicationResult<TripDayPlanResult> result =
            await service.PutAsync(
                "owner-1",
                trip.Id.Value,
                trip.Version,
                localDate,
                persistedVersion - 1,
                new TripDayPlanInput(
                    candidate.Id.Value,
                    new TimeOnly(11, 0),
                    "Modification obsolète",
                    Array.Empty<TripDayBlockInput>()),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(persistedVersion, Assert.Single(result.Errors).CurrentVersion);
        days.Verify(item => item.PutAsync(
            It.IsAny<TripDayPlan>(),
            It.IsAny<long?>(),
            It.IsAny<TripChildMutationLease>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        trips.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        parks.VerifyNoOtherCalls();
        leases.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task AddCandidateAsync_WhenTheOperationKeyHasDifferentContent_ShouldReturnAConflict()
    {
        DateOnly localDate = new(2027, 7, 8);
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage",
            TripDateProposal.Fixed(localDate),
            "Europe/Paris",
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripChildMutationLease lease = new(
            "candidate-operation",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddMinutes(5));
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        candidates.Setup(item => item.ResolveCreationAsync(
                trip.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(new TripParkCandidateWriteResult(TripChildWriteOutcome.NotFound));
        trips.Setup(item => item.GetAccessibleAsync("owner-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        parks.Setup(item => item.GetByIdAsync("park-1", false, CancellationToken.None))
            .ReturnsAsync(new Park
            {
                Id = "park-1",
                Name = "Parc test",
                IsVisible = true,
            });
        candidates.Setup(item => item.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripParkCandidate>());
        candidates.Setup(item => item.CreateAsync(
                It.IsAny<TripParkCandidate>(),
                lease,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(new TripParkCandidateWriteResult(
                TripChildWriteOutcome.IdempotencyConflict));
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
        TripProgramService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            new TripProgramResultFactory(trips.Object, candidates.Object, days.Object, parks.Object),
            clock.Object);

        AmusementPark.Application.Errors.ApplicationResult<CreateTripParkCandidateResult> result =
            await service.AddCandidateAsync(
                "owner-1",
                trip.Id.Value,
                trip.Version,
                "reused-key",
                new TripParkCandidateInput(
                    "park-1",
                    new[] { localDate },
                    TripParkCandidateSource.Manual,
                    null),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.candidate.idempotency-conflict", Assert.Single(result.Errors).Code);
        trips.VerifyAll();
        candidates.VerifyAll();
        days.VerifyNoOtherCalls();
        parks.VerifyAll();
        leases.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task AddCandidateAsync_WhenACommittedOperationIsRetried_ShouldReplayBeforeMutableChecks()
    {
        DateOnly localDate = new(2027, 7, 8);
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage",
            TripDateProposal.Fixed(localDate),
            "Europe/Paris",
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate existing = TripParkCandidate.Create(
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
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        trips.Setup(item => item.GetAccessibleAsync("owner-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        candidates.Setup(item => item.ResolveCreationAsync(
                trip.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(new TripParkCandidateWriteResult(
                TripChildWriteOutcome.Success,
                existing,
                existing.Version,
                true));
        parks.Setup(item => item.GetByIdAsync(existing.ParkId, true, CancellationToken.None))
            .ReturnsAsync(new Park
            {
                Id = existing.ParkId,
                Name = "Parc masqué",
                IsVisible = false,
            });
        TripProgramService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            new TripProgramResultFactory(trips.Object, candidates.Object, days.Object, parks.Object));

        AmusementPark.Application.Errors.ApplicationResult<CreateTripParkCandidateResult> result =
            await service.AddCandidateAsync(
                "owner-1",
                trip.Id.Value,
                trip.Version + 10,
                "retry-key",
                new TripParkCandidateInput(
                    existing.ParkId,
                    new[] { localDate },
                    TripParkCandidateSource.Manual,
                    null),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.WasReplayed);
        Assert.Equal(existing.Id.Value, result.Value.Candidate.CandidateId);
        Assert.Null(result.Value.Candidate.ParkName);
        Assert.False(result.Value.Candidate.IsParkAvailable);
        candidates.VerifyAll();
        parks.VerifyAll();
        trips.VerifyAll();
        days.VerifyNoOtherCalls();
        leases.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddCandidateAsync_WhenTheOriginalCandidateWasDeleted_ShouldNotRecreateIt()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage",
            TripDateProposal.None(),
            "Europe/Paris",
            nowUtc);
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        trips.Setup(item => item.GetAccessibleAsync("owner-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        candidates.Setup(item => item.ResolveCreationAsync(
                trip.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(new TripParkCandidateWriteResult(TripChildWriteOutcome.Deleted));
        TripProgramService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            new TripProgramResultFactory(trips.Object, candidates.Object, days.Object, parks.Object));

        AmusementPark.Application.Errors.ApplicationResult<CreateTripParkCandidateResult> result =
            await service.AddCandidateAsync(
                "owner-1",
                trip.Id.Value,
                1,
                "deleted-key",
                new TripParkCandidateInput(
                    "park-1",
                    Array.Empty<DateOnly>(),
                    TripParkCandidateSource.Manual,
                    null),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.candidate.creation-deleted", Assert.Single(result.Errors).Code);
        candidates.VerifyAll();
        trips.VerifyAll();
        days.VerifyNoOtherCalls();
        parks.VerifyNoOtherCalls();
        leases.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddCandidateAsync_WhenAReplayBelongsToAnInactiveTrip_ShouldReturnNotFound()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage",
            TripDateProposal.None(),
            "Europe/Paris",
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate existing = TripParkCandidate.Create(
            TripParkCandidateId.New(),
            trip.Id,
            "park-1",
            Array.Empty<DateOnly>(),
            TripParkCandidateSource.Manual,
            null,
            null,
            owner.Id,
            TripParkCandidate.SortPositionStep,
            nowUtc);
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        candidates.Setup(item => item.ResolveCreationAsync(
                trip.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(new TripParkCandidateWriteResult(
                TripChildWriteOutcome.Success,
                existing,
                existing.Version,
                true));
        trips.Setup(item => item.GetAccessibleAsync("owner-1", trip.Id, CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        TripProgramService service = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            new TripProgramResultFactory(trips.Object, candidates.Object, days.Object, parks.Object));

        AmusementPark.Application.Errors.ApplicationResult<CreateTripParkCandidateResult> result =
            await service.AddCandidateAsync(
                "owner-1",
                trip.Id.Value,
                trip.Version,
                "retry-key",
                new TripParkCandidateInput(
                    existing.ParkId,
                    Array.Empty<DateOnly>(),
                    TripParkCandidateSource.Manual,
                    null),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.not-found", Assert.Single(result.Errors).Code);
        trips.VerifyAll();
        candidates.VerifyAll();
        days.VerifyNoOtherCalls();
        parks.VerifyNoOtherCalls();
        leases.VerifyNoOtherCalls();
    }
}
