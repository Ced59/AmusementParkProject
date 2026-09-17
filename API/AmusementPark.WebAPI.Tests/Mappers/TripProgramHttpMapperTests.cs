using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripProgramHttpMapperTests
{
    [Fact]
    public void TryToApplication_ShouldParseCandidateDatesAndSourceStrictly()
    {
        AddTripParkCandidateRequestDto request = new()
        {
            ParkId = "park-1",
            CandidateDates = new[] { "2027-07-08" },
            Source = "Wishlist",
            CollectiveNote = "Priorité",
        };

        bool success = request.TryToApplication(out TripParkCandidateInput? input);

        Assert.True(success);
        Assert.NotNull(input);
        Assert.Equal(TripParkCandidateSource.Wishlist, input.Source);
        Assert.Equal(new DateOnly(2027, 7, 8), Assert.Single(input.CandidateDates));
    }

    [Fact]
    public void TryToApplication_ShouldRejectAnInvalidBlockTime()
    {
        PutTripDayPlanRequestDto request = new()
        {
            ParkCandidateId = "candidate-1",
            Blocks = new[]
            {
                new TripDayBlockRequestDto
                {
                    BlockId = Guid.NewGuid().ToString(),
                    Type = "Meal",
                    Title = "Déjeuner",
                    LocalTime = "25:10",
                },
            },
        };

        bool success = request.TryToApplication(out TripDayPlanInput? input);

        Assert.False(success);
        Assert.Null(input);
    }

    [Fact]
    public void TryToApplication_ShouldRejectADayBlockWithoutAStableIdentifier()
    {
        PutTripDayPlanRequestDto request = new()
        {
            ParkCandidateId = "candidate-1",
            Blocks = new[]
            {
                new TripDayBlockRequestDto
                {
                    Type = "Meal",
                    Title = "Déjeuner",
                    LocalTime = "12:30",
                },
            },
        };

        bool success = request.TryToApplication(out TripDayPlanInput? input);

        Assert.False(success);
        Assert.Null(input);
    }

    [Fact]
    public void TryToApplication_ShouldRejectANullBlockCollection()
    {
        PutTripDayPlanRequestDto request = new()
        {
            ParkCandidateId = "candidate-1",
            Blocks = null!,
        };

        bool success = request.TryToApplication(out TripDayPlanInput? input);

        Assert.False(success);
        Assert.Null(input);
    }

    [Fact]
    public void TryToApplication_ShouldRejectANullBlockEntry()
    {
        PutTripDayPlanRequestDto request = new()
        {
            ParkCandidateId = "candidate-1",
            Blocks = new TripDayBlockRequestDto[] { null! },
        };

        bool success = request.TryToApplication(out TripDayPlanInput? input);

        Assert.False(success);
        Assert.Null(input);
    }

    [Fact]
    public void ToHttp_ShouldKeepTheHydratedParkName()
    {
        TripParkCandidateResult result = new(
            "candidate-1",
            "park-technical-id",
            "Europa-Park",
            Array.Empty<DateOnly>(),
            TripParkCandidateSource.Manual,
            TripParkCandidateState.Proposed,
            null,
            null,
            1024,
            1,
            DateTime.UtcNow,
            DateTime.UtcNow);

        TripParkCandidateDto dto = result.ToHttp();

        Assert.Equal("Europa-Park", dto.ParkName);
        Assert.NotEqual(dto.ParkId, dto.ParkName);
    }
}
