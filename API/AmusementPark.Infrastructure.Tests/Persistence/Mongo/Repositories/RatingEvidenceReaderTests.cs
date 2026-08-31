using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RatingEvidenceReaderTests
{
    [Fact]
    public void BuildContributorPipeline_WhenParkAndItemsAreIncluded_ShouldDeduplicateUsersBeforeParkCounts()
    {
        IReadOnlyCollection<BsonDocument> pipeline = RatingEvidenceReader.BuildContributorPipeline(new[]
        {
            new RatingEvidenceTarget(RatingTargetType.Park, "park-1", "park-1"),
            new RatingEvidenceTarget(RatingTargetType.ParkItem, "item-1", "park-1"),
            new RatingEvidenceTarget(RatingTargetType.ParkItem, "item-2", "park-1"),
        });

        Assert.Equal(7, pipeline.Count);
        BsonDocument ratingValueExpression = pipeline.ElementAt(2)["$match"]["$expr"].AsBsonDocument;
        BsonArray ratingValueCondition = ratingValueExpression["$cond"].AsBsonArray;
        Assert.Equal("$value", ratingValueCondition[0]["$isNumber"].AsString);
        Assert.Contains("$mod", ratingValueCondition[1].ToJson(), StringComparison.Ordinal);
        Assert.False(ratingValueCondition[2].AsBoolean);

        BsonDocument perUserGroup = pipeline.ElementAt(3)["$group"].AsBsonDocument;
        Assert.Equal("$parkId", perUserGroup["_id"]["parkId"].AsString);
        Assert.Equal("$userId", perUserGroup["_id"]["userId"].AsString);
        Assert.Equal(1, perUserGroup["ratingObservationCount"]["$sum"].AsInt32);

        BsonDocument perParkGroup = pipeline.ElementAt(4)["$group"].AsBsonDocument;
        Assert.Equal("$_id.parkId", perParkGroup["_id"].AsString);
        Assert.Equal(1, perParkGroup["uniqueContributorCount"]["$sum"].AsInt32);
        Assert.Equal("$ratingObservationCount", perParkGroup["ratingObservationCount"]["$sum"].AsString);
        Assert.Equal(
            "$directRatingCount",
            perParkGroup["directParkContributorCount"]["$sum"]["$cond"][0]["$gt"][0].AsString);
        Assert.Equal(
            "$itemRatingCount",
            perParkGroup["itemContributorCount"]["$sum"]["$cond"][0]["$gt"][0].AsString);
    }

    [Fact]
    public void BuildPublicItemFacts_WhenFixturesMixStatuses_ShouldKeepOnlyCurrentPublicCoverage()
    {
        IReadOnlyCollection<BsonDocument> documents = new[]
        {
            CreateProjection("coaster-open", ParkItemCategory.Attraction, ParkItemStatusNormalizer.Operating),
            CreateProjection("coaster-closed", ParkItemCategory.Attraction, ParkItemStatusNormalizer.ClosedDefinitively),
            CreateProjection("restaurant", ParkItemCategory.Restaurant, null),
            CreateProjection("restaurant", ParkItemCategory.Restaurant, null),
            CreateProjection(string.Empty, ParkItemCategory.Shop, null),
        };

        RatingEvidenceReader.PublicParkItemEvidenceReadResult result =
            RatingEvidenceReader.BuildPublicItemFacts(documents);

        Assert.Collection(
            result.Facts.OrderBy(static fact => fact.TargetId, StringComparer.Ordinal),
            first =>
            {
                Assert.Equal("coaster-open", first.TargetId);
                Assert.Equal(ParkItemCategory.Attraction, first.Category);
            },
            second =>
            {
                Assert.Equal("restaurant", second.TargetId);
                Assert.Equal(ParkItemCategory.Restaurant, second.Category);
            });
        Assert.Equal("park-1", Assert.Single(result.IncompleteParkIds));
    }

    [Fact]
    public void BuildPublicItemFacts_WhenCategoryIsMalformed_ShouldMarkParkInventoryIncomplete()
    {
        IReadOnlyCollection<BsonDocument> documents = new[]
        {
            CreateProjection("valid-item", ParkItemCategory.Attraction, ParkItemStatusNormalizer.Operating),
            new BsonDocument
            {
                { "_id", "broken-item" },
                { "parkId", "park-1" },
                { "category", "FutureCategory" },
            },
        };

        RatingEvidenceReader.PublicParkItemEvidenceReadResult result =
            RatingEvidenceReader.BuildPublicItemFacts(documents);

        Assert.Single(result.Facts);
        Assert.Equal("park-1", Assert.Single(result.IncompleteParkIds));
    }

    private static BsonDocument CreateProjection(
        string id,
        ParkItemCategory category,
        string? status)
    {
        BsonDocument document = new BsonDocument
        {
            { "_id", id },
            { "parkId", "park-1" },
            { "category", category.ToString() },
        };
        if (status is not null)
        {
            document["attractionDetails"] = new BsonDocument("status", status);
        }

        return document;
    }
}
