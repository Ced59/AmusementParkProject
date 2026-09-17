using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripProgramResultFactoryTests
{
    [Fact]
    public async Task BuildAsync_ShouldHydrateParkNamesInOneBatchWithoutExposingAnIdAsALabel()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlanId tripId = TripPlanId.New();
        TripParkCandidate candidate = TripParkCandidate.Create(
            TripParkCandidateId.New(),
            tripId,
            "park-1",
            Array.Empty<DateOnly>(),
            TripParkCandidateSource.Manual,
            null,
            null,
            TripMemberId.New(),
            1024,
            nowUtc);
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.SetupSequence(item => item.GetProgramReadSequenceAsync(
                tripId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(4)
            .ReturnsAsync(4);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(item => item.ListAsync(tripId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { candidate });
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        days.Setup(item => item.ListAsync(tripId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TripDayPlan>());
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(item => item.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Phantasialand",
                },
            });
        TripProgramResultFactory factory = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object);

        AmusementPark.Application.Errors.ApplicationResult<TripProgramResult> result =
            await factory.BuildAsync(tripId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        TripParkCandidateResult candidateResult = Assert.Single(result.Value.Candidates);
        Assert.Equal("Phantasialand", candidateResult.ParkName);
        Assert.True(candidateResult.IsParkAvailable);
        Assert.NotEqual(candidateResult.ParkId, candidateResult.ParkName);
        trips.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        parks.VerifyAll();
    }

    [Fact]
    public async Task BuildAsync_WhenAChildMutationStartsDuringTheRead_ShouldRetryTheWholeSnapshot()
    {
        DateTime nowUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);
        TripPlanId tripId = TripPlanId.New();
        TripParkCandidate candidate = TripParkCandidate.Create(
            TripParkCandidateId.New(),
            tripId,
            "park-1",
            Array.Empty<DateOnly>(),
            TripParkCandidateSource.Manual,
            null,
            null,
            TripMemberId.New(),
            1024,
            nowUtc);
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.SetupSequence(item => item.GetProgramReadSequenceAsync(
                tripId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(4)
            .ReturnsAsync(5)
            .ReturnsAsync(5)
            .ReturnsAsync(5);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(item => item.ListAsync(tripId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { candidate });
        Mock<ITripDayPlanRepository> days = new(MockBehavior.Strict);
        days.Setup(item => item.ListAsync(tripId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TripDayPlan>());
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(item => item.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Parc test" } });
        TripProgramResultFactory factory = new(
            trips.Object,
            candidates.Object,
            days.Object,
            parks.Object);

        AmusementPark.Application.Errors.ApplicationResult<TripProgramResult> result =
            await factory.BuildAsync(tripId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Candidates);
        trips.VerifyAll();
        candidates.Verify(
            item => item.ListAsync(tripId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        days.Verify(
            item => item.ListAsync(tripId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        parks.VerifyAll();
    }

    [Fact]
    public void ToCandidateResult_WhenTheParkCannotBeHydrated_ShouldExposeANeutralUnavailableState()
    {
        TripParkCandidate candidate = TripParkCandidate.Create(
            TripParkCandidateId.New(),
            TripPlanId.New(),
            "missing-park",
            Array.Empty<DateOnly>(),
            TripParkCandidateSource.Manual,
            null,
            null,
            TripMemberId.New(),
            1024,
            new DateTime(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc));

        TripParkCandidateResult result = TripProgramResultFactory.ToCandidateResult(candidate, null);

        Assert.Null(result.ParkName);
        Assert.False(result.IsParkAvailable);
    }
}
