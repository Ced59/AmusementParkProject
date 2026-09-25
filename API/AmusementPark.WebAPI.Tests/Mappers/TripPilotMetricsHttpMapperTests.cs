using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripPilotMetricsHttpMapperTests
{
    [Fact]
    public void ToHttp_ShouldPreserveAggregateMetrics()
    {
        DateTime generatedAtUtc = new DateTime(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);
        TripPilotMetricsResult result = new(
            generatedAtUtc,
            12,
            5,
            8,
            4,
            2,
            3,
            42,
            1,
            new Dictionary<string, long> { ["CandidateAdded"] = 9 });

        TripPilotMetricsDto dto = result.ToHttp();

        Assert.Equal(generatedAtUtc, dto.GeneratedAtUtc);
        Assert.Equal(2, dto.ExpiredInvitations);
        Assert.Equal(9, dto.ActivityCounts["CandidateAdded"]);
    }
}
