using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class YearRecapSourceReaderTests
{
    [Fact]
    public void ComputeFingerprint_ShouldBeOrderIndependentAndCoverSourceVersions()
    {
        YearRecapVisitSourceDocument firstVisit = CreateVisit("visit-a", 1);
        YearRecapVisitSourceDocument secondVisit = CreateVisit("visit-b", 1);
        YearRecapRideSourceDocument firstRide = CreateRide("ride-a", "visit-a", 1);
        YearRecapRideSourceDocument secondRide = CreateRide("ride-b", "visit-b", 1);

        string ordered = YearRecapSourceReader.ComputeFingerprint(
            new[] { firstVisit, secondVisit },
            new[] { firstRide, secondRide });
        string reversed = YearRecapSourceReader.ComputeFingerprint(
            new[] { secondVisit, firstVisit },
            new[] { secondRide, firstRide });
        string changed = YearRecapSourceReader.ComputeFingerprint(
            new[] { firstVisit, CreateVisit("visit-b", 2) },
            new[] { firstRide, secondRide });

        Assert.Equal(64, ordered.Length);
        Assert.Equal(ordered, reversed);
        Assert.NotEqual(ordered, changed);
    }

    private static YearRecapVisitSourceDocument CreateVisit(string id, long version)
    {
        return new YearRecapVisitSourceDocument
        {
            Id = id,
            ParkId = "park-1",
            Date = new VisitDateDocument
            {
                Year = 2026,
                Month = 7,
                Day = 12,
                Precision = VisitDatePrecision.Day,
                IsApproximate = false,
            },
            ParkAssessmentValueHalfSteps = 8,
            Version = version,
        };
    }

    private static YearRecapRideSourceDocument CreateRide(
        string id,
        string visitId,
        long version)
    {
        return new YearRecapRideSourceDocument
        {
            Id = id,
            VisitId = visitId,
            ParkId = "park-1",
            ParkItemId = "item-1",
            Status = RideOccurrenceStatus.Completed,
            AssessmentValueHalfSteps = 9,
            HistoricalName = "Attraction",
            HistoricalCategory = "Attraction",
            Version = version,
        };
    }
}
