using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportItemStatisticsSourceReaderTests
{
    [Fact]
    public void BuildOccurrenceFilter_ShouldUseTheIndexedOwnerItemScopeAndOnlyActiveCompletedRides()
    {
        FilterDefinition<UserRideOccurrenceDocument> filter =
            PassportItemStatisticsSourceReader.BuildOccurrenceFilter(
                " owner-1 ",
                " item-1 ");

        BsonDocument rendered = Render(filter);

        Assert.Equal("owner-1", rendered["userId"].AsString);
        Assert.Equal("item-1", rendered["parkItemId"].AsString);
        Assert.Equal(RideOccurrenceStatus.Completed.ToString(), rendered["status"].AsString);
        Assert.True(rendered["deletedAtUtc"].IsBsonNull);
        Assert.True(rendered["creationPendingCompletion"]["$ne"].AsBoolean);
    }

    [Fact]
    public void BuildVisitFilter_ShouldKeepTheSecondReadOwnerBoundAndBatched()
    {
        FilterDefinition<UserVisitDocument> filter =
            PassportItemStatisticsSourceReader.BuildVisitFilter(
                " owner-1 ",
                new[] { "visit-1", "visit-2" });

        BsonDocument rendered = Render(filter);

        Assert.Equal("owner-1", rendered["userId"].AsString);
        Assert.Equal(
            new[] { "visit-1", "visit-2" },
            rendered["_id"]["$in"].AsBsonArray.Select(static value => value.AsString));
    }

    [Theory]
    [InlineData(null, false, null, null, true)]
    [InlineData(null, false, null, 1L, false)]
    [InlineData(7L, true, 7L, 7L, true)]
    [InlineData(7L, true, 7L, 6L, false)]
    [InlineData(7L, false, 6L, 6L, true)]
    [InlineData(7L, false, 6L, 7L, true)]
    [InlineData(7L, false, 6L, 5L, false)]
    [InlineData(7L, false, null, null, true)]
    [InlineData(7L, false, null, 1L, true)]
    [InlineData(7L, false, null, 8L, false)]
    public void ContentFenceAllowsRead_ShouldExposeOnlyTheCurrentSafeInterval(
        long? currentFence,
        bool ready,
        long? stableFence,
        long? occurrenceFence,
        bool expected)
    {
        PassportItemVisitStatisticsSourceDocument visit = CreateVisitSource(
            "visit-1",
            currentFence,
            stableFence,
            ready);

        bool result = PassportItemStatisticsSourceReader.ContentFenceAllowsRead(
            visit,
            occurrenceFence);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildObservations_ShouldIgnoreMissingAndFencedOutVisitsWithoutLoadingPrivateText()
    {
        PassportItemOccurrenceStatisticsSourceDocument[] occurrences =
        {
            CreateOccurrenceSource("visit-current", 8, 4),
            CreateOccurrenceSource("visit-current", null, 3),
            CreateOccurrenceSource("visit-recovering", 6, 5),
            CreateOccurrenceSource("visit-recovering", 10, 3),
            CreateOccurrenceSource("visit-missing", 9, null),
        };
        IReadOnlyDictionary<string, PassportItemVisitStatisticsSourceDocument> visits =
            new Dictionary<string, PassportItemVisitStatisticsSourceDocument>(StringComparer.Ordinal)
            {
                ["visit-current"] = CreateVisitSource("visit-current", 4, 4, true),
                ["visit-recovering"] = CreateVisitSource("visit-recovering", 5, 4, false),
            };

        IReadOnlyCollection<PassportItemRideObservation> result =
            PassportItemStatisticsSourceReader.BuildObservations(occurrences, visits);

        Assert.Equal(2, result.Count);
        Assert.Collection(
            result,
            observation =>
            {
                Assert.Equal("visit-current", observation.VisitId);
                Assert.Equal(4d, observation.Assessment?.DoubleValue);
            },
            observation =>
            {
                Assert.Equal("visit-recovering", observation.VisitId);
                Assert.Equal(3d, observation.Assessment?.DoubleValue);
            });
    }

    private static PassportItemOccurrenceStatisticsSourceDocument CreateOccurrenceSource(
        string visitId,
        byte? assessmentHalfSteps,
        long? fenceToken)
    {
        return new PassportItemOccurrenceStatisticsSourceDocument
        {
            VisitId = visitId,
            AssessmentValueHalfSteps = assessmentHalfSteps,
            ContentMutationFenceToken = fenceToken,
        };
    }

    private static PassportItemVisitStatisticsSourceDocument CreateVisitSource(
        string visitId,
        long? currentFence,
        long? stableFence,
        bool ready)
    {
        return new PassportItemVisitStatisticsSourceDocument
        {
            Id = visitId,
            Date = new VisitDateDocument
            {
                Year = 2025,
                Month = 7,
                Day = 2,
                Precision = VisitDatePrecision.Day,
            },
            ContentMutationFenceToken = currentFence,
            ContentMutationFenceStableToken = stableFence,
            ContentMutationFenceReady = ready,
        };
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

    private static BsonDocument Render(FilterDefinition<UserVisitDocument> filter)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        return filter.Render(new RenderArgs<UserVisitDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
