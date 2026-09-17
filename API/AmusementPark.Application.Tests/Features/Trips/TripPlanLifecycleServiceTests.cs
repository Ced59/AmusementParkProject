using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripPlanLifecycleServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldReturnTheStableCreationSnapshot()
    {
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        timeZoneValidator.Setup(validator => validator.IsValidIanaTimeZone("Europe/Paris")).Returns(true);
        repository.Setup(item => item.ResolveExistingCreationAsync(
                It.IsAny<TripPlan>(),
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotentTripPlanCreationResult?)null);
        repository.Setup(item => item.CreateIdempotentAsync(
                It.IsAny<TripPlan>(),
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TripPlan trip, string _, CancellationToken _) =>
                new IdempotentTripPlanCreationResult(IdempotentTripPlanCreationStatus.Created, trip));
        TripPlanLifecycleService service = new(repository.Object, timeZoneValidator.Object);

        ApplicationResult<CreateTripPlanResult> result = await service.CreateAsync(
            "user-1",
            "operation-1",
            new TripPlanDetailsInput(
                "Voyage été",
                TripDateProposal.Fixed(new DateOnly(2027, 7, 8)),
                "Europe/Paris"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.WasReplayed);
        Assert.True(result.Value.TripPlan.IsOwner);
        Assert.Equal(1, result.Value.TripPlan.MemberCount);
        repository.VerifyAll();
        timeZoneValidator.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectAReusedKeyWithDifferentPayload()
    {
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        repository.Setup(item => item.ResolveExistingCreationAsync(
                It.IsAny<TripPlan>(),
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotentTripPlanCreationResult(
                IdempotentTripPlanCreationStatus.Conflict,
                null));
        TripPlanLifecycleService service = new(repository.Object, timeZoneValidator.Object);

        ApplicationResult<CreateTripPlanResult> result = await service.CreateAsync(
            "user-1",
            "operation-1",
            new TripPlanDetailsInput("Voyage", TripDateProposal.None(), null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.idempotency-conflict", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_ShouldReplayBeforeRevalidatingTheStoredTimeZone()
    {
        DateTime createdAtUtc = new(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        TripPlan existingTrip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.Fixed(new DateOnly(2027, 7, 8)),
            "Europe/Legacy",
            createdAtUtc);
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.ResolveExistingCreationAsync(
                It.Is<TripPlan>(trip => trip.DestinationTimeZoneId == "Europe/Legacy"),
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotentTripPlanCreationResult(
                IdempotentTripPlanCreationStatus.Replayed,
                existingTrip));
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        TripPlanLifecycleService service = new(repository.Object, timeZoneValidator.Object);

        ApplicationResult<CreateTripPlanResult> result = await service.CreateAsync(
            "user-1",
            "operation-1",
            new TripPlanDetailsInput(
                "Voyage",
                TripDateProposal.Fixed(new DateOnly(2027, 7, 8)),
                "Europe/Legacy"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.WasReplayed);
        Assert.Equal(existingTrip.Id.Value, result.Value.TripPlan.TripPlanId);
        repository.VerifyAll();
        timeZoneValidator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_WhenTheOriginalTripWasDeleted_ShouldRejectTheLateRetry()
    {
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        repository.Setup(item => item.ResolveExistingCreationAsync(
                It.IsAny<TripPlan>(),
                "operation-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotentTripPlanCreationResult(
                IdempotentTripPlanCreationStatus.Deleted,
                null));
        TripPlanLifecycleService service = new(repository.Object, timeZoneValidator.Object);

        ApplicationResult<CreateTripPlanResult> result = await service.CreateAsync(
            "user-1",
            "operation-1",
            new TripPlanDetailsInput("Voyage", TripDateProposal.None(), null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.creation-deleted", Assert.Single(result.Errors).Code);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RenameAsync_ShouldRejectAStaleVersionWithoutWriting()
    {
        DateTime nowUtc = DateTime.UtcNow.AddMinutes(-1);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            nowUtc);
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.GetOwnedAsync(
                "user-1",
                trip.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        TripPlanLifecycleService service = new(repository.Object, timeZoneValidator.Object);

        ApplicationResult<TripPlanResult> result = await service.RenameAsync(
            "user-1",
            trip.Id.Value,
            2,
            "Nouveau titre",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("trip.plan.changed-concurrently", error.Code);
        Assert.Equal(1, error.CurrentVersion);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RenameAsync_WhenTheAtomicWriteLosesARace_ShouldReturnTheStoredCurrentVersion()
    {
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            DateTime.UtcNow.AddMinutes(-1));
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.GetOwnedAsync(
                "user-1",
                trip.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);
        repository.Setup(item => item.ReplaceOwnedAsync(
                It.Is<TripPlan>(candidate => candidate.Version == 2),
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TripPlanWriteResult(TripPlanWriteOutcome.Conflict, 4));
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        TripPlanLifecycleService service = new(repository.Object, timeZoneValidator.Object);

        ApplicationResult<TripPlanResult> result = await service.RenameAsync(
            "user-1",
            trip.Id.Value,
            1,
            "Nouveau titre",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("trip.plan.changed-concurrently", error.Code);
        Assert.Equal(4, error.CurrentVersion);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RenameAsync_ShouldReturnTheTimestampPersistedByTheRepository()
    {
        DateTime createdAtUtc = new(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        DateTime requestedAtUtc = createdAtUtc.AddMinutes(1).AddTicks(4567);
        DateTime persistedAtUtc = new(
            requestedAtUtc.Ticks - (requestedAtUtc.Ticks % TimeSpan.TicksPerMillisecond),
            DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            createdAtUtc);
        TripPlan persistedTrip = TripPlan.Create(
            trip.Id,
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            createdAtUtc);
        persistedTrip.Rename("Nouveau titre", persistedAtUtc);
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.GetOwnedAsync(
                "user-1",
                trip.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);
        repository.Setup(item => item.ReplaceOwnedAsync(
                It.Is<TripPlan>(candidate => candidate.UpdatedAtUtc == requestedAtUtc),
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TripPlanWriteResult(
                TripPlanWriteOutcome.Success,
                persistedTrip.Version,
                persistedTrip));
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow())
            .Returns(new DateTimeOffset(requestedAtUtc));
        TripPlanLifecycleService service = new(
            repository.Object,
            timeZoneValidator.Object,
            timeProvider.Object);

        ApplicationResult<TripPlanResult> result = await service.RenameAsync(
            "user-1",
            trip.Id.Value,
            1,
            "Nouveau titre",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(persistedAtUtc, result.Value.UpdatedAtUtc);
        Assert.NotEqual(requestedAtUtc, result.Value.UpdatedAtUtc);
        repository.VerifyAll();
        timeProvider.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_ShouldPersistAClosedPendingDeletionAtTheNextVersion()
    {
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage",
            TripDateProposal.None(),
            null,
            DateTime.UtcNow.AddMinutes(-1));
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.GetOwnedAsync(
                "user-1",
                trip.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);
        repository.Setup(item => item.DeleteOwnedAsync(
                It.Is<TripPlan>(candidate =>
                    candidate.DeletionState == TripDeletionState.Pending
                    && candidate.AdmissionClosureState == TripAdmissionClosureState.Closing
                    && candidate.Version == 2),
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TripPlanWriteResult(TripPlanWriteOutcome.Success, 2));
        repository.Setup(item => item.PurgeChildrenAsync(
                trip.Id,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(item => item.FinalizeDeletionOwnedAsync(
                It.Is<TripPlan>(candidate => candidate.DeletionState == TripDeletionState.Pending),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TripPlanWriteResult(TripPlanWriteOutcome.Success, 2));
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        TripPlanLifecycleService service = new(repository.Object, timeZoneValidator.Object);

        ApplicationResult result = await service.DeleteAsync(
            "user-1",
            trip.Id.Value,
            1,
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_WhenAuthenticationIsNotRecent_ShouldRejectBeforeReadingTheTrip()
    {
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        Mock<ITripTimeZoneValidator> timeZoneValidator = new(MockBehavior.Strict);
        TripPlanLifecycleService service = new(repository.Object, timeZoneValidator.Object);

        ApplicationResult result = await service.DeleteAsync(
            "user-1",
            TripPlanId.New().Value,
            1,
            DateTime.UtcNow.Subtract(TripPlanLifecycleService.MaximumRecentAuthenticationAge).AddSeconds(-1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.recent-authentication-required", Assert.Single(result.Errors).Code);
        repository.VerifyNoOtherCalls();
    }
}
