using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class PassportRideOccurrenceHttpMappersTests
{
    [Fact]
    public void ToHttp_ShouldPreserveTheTripTransitionSource()
    {
        DateTime nowUtc = new(2026, 9, 25, 9, 0, 0, DateTimeKind.Utc);
        RideOccurrenceResult result = new(
            "ride-1",
            "visit-1",
            "park-1",
            "item-1",
            1024,
            new RideOccurrenceMomentResult(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.TripTransition,
            HistoricalConsistency.Verified,
            null,
            true,
            1,
            nowUtc,
            nowUtc);

        PassportRideOccurrenceDto dto = result.ToHttp();

        Assert.Equal(PassportRideLogSourceDto.TripTransition, dto.Source);
        Assert.Equal((int)RideLogSource.TripTransition, (int)dto.Source);
    }
}
