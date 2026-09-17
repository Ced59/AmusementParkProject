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
        TripProgramResultFactory factory = new(candidates.Object, days.Object, parks.Object);

        TripProgramResult result = await factory.BuildAsync(tripId, CancellationToken.None);

        TripParkCandidateResult candidateResult = Assert.Single(result.Candidates);
        Assert.Equal("Phantasialand", candidateResult.ParkName);
        Assert.NotEqual(candidateResult.ParkId, candidateResult.ParkName);
        candidates.VerifyAll();
        days.VerifyAll();
        parks.VerifyAll();
    }
}
