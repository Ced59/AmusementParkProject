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

        Assert.Equal(6, pipeline.Count);
        BsonDocument perUserGroup = pipeline.ElementAt(2)["$group"].AsBsonDocument;
        Assert.Equal("$parkId", perUserGroup["_id"]["parkId"].AsString);
        Assert.Equal("$userId", perUserGroup["_id"]["userId"].AsString);
        Assert.Equal(1, perUserGroup["ratingObservationCount"]["$sum"].AsInt32);

        BsonDocument perParkGroup = pipeline.ElementAt(3)["$group"].AsBsonDocument;
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
        IReadOnlyCollection<RatingEvidenceReader.PublicParkItemProjection> documents = new[]
        {
            CreateProjection("coaster-open", ParkItemCategory.Attraction, ParkItemStatusNormalizer.Operating),
            CreateProjection("coaster-closed", ParkItemCategory.Attraction, ParkItemStatusNormalizer.ClosedDefinitively),
            CreateProjection("restaurant", ParkItemCategory.Restaurant, null),
            CreateProjection("restaurant", ParkItemCategory.Restaurant, null),
            CreateProjection(string.Empty, ParkItemCategory.Shop, null),
        };

        IReadOnlyCollection<PublicParkItemEvidenceFact> facts = RatingEvidenceReader.BuildPublicItemFacts(documents);

        Assert.Collection(
            facts.OrderBy(static fact => fact.TargetId, StringComparer.Ordinal),
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
    }

    private static RatingEvidenceReader.PublicParkItemProjection CreateProjection(
        string id,
        ParkItemCategory category,
        string? status)
    {
        return new RatingEvidenceReader.PublicParkItemProjection
        {
            Id = id,
            ParkId = "park-1",
            Category = category,
            AttractionStatus = status,
        };
    }
}
