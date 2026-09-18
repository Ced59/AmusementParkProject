using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripProgramCoherenceHttpMapperTests
{
    [Fact]
    public void ToHttp_ShouldKeepReadableNamesAndSerializeBusinessEnumsAsStrings()
    {
        DateTime nowUtc = new DateTime(2027, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateOnly date = new DateOnly(2027, 5, 3);
        TripProgramCoherenceResult result = new TripProgramCoherenceResult(
            "trip-technical-id",
            "Voyage test",
            2,
            nowUtc,
            1,
            0,
            0,
            new[]
            {
                new TripProgramDayEvidenceResult(
                    date,
                    "park-technical-id",
                    "Parc lisible",
                    true,
                    "Operating",
                    TripProgramOpeningState.Closed,
                    "https://example.com/hours",
                    nowUtc,
                    nowUtc),
            },
            Array.Empty<TripProgramTravelSegmentResult>(),
            new[]
            {
                new TripProgramCoherenceIssueResult(
                    TripProgramCoherenceCode.OpeningHoursClosed,
                    TripProgramCoherenceSeverity.Critical,
                    date,
                    "park-technical-id",
                    "Parc lisible",
                    null,
                    null,
                    null,
                    "https://example.com/hours",
                    nowUtc),
            });

        TripProgramCoherenceDto dto = result.ToHttp();

        TripProgramDayEvidenceDto day = Assert.Single(dto.Days);
        TripProgramCoherenceIssueDto issue = Assert.Single(dto.Issues);
        Assert.Equal("Parc lisible", day.ParkName);
        Assert.NotEqual(day.ParkId, day.ParkName);
        Assert.Equal("Closed", day.OpeningState);
        Assert.Equal("OpeningHoursClosed", issue.Code);
        Assert.Equal("Critical", issue.Severity);
    }
}
