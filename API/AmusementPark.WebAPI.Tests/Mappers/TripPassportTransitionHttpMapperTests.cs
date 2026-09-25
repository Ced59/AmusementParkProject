using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripPassportTransitionHttpMapperTests
{
    [Fact]
    public void ToHttp_ShouldExposeReadableNamesAndStableEnumValues()
    {
        TripPassportTransitionResult result = new(
            "Voyage test",
            new DateOnly(2027, 8, 22),
            new[]
            {
                new TripPassportTransitionDayResult(
                    new DateOnly(2027, 8, 20),
                    "park-1",
                    "Phantasialand",
                    true,
                    true,
                    false,
                    null,
                    null,
                    new[]
                    {
                        new TripPassportTransitionItemResult(
                            "item-1",
                            "Taron",
                            "image-1",
                            TripItemPreferenceLevel.MustDo,
                            HistoricalConsistency.Verified),
                    }),
            });

        TripPassportTransitionDto dto = result.ToHttp();

        Assert.Equal("Voyage test", dto.Title);
        TripPassportTransitionDayDto day = Assert.Single(dto.Days);
        Assert.Equal("Phantasialand", day.ParkName);
        Assert.False(day.CanResume);
        TripPassportTransitionItemDto item = Assert.Single(day.Attractions);
        Assert.Equal("Taron", item.Name);
        Assert.Equal("MustDo", item.OwnPreference);
        Assert.Equal("Verified", item.HistoricalConsistency);
    }

    [Fact]
    public void ToHttp_ShouldMapConfirmationReplayInformation()
    {
        ConfirmTripPassportTransitionResult result = new("visit-1", true, 3);

        ConfirmTripPassportTransitionDto dto = result.ToHttp();

        Assert.Equal("visit-1", dto.VisitId);
        Assert.True(dto.WasReplayed);
        Assert.Equal(3, dto.AddedRideCount);
    }
}
