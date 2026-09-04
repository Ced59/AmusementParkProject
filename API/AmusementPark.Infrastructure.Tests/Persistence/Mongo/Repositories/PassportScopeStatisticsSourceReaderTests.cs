using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportScopeStatisticsSourceReaderTests
{
    [Fact]
    public void BuildParkVisitFilter_ShouldUseOwnerParkIndexAndExcludeArchivedVisits()
    {
        BsonDocument rendered = Render(
            PassportScopeStatisticsMongoDefinitions.BuildParkVisitFilter(
                " owner-1 ",
                " park-1 "));

        Assert.Equal("owner-1", rendered["userId"].AsString);
        Assert.Equal("park-1", rendered["parkId"].AsString);
        Assert.Equal(VisitStatus.Archived.ToString(), rendered["status"]["$ne"].AsString);
    }

    [Fact]
    public void BuildYearVisitFilter_ShouldUseOwnerYearIndexAndExcludeArchivedVisits()
    {
        BsonDocument rendered = Render(
            PassportScopeStatisticsMongoDefinitions.BuildYearVisitFilter(" owner-1 ", 2025));

        Assert.Equal("owner-1", rendered["userId"].AsString);
        Assert.Equal(2025, rendered["date.year"].AsInt32);
        Assert.Equal(VisitStatus.Archived.ToString(), rendered["status"]["$ne"].AsString);
    }

    [Fact]
    public void BuildOccurrenceFilter_ShouldRemainOwnerBoundAndHideDeletedOrPendingRows()
    {
        BsonDocument rendered = Render(
            PassportScopeStatisticsMongoDefinitions.BuildOccurrenceFilter(
                " owner-1 ",
                new[] { "visit-1", "visit-2" }));

        Assert.Equal("owner-1", rendered["userId"].AsString);
        Assert.Equal(
            new[] { "visit-1", "visit-2" },
            rendered["visitId"]["$in"].AsBsonArray.Select(static value => value.AsString));
        Assert.True(rendered["deletedAtUtc"].IsBsonNull);
        Assert.True(rendered["creationPendingCompletion"]["$ne"].AsBoolean);
    }

    [Fact]
    public void BuildParkRatingFilter_ShouldUseOwnerParkIndexAndPersistedSupportedTargets()
    {
        BsonDocument rendered = Render(
            PassportScopeStatisticsMongoDefinitions.BuildParkRatingFilter(
                " owner-1 ",
                " park-1 "));
        string json = rendered.ToJson();

        Assert.Equal("owner-1", rendered["userId"].AsString);
        Assert.Equal("park-1", rendered["parkId"].AsString);
        Assert.True(rendered["isMutationPlaceholder"]["$ne"].AsBoolean);
        Assert.Contains(RatingTargetType.Park.ToString(), json, StringComparison.Ordinal);
        Assert.Contains(RatingTargetType.ParkItem.ToString(), json, StringComparison.Ordinal);
        Assert.Contains(ParkItemCategory.Attraction.ToString(), json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildParkSource_ShouldRespectFenceCategoryFallbackAndCurrentRatings()
    {
        PassportScopeVisitSourceDocument[] visits =
        {
            Visit("visit-1", "park-1", 7, 7, true, 8),
            Visit("visit-2", "park-1", 4, 3, false, null),
        };
        PassportScopeOccurrenceSourceDocument[] occurrences =
        {
            Ride("occ-1", "visit-1", "park-1", "item-1", 7, "Historic", 10),
            Ride("occ-2", "visit-2", "park-1", "item-2", 3, null, null),
            Ride("occ-fenced", "visit-1", "park-1", "item-3", 6, null, 6),
            Ride("occ-wrong-park", "visit-1", "park-2", "item-4", 7, null, 8),
        };

        AmusementPark.Application.Features.Passport.Ports.PassportParkStatisticsSource result =
            PassportScopeStatisticsSourceReader.BuildParkSource(
                "park-1",
                visits,
                occurrences,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["item-1"] = "Attraction",
                    ["item-2"] = "Show",
                },
                new[]
                {
                    new PassportScopeRatingSourceDocument
                    {
                        TargetType = RatingTargetType.Park,
                        TargetId = "park-1",
                        Value = 4.5d,
                    },
                    new PassportScopeRatingSourceDocument
                    {
                        TargetType = RatingTargetType.ParkItem,
                        TargetId = "item-1",
                        Value = 4d,
                    },
                });

        Assert.Equal(2, result.Visits.Count);
        Assert.Equal(2, result.Rides.Count);
        Assert.Equal(4.5d, result.CurrentGlobalRating?.DoubleValue);
        Assert.Equal("item-1", Assert.Single(result.CurrentItemRatings).ParkItemId);
        PassportRideStatisticsObservation historical = Assert.Single(
            result.Rides,
            static ride => ride.ParkItemId == "item-1");
        Assert.Equal("Historic", historical.HistoricalCategory);
        Assert.Equal("Attraction", historical.CurrentCategory);
        PassportRideStatisticsObservation current = Assert.Single(
            result.Rides,
            static ride => ride.ParkItemId == "item-2");
        Assert.Null(current.HistoricalCategory);
        Assert.Equal("Show", current.CurrentCategory);
    }

    private static PassportScopeVisitSourceDocument Visit(
        string id,
        string parkId,
        long currentFence,
        long stableFence,
        bool ready,
        byte? ratingHalfSteps)
    {
        return new PassportScopeVisitSourceDocument
        {
            Id = id,
            ParkId = parkId,
            Date = new VisitDateDocument
            {
                Year = 2025,
                Precision = VisitDatePrecision.Year,
            },
            ParkAssessmentValueHalfSteps = ratingHalfSteps,
            ContentMutationFenceToken = currentFence,
            ContentMutationFenceStableToken = stableFence,
            ContentMutationFenceReady = ready,
        };
    }

    private static PassportScopeOccurrenceSourceDocument Ride(
        string id,
        string visitId,
        string parkId,
        string parkItemId,
        long fence,
        string? historicalCategory,
        byte? ratingHalfSteps)
    {
        return new PassportScopeOccurrenceSourceDocument
        {
            Id = id,
            VisitId = visitId,
            ParkId = parkId,
            ParkItemId = parkItemId,
            Status = RideOccurrenceStatus.Completed,
            ContentMutationFenceToken = fence,
            HistoricalCategory = historicalCategory,
            AssessmentValueHalfSteps = ratingHalfSteps,
        };
    }

    private static BsonDocument Render(FilterDefinition<UserVisitDocument> filter)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        return filter.Render(new RenderArgs<UserVisitDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(
        FilterDefinition<UserRideOccurrenceDocument> filter)
    {
        IBsonSerializer<UserRideOccurrenceDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserRideOccurrenceDocument>();
        return filter.Render(new RenderArgs<UserRideOccurrenceDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(FilterDefinition<UserRatingDocument> filter)
    {
        IBsonSerializer<UserRatingDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserRatingDocument>();
        return filter.Render(new RenderArgs<UserRatingDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
