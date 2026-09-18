using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripPreferenceSummaryHttpMapperTests
{
    [Fact]
    public void ToHttp_ShouldExposeAggregatesAndDecisionWithoutTechnicalAuthorId()
    {
        DateTime nowUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPreferenceSummaryResult result = new(
            "trip-1",
            "Voyage test",
            7,
            4,
            true,
            new[]
            {
                new TripItemPreferenceSummaryResult(
                    "park-1",
                    "Parc test",
                    "item-1",
                    "Grand huit",
                    "image-1",
                    2,
                    1,
                    0,
                    1,
                    0,
                    TripPreferenceCompatibility.Conflict,
                    true,
                    true,
                    false,
                    "Operating",
                    "https://example.com/ride",
                    nowUtc,
                    new TripItemDecisionResult(
                        TripItemDecisionStatus.SplitGroup,
                        "Le groupe se retrouve ensuite.",
                        "CapitaineParc",
                        nowUtc,
                        2)),
            });

        TripPreferenceSummaryDto dto = result.ToHttp();

        TripItemPreferenceSummaryDto item = Assert.Single(dto.Items);
        Assert.Equal("Conflict", item.Compatibility);
        Assert.Equal(nowUtc, item.OfficialStatusVerifiedAtUtc);
        Assert.Equal("SplitGroup", item.Decision!.Status);
        Assert.Equal("CapitaineParc", item.Decision.DecidedByDisplayName);
    }

    [Fact]
    public void TryToApplication_ShouldRejectUnknownDecisionStatus()
    {
        SetTripItemDecisionRequestDto request = new()
        {
            ExpectedPlanVersion = 7,
            Status = "AutomaticMajority",
            Reason = "A reason",
        };

        bool parsed = request.TryToApplication("item-1", out _);

        Assert.False(parsed);
    }
}
