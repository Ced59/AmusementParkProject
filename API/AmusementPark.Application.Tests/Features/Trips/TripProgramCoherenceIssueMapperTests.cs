using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripProgramCoherenceIssueMapperTests
{
    [Fact]
    public void Map_WhenAnAttractionIsNoLongerPublic_ShouldNotExposeItsPrivateMetadata()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Parc visible",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = park.Id,
            Name = "Nom retiré du catalogue",
            Category = ParkItemCategory.Attraction,
            IsVisible = false,
            AttractionDetails = new AttractionDetails
            {
                Status = "TemporarilyClosed",
                SourceUrl = "https://internal.example.com/source",
            },
        };
        TripProgramCoherenceIssue issue = new TripProgramCoherenceIssue(
            TripProgramCoherenceCode.AttractionUnavailable,
            TripProgramCoherenceSeverity.Critical,
            null,
            park.Id,
            item.Id);

        IReadOnlyCollection<TripProgramCoherenceIssueResult> mapped = new TripProgramCoherenceIssueMapper().Map(
            new[] { issue },
            new Dictionary<string, Park>(StringComparer.Ordinal) { [park.Id] = park },
            new Dictionary<string, ParkItem>(StringComparer.Ordinal) { [item.Id] = item },
            new Dictionary<string, ParkOpeningHoursSchedule>(StringComparer.Ordinal));

        TripProgramCoherenceIssueResult result = Assert.Single(mapped);
        Assert.Equal(park.Name, result.ParkName);
        Assert.Null(result.ParkItemName);
        Assert.Null(result.OfficialStatus);
        Assert.Null(result.OfficialSourceUrl);
    }
}
