using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class TripActivityHttpMapperTests
{
    [Fact]
    public void ToHttp_ShouldExposeReadableEvidenceWithoutTechnicalIdentifiers()
    {
        TripActivityPageResult result = new TripActivityPageResult(
            "Voyage test",
            new[]
            {
                new TripActivityEntryResult(
                    8,
                    TripActivityKind.PreferencesUpdated,
                    "Camille",
                    true,
                    12,
                    new DateTime(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc)),
            },
            8);

        TripActivityPageDto dto = result.ToHttp();

        TripActivityEntryDto entry = Assert.Single(dto.Entries);
        Assert.Equal("PreferencesUpdated", entry.Kind);
        Assert.Equal("Camille", entry.ActorDisplayName);
        Assert.DoesNotContain(
            dto.GetType().GetProperties(),
            property => property.Name.EndsWith("Id", StringComparison.Ordinal));
        Assert.DoesNotContain(
            entry.GetType().GetProperties(),
            property => property.Name.EndsWith("Id", StringComparison.Ordinal));
    }
}
