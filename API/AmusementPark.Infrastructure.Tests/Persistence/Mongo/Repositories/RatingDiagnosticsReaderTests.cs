using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RatingDiagnosticsReaderTests
{
    [Fact]
    public void BuildUserRatingsDiagnosticPipeline_ShouldKeepTheReportBoundedAndUseConfiguredAggregateCollection()
    {
        IReadOnlyCollection<BsonDocument> pipeline =
            RatingDiagnosticsReader.BuildUserRatingsDiagnosticPipeline("custom-rating-aggregates");

        Assert.Equal(4, pipeline.Count);
        BsonDocument facet = pipeline.Last()["$facet"].AsBsonDocument;
        Assert.True(facet.Contains("summary"));
        Assert.True(facet.Contains("duplicates"));
        Assert.True(facet.Contains("targetDistribution"));
        Assert.True(facet.Contains("integrity"));
        BsonArray integrity = facet["integrity"].AsBsonArray;
        Assert.Equal("custom-rating-aggregates", integrity[2]["$lookup"]["from"].AsString);
        BsonArray distinctValues = facet["distinctValues"].AsBsonArray;
        BsonArray sample = distinctValues[3]["$facet"]["sample"].AsBsonArray;
        Assert.Equal(RatingDiagnosticsReader.DistinctValueSampleLimit, sample[0]["$limit"].AsInt32);
    }

    [Fact]
    public void EvaluateIndexStatuses_WhenDefinitionsMatch_ShouldValidateEveryRequiredIndex()
    {
        IReadOnlyCollection<BsonDocument> userIndexes = new[]
        {
            CreateIndex("idx_user_ratings_user_target_unique", true, ("userId", 1), ("targetType", 1), ("targetId", 1)),
            CreateIndex("idx_user_ratings_target", false, ("targetType", 1), ("targetId", 1)),
            CreateIndex("idx_user_ratings_user_updated", false, ("userId", 1), ("updatedAt", -1)),
            CreateIndex("idx_user_ratings_user_park", false, ("userId", 1), ("parkId", 1)),
        };
        IReadOnlyCollection<BsonDocument> aggregateIndexes = new[]
        {
            CreateIndex("idx_rating_aggregates_target_unique", true, ("targetType", 1), ("targetId", 1)),
            CreateIndex("idx_rating_aggregates_ranking", false, ("bayesianScore", -1), ("ratingCount", -1), ("averageRating", -1)),
            CreateIndex("idx_rating_aggregates_type_ranking", false, ("targetType", 1), ("bayesianScore", -1), ("ratingCount", -1)),
            CreateIndex("idx_rating_aggregates_category_ranking", false, ("parkItemCategory", 1), ("bayesianScore", -1), ("ratingCount", -1)),
        };

        IReadOnlyCollection<RatingIndexStatusResult> results = RatingDiagnosticsReader.EvaluateIndexStatuses(
            "userRatings",
            userIndexes,
            "ratingAggregates",
            aggregateIndexes);

        Assert.Equal(8, results.Count);
        Assert.All(results, static result =>
        {
            Assert.True(result.IsPresent);
            Assert.True(result.MatchesExpectedDefinition);
        });
        Assert.True(results.Single(static result => result.Name == "idx_user_ratings_user_target_unique").IsUnique);
    }

    [Fact]
    public void EvaluateIndexStatuses_WhenUniqueInvariantIsMissing_ShouldExposeTheMismatch()
    {
        IReadOnlyCollection<BsonDocument> userIndexes = new[]
        {
            CreateIndex("idx_user_ratings_user_target_unique", false, ("userId", 1), ("targetType", 1), ("targetId", 1)),
        };

        IReadOnlyCollection<RatingIndexStatusResult> results = RatingDiagnosticsReader.EvaluateIndexStatuses(
            "userRatings",
            userIndexes,
            "ratingAggregates",
            Array.Empty<BsonDocument>());

        RatingIndexStatusResult uniqueVoteIndex = results.Single(static result =>
            result.Name == "idx_user_ratings_user_target_unique");
        Assert.True(uniqueVoteIndex.IsPresent);
        Assert.False(uniqueVoteIndex.IsUnique);
        Assert.False(uniqueVoteIndex.MatchesExpectedDefinition);
        Assert.Contains(results, static result => !result.IsPresent);
    }

    private static BsonDocument CreateIndex(string name, bool unique, params (string Name, int Direction)[] keys)
    {
        BsonDocument keyDocument = new BsonDocument();
        foreach ((string keyName, int direction) in keys)
        {
            keyDocument.Add(keyName, direction);
        }

        BsonDocument index = new BsonDocument
        {
            { "name", name },
            { "key", keyDocument },
        };
        if (unique)
        {
            index.Add("unique", true);
        }

        return index;
    }
}
