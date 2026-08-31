using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RatingAggregateSourceReaderTests
{
    [Fact]
    public void BuildPipeline_ShouldDeduplicateContributorsAndRetainObservationTotals()
    {
        IReadOnlyCollection<BsonDocument> pipeline = RatingAggregateSourceReader.BuildPipeline(new[]
        {
            new RatingAggregateSourceTarget(RatingTargetType.Park, "park-1"),
            new RatingAggregateSourceTarget(RatingTargetType.ParkItem, "item-1"),
        });

        Assert.Equal(6, pipeline.Count);
        Assert.Contains("$isNumber", pipeline.ElementAt(1).ToJson(), StringComparison.Ordinal);
        BsonDocument contributorGroup = pipeline.ElementAt(2)["$group"].AsBsonDocument;
        Assert.Contains("$trim", contributorGroup["_id"]["userId"].ToJson(), StringComparison.Ordinal);
        Assert.Equal(1, contributorGroup["observationCount"]["$sum"].AsInt32);
        BsonDocument targetGroup = pipeline.ElementAt(3)["$group"].AsBsonDocument;
        Assert.Equal(1, targetGroup["uniqueContributorCount"]["$sum"].AsInt32);
        Assert.Equal("$observationCount", targetGroup["ratingObservationCount"]["$sum"].AsString);
        Assert.Equal("$ratingSum", targetGroup["ratingSum"]["$sum"].AsString);
    }

    [Fact]
    public void TryVerifyAndHydrateProjection_WhenDerivedScoreDiverges_ShouldReturnFalse()
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.ParkItem,
            TargetId = "item-1",
            RatingCount = 10,
            UniqueContributorCount = 10,
            RatingSum = 45d,
            AverageRating = 4.5d,
            BayesianScore = 4.2d,
        };
        RatingAggregateSourceFact source = new RatingAggregateSourceFact(
            RatingTargetType.ParkItem,
            "item-1",
            UniqueContributorCount: 10,
            RatingObservationCount: 10,
            RatingSum: 45d);

        Assert.False(RatingAggregateSourceReader.TryVerifyAndHydrateProjection(aggregate, source));
    }

    [Fact]
    public void TryVerifyAndHydrateProjection_WhenSourceAndProjectionAreEmpty_ShouldReturnTrue()
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            RatingCount = 0,
            UniqueContributorCount = 0,
            RatingSum = 0d,
            AverageRating = 0d,
            BayesianScore = RatingScoreCalculator.PriorMean,
        };

        Assert.True(RatingAggregateSourceReader.TryVerifyAndHydrateProjection(aggregate, source: null));
        Assert.Equal(0, aggregate.UniqueContributorCount);
    }

    [Fact]
    public void TryVerifyAndHydrateProjection_WhenLegacyCountIsMissing_ShouldUseVerifiedSourceCount()
    {
        RatingAggregate aggregate = new RatingAggregate
        {
            TargetType = RatingTargetType.ParkItem,
            TargetId = "item-1",
            RatingCount = 10,
            UniqueContributorCount = null,
            RatingSum = 45d,
            AverageRating = 4.5d,
            BayesianScore = 4d,
        };
        RatingAggregateSourceFact source = new RatingAggregateSourceFact(
            RatingTargetType.ParkItem,
            "item-1",
            UniqueContributorCount: 8,
            RatingObservationCount: 10,
            RatingSum: 45d);

        Assert.True(RatingAggregateSourceReader.TryVerifyAndHydrateProjection(aggregate, source));
        Assert.Equal(8, aggregate.UniqueContributorCount);
    }
}
