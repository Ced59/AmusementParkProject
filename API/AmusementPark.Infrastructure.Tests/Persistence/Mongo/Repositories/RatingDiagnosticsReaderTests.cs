using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;
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
            RatingDiagnosticsReader.BuildUserRatingsDiagnosticPipeline(
                "custom-rating-aggregates",
                new[] { "park-1" },
                new[] { "item-1" },
                true);

        Assert.Equal(4, pipeline.Count);
        BsonDocument facet = pipeline.Last()["$facet"].AsBsonDocument;
        Assert.True(facet.Contains("summary"));
        Assert.True(facet.Contains("duplicates"));
        Assert.True(facet.Contains("targetDistribution"));
        Assert.True(facet.Contains("integrity"));
        BsonArray integrity = facet["integrity"].AsBsonArray;
        Assert.Equal("custom-rating-aggregates", integrity[3]["$lookup"]["from"].AsString);
        BsonArray distinctValues = facet["distinctValues"].AsBsonArray;
        BsonArray sample = distinctValues[3]["$facet"]["sample"].AsBsonArray;
        Assert.Equal(RatingDiagnosticsReader.DistinctValueSampleLimit, sample[0]["$limit"].AsInt32);

        BsonDocument distributionMatch = facet["targetDistribution"].AsBsonArray[0]["$match"].AsBsonDocument;
        Assert.True(distributionMatch["_diagnosticIsExactHalfStep"].AsBoolean);
        BsonArray eligibility = distributionMatch["$or"].AsBsonArray;
        Assert.Equal("park-1", Assert.Single(eligibility[0]["_diagnosticTargetText"]["$in"].AsBsonArray).AsString);
        Assert.Equal("item-1", Assert.Single(eligibility[1]["_diagnosticTargetText"]["$in"].AsBsonArray).AsString);
    }

    [Fact]
    public void BuildUserRatingsDiagnosticPipeline_ShouldRequireAValidTargetTypeForTargetMeasurements()
    {
        IReadOnlyCollection<BsonDocument> pipeline =
            RatingDiagnosticsReader.BuildUserRatingsDiagnosticPipeline(
                "ratingAggregates",
                Array.Empty<string>(),
                Array.Empty<string>(),
                true);

        BsonDocument hasTarget = pipeline.ElementAt(1)["$set"]["_diagnosticHasTarget"].AsBsonDocument;
        BsonArray requirements = hasTarget["$and"].AsBsonArray;
        BsonDocument targetTypeRequirement = requirements[1].AsBsonDocument;
        BsonArray targetTypes = targetTypeRequirement["$in"].AsBsonArray[1].AsBsonArray;

        Assert.Equal(new[] { "Park", "ParkItem" }, targetTypes.Select(static value => value.AsString));
    }

    [Fact]
    public void BuildUserRatingsDiagnosticPipeline_WithDuplicateVotes_ShouldCompareAggregateCountToUniqueContributors()
    {
        IReadOnlyCollection<BsonDocument> pipeline =
            RatingDiagnosticsReader.BuildUserRatingsDiagnosticPipeline(
                "ratingAggregates",
                Array.Empty<string>(),
                Array.Empty<string>(),
                true);

        BsonArray integrity = pipeline.Last()["$facet"]["integrity"].AsBsonArray;
        BsonDocument contributorKey = integrity[1]["$group"]["_id"].AsBsonDocument;
        BsonDocument targetGroup = integrity[2]["$group"].AsBsonDocument;

        Assert.Equal("$_diagnosticUserText", contributorKey["userId"].AsString);
        Assert.Equal("$hasValidUser", targetGroup["sourceUniqueContributorCount"]["$sum"].AsString);
        Assert.Equal("$ratingObservationCount", targetGroup["sourceRatingObservationCount"]["$sum"].AsString);

        BsonDocument facets = CreateIntegrityFacets(
            sourceObservationCount: 2,
            sourceUniqueContributorCount: 1,
            sourceRatingSum: 8d,
            aggregateRatingCount: 2,
            aggregateRatingSum: 8d,
            aggregateAverageRating: 4d,
            aggregateBayesianScore: RatingScoreCalculator.CalculateBayesianScore(8d, 2));
        RatingAggregateIntegrityResult result = RatingDiagnosticsReader.EvaluateAggregateIntegrity(
            facets,
            true,
            true,
            0);

        Assert.Equal(1, result.ContributorCountMismatchCount);
        Assert.Equal(1, result.DivergentAggregateCount);
        Assert.Equal(0, result.DerivedScoreMismatchCount);
    }

    [Fact]
    public void BuildUserRatingsDiagnosticPipeline_WithMalformedValues_ShouldKeepEveryStoredSourceInIntegrityChecks()
    {
        IReadOnlyCollection<BsonDocument> pipeline =
            RatingDiagnosticsReader.BuildUserRatingsDiagnosticPipeline(
                "ratingAggregates",
                Array.Empty<string>(),
                Array.Empty<string>(),
                true);

        BsonArray integrity = pipeline.Last()["$facet"]["integrity"].AsBsonArray;
        BsonDocument integrityMatch = integrity[0]["$match"].AsBsonDocument;
        BsonDocument sourceGroup = integrity[1]["$group"].AsBsonDocument;
        BsonDocument targetGroup = integrity[2]["$group"].AsBsonDocument;

        Assert.True(integrityMatch["_diagnosticHasTarget"].AsBoolean);
        Assert.False(integrityMatch.Contains("_diagnosticHasUser"));
        Assert.False(integrityMatch.Contains("_diagnosticIsExactHalfStep"));
        Assert.Equal("$_diagnosticNumericValue", sourceGroup["sourceRatingSum"]["$sum"].AsString);
        Assert.Contains("$_diagnosticHasUser", sourceGroup["hasValidUser"].ToJson(), StringComparison.Ordinal);
        Assert.Equal("$hasValidUser", targetGroup["sourceUniqueContributorCount"]["$sum"].AsString);
    }

    [Fact]
    public void EvaluateAggregateIntegrity_WhenDerivedScoresAreStale_ShouldExposeTheDivergence()
    {
        BsonDocument facets = CreateIntegrityFacets(
            sourceObservationCount: 2,
            sourceUniqueContributorCount: 2,
            sourceRatingSum: 8d,
            aggregateRatingCount: 2,
            aggregateRatingSum: 8d,
            aggregateAverageRating: 3.75d,
            aggregateBayesianScore: RatingScoreCalculator.CalculateBayesianScore(8d, 2));

        RatingAggregateIntegrityResult result = RatingDiagnosticsReader.EvaluateAggregateIntegrity(
            facets,
            true,
            true,
            0);

        Assert.Equal(1, result.DerivedScoreMismatchCount);
        Assert.Equal(1, result.DivergentAggregateCount);
        Assert.Equal(0, result.ContributorCountMismatchCount);
    }

    [Fact]
    public void BuildUserRatingsDiagnosticPipeline_WhenAggregateIndexIsUnavailable_ShouldSkipTheDependentLookup()
    {
        IReadOnlyCollection<BsonDocument> pipeline =
            RatingDiagnosticsReader.BuildUserRatingsDiagnosticPipeline(
                "ratingAggregates",
                Array.Empty<string>(),
                Array.Empty<string>(),
                false);

        BsonDocument facet = pipeline.Last()["$facet"].AsBsonDocument;
        Assert.False(facet.Contains("integrity"));
        Assert.True(facet.Contains("summary"));
        Assert.True(facet.Contains("targetDistribution"));
    }

    [Fact]
    public void CompleteTargetDistribution_ShouldIncludeEveryEligibleTargetWithoutAContributor()
    {
        IReadOnlyCollection<RatingTargetDistributionResult> ratedTargets = new[]
        {
            new RatingTargetDistributionResult("Park", "3-9", 2, 14, 12),
            new RatingTargetDistributionResult("Park", "1-2", 1, 2, 2),
            new RatingTargetDistributionResult("ParkItem", "10-29", 4, 48, 45),
        };

        IReadOnlyCollection<RatingTargetDistributionResult> results =
            RatingDiagnosticsReader.CompleteTargetDistribution(ratedTargets, 8, 10);

        RatingTargetDistributionResult parkZero = results.Single(static result =>
            result.TargetType == "Park" && result.EvidenceBand == "0");
        RatingTargetDistributionResult parkItemZero = results.Single(static result =>
            result.TargetType == "ParkItem" && result.EvidenceBand == "0");
        Assert.Equal(5, parkZero.TargetCount);
        Assert.Equal(6, parkItemZero.TargetCount);
        Assert.Equal(0, parkZero.RatingObservationCount);
        Assert.Equal(new[] { "0", "1-2", "3-9" }, results
            .Where(static result => result.TargetType == "Park")
            .Select(static result => result.EvidenceBand));
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
            Assert.True(result.SupportsExpectedQueries);
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

    [Fact]
    public void EvaluateIndexStatuses_WhenAggregateIndexLosesUniqueness_ShouldKeepTheLookupUsable()
    {
        BsonDocument aggregateIndex = CreateIndex(
            RatingDiagnosticsReader.RatingAggregatesTargetIndexName,
            false,
            ("targetType", 1),
            ("targetId", 1));

        IReadOnlyCollection<RatingIndexStatusResult> results = RatingDiagnosticsReader.EvaluateIndexStatuses(
            "userRatings",
            Array.Empty<BsonDocument>(),
            "ratingAggregates",
            new[] { aggregateIndex });

        RatingIndexStatusResult result = results.Single(static item =>
            item.Name == RatingDiagnosticsReader.RatingAggregatesTargetIndexName);
        Assert.False(result.IsUnique);
        Assert.True(result.SupportsExpectedQueries);
        Assert.False(result.MatchesExpectedDefinition);
    }

    [Fact]
    public void EvaluateIndexStatuses_WhenIndexHasTrailingKey_ShouldKeepTheQueryUsable()
    {
        BsonDocument aggregateIndex = CreateIndex(
            RatingDiagnosticsReader.RatingAggregatesTargetIndexName,
            true,
            ("targetType", 1),
            ("targetId", 1),
            ("updatedAt", -1));

        IReadOnlyCollection<RatingIndexStatusResult> results = RatingDiagnosticsReader.EvaluateIndexStatuses(
            "userRatings",
            Array.Empty<BsonDocument>(),
            "ratingAggregates",
            new[] { aggregateIndex });

        RatingIndexStatusResult result = results.Single(static item =>
            item.Name == RatingDiagnosticsReader.RatingAggregatesTargetIndexName);
        Assert.True(result.SupportsExpectedQueries);
        Assert.False(result.MatchesExpectedDefinition);
    }

    [Fact]
    public void EvaluateIndexStatuses_WhenExpectedKeysAreNotTheLeadingPrefix_ShouldRejectQuerySupport()
    {
        BsonDocument aggregateIndex = CreateIndex(
            RatingDiagnosticsReader.RatingAggregatesTargetIndexName,
            true,
            ("updatedAt", -1),
            ("targetType", 1),
            ("targetId", 1));

        IReadOnlyCollection<RatingIndexStatusResult> results = RatingDiagnosticsReader.EvaluateIndexStatuses(
            "userRatings",
            Array.Empty<BsonDocument>(),
            "ratingAggregates",
            new[] { aggregateIndex });

        RatingIndexStatusResult result = results.Single(static item =>
            item.Name == RatingDiagnosticsReader.RatingAggregatesTargetIndexName);
        Assert.False(result.SupportsExpectedQueries);
        Assert.False(result.MatchesExpectedDefinition);
    }

    [Fact]
    public void EvaluateIndexStatuses_WhenIndexIsHidden_ShouldExposeTheDefinitionMismatch()
    {
        BsonDocument hiddenIndex = CreateIndex(
            "idx_user_ratings_target",
            false,
            ("targetType", 1),
            ("targetId", 1));
        hiddenIndex.Add("hidden", true);

        IReadOnlyCollection<RatingIndexStatusResult> results = RatingDiagnosticsReader.EvaluateIndexStatuses(
            "userRatings",
            new[] { hiddenIndex },
            "ratingAggregates",
            Array.Empty<BsonDocument>());

        RatingIndexStatusResult result = results.Single(static item => item.Name == "idx_user_ratings_target");
        Assert.True(result.IsPresent);
        Assert.True(result.IsHidden);
        Assert.False(result.SupportsExpectedQueries);
        Assert.False(result.MatchesExpectedDefinition);
    }

    [Theory]
    [InlineData("partial")]
    [InlineData("sparse")]
    [InlineData("collation")]
    [InlineData("ttl")]
    public void EvaluateIndexStatuses_WhenIndexHasUnexpectedOptions_ShouldExposeTheDefinitionMismatch(
        string option)
    {
        BsonDocument index = CreateIndex(
            RatingDiagnosticsReader.UserRatingsTargetIndexName,
            false,
            ("targetType", 1),
            ("targetId", 1));
        switch (option)
        {
            case "partial":
                index.Add("partialFilterExpression", new BsonDocument("targetId", new BsonDocument("$exists", true)));
                break;
            case "sparse":
                index.Add("sparse", true);
                break;
            case "collation":
                index.Add("collation", new BsonDocument("locale", "fr"));
                break;
            case "ttl":
                index.Add("expireAfterSeconds", 3600);
                break;
            default:
                throw new InvalidOperationException($"Unsupported index option fixture: {option}.");
        }

        IReadOnlyCollection<RatingIndexStatusResult> results = RatingDiagnosticsReader.EvaluateIndexStatuses(
            "userRatings",
            new[] { index },
            "ratingAggregates",
            Array.Empty<BsonDocument>());

        RatingIndexStatusResult result = results.Single(static item =>
            item.Name == RatingDiagnosticsReader.UserRatingsTargetIndexName);
        Assert.True(result.HasUnexpectedOptions);
        Assert.Equal(option == "ttl", result.SupportsExpectedQueries);
        Assert.False(result.MatchesExpectedDefinition);
    }

    private static BsonDocument CreateIntegrityFacets(
        long sourceObservationCount,
        long sourceUniqueContributorCount,
        double sourceRatingSum,
        long aggregateRatingCount,
        double aggregateRatingSum,
        double aggregateAverageRating,
        double aggregateBayesianScore)
    {
        return new BsonDocument("integrity", new BsonArray
        {
            new BsonDocument
            {
                { "sourceRatingObservationCount", sourceObservationCount },
                { "sourceUniqueContributorCount", sourceUniqueContributorCount },
                { "sourceRatingSum", sourceRatingSum },
                { "aggregateCount", 1 },
                {
                    "aggregate",
                    new BsonDocument
                    {
                        { "ratingCount", aggregateRatingCount },
                        { "ratingSum", aggregateRatingSum },
                        { "averageRating", aggregateAverageRating },
                        { "bayesianScore", aggregateBayesianScore },
                    }
                },
            },
        });
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
