using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RatingRepositoryTests
{
    [Fact]
    public void BuildUserRatingSearchWindow_WhenMatchedParkHasManyRatings_ShouldCapResultsToPageSize()
    {
        List<UserRatingListItemResult> ratings = Enumerable.Range(1, 12)
            .Select(index => CreateRating($"rating-{index}", $"item-{index}", $"Target {index}", "park-1", "Match Park", 5d - (index / 100d)))
            .ToList();

        IReadOnlyCollection<UserRatingListItemResult> result = RatingRepository.BuildUserRatingSearchWindow(ratings, "match", 5);

        Assert.Equal(5, result.Count);
        Assert.All(result, static item => Assert.Equal("park-1", item.ParkId));
    }

    [Fact]
    public void BuildUserRatingSearchWindow_WhenContextHasManyRatings_ShouldKeepMatchedParkFirst()
    {
        List<UserRatingListItemResult> ratings = Enumerable.Range(1, 8)
            .Select(index => CreateRating($"top-{index}", $"top-item-{index}", $"Top Target {index}", "park-top", "Top Park", 5d))
            .Concat(Enumerable.Range(1, 2)
                .Select(index => CreateRating($"match-{index}", $"match-item-{index}", $"Match Target {index}", "park-match", "Match Park", 4d)))
            .ToList();

        IReadOnlyCollection<UserRatingListItemResult> result = RatingRepository.BuildUserRatingSearchWindow(ratings, "match", 5);

        Assert.Equal(5, result.Count);
        Assert.Equal("park-match", result.First().ParkId);
        Assert.Contains(result, static item => item.ParkId == "park-top");
    }

    [Theory]
    [InlineData(ParkStatus.Operating, true)]
    [InlineData(ParkStatus.TemporarilyClosed, true)]
    [InlineData(ParkStatus.ClosedDefinitively, true)]
    [InlineData(ParkStatus.Planned, false)]
    [InlineData(ParkStatus.UnderConstruction, false)]
    [InlineData(ParkStatus.Cancelled, false)]
    public void CanTargetReceiveVisitorRatings_ForRetainedParkRating_ShouldHonorParkStatus(
        ParkStatus status,
        bool expected)
    {
        UserRatingDocument rating = new UserRatingDocument
        {
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
        };
        Dictionary<string, ParkDocument> parks = new Dictionary<string, ParkDocument>(StringComparer.Ordinal)
        {
            ["park-1"] = new ParkDocument { Id = "park-1", Status = status },
        };

        bool result = RatingRepository.CanTargetReceiveVisitorRatings(
            rating,
            parks,
            new Dictionary<string, ParkItemDocument>(StringComparer.Ordinal));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ParkStatus.Operating, ParkItemStatusNormalizer.Operating, true)]
    [InlineData(ParkStatus.Operating, ParkItemStatusNormalizer.Planned, false)]
    [InlineData(ParkStatus.Planned, ParkItemStatusNormalizer.Operating, false)]
    public void CanTargetReceiveVisitorRatings_ForRetainedParkItemRating_ShouldHonorParentAndItemStatus(
        ParkStatus parkStatus,
        string itemStatus,
        bool expected)
    {
        UserRatingDocument rating = new UserRatingDocument
        {
            TargetType = RatingTargetType.ParkItem,
            TargetId = "item-1",
            ParkId = "park-1",
        };
        Dictionary<string, ParkDocument> parks = new Dictionary<string, ParkDocument>(StringComparer.Ordinal)
        {
            ["park-1"] = new ParkDocument { Id = "park-1", Status = parkStatus },
        };
        Dictionary<string, ParkItemDocument> parkItems = new Dictionary<string, ParkItemDocument>(StringComparer.Ordinal)
        {
            ["item-1"] = new ParkItemDocument
            {
                Id = "item-1",
                ParkId = "park-1",
                Category = ParkItemCategory.Attraction,
                AttractionDetails = new AttractionDetailsDocument { Status = itemStatus },
            },
        };

        bool result = RatingRepository.CanTargetReceiveVisitorRatings(rating, parks, parkItems);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(5000, 100, 100, false)]
    [InlineData(5001, 100, 100, true)]
    [InlineData(5000, 101, 100, true)]
    public void IsVisibleRankingSourceSetTruncated_ShouldDetectLookAheadDocument(
        int parkDocumentCount,
        int parkItemDocumentCount,
        int parkItemLimit,
        bool expected)
    {
        bool result = RatingRepository.IsVisibleRankingSourceSetTruncated(
            parkDocumentCount,
            parkItemDocumentCount,
            parkItemLimit);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(5000, 5000, false)]
    [InlineData(5001, 5000, true)]
    public void IsParkItemRankingSourceSetTruncated_ShouldDetectLookAheadDocument(
        int documentCount,
        int documentLimit,
        bool expected)
    {
        bool result = RatingRepository.IsParkItemRankingSourceSetTruncated(
            documentCount,
            documentLimit);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsParkRankingSetTruncated_ShouldApplyLimitAfterGroupingSourcesByPark()
    {
        IReadOnlyCollection<RatingRankingItemResult> sameParkSources = Enumerable.Range(1, 6)
            .Select(index => CreateParkItemRankingSource($"item-{index}", "park-1"))
            .ToArray();
        IReadOnlyCollection<RatingRankingItemResult> distinctParkSources = Enumerable.Range(1, 6)
            .Select(index => CreateParkItemRankingSource($"item-{index}", $"park-{index}"))
            .ToArray();

        Assert.False(RatingRepository.IsParkRankingSetTruncated(sameParkSources, 5));
        Assert.True(RatingRepository.IsParkRankingSetTruncated(distinctParkSources, 5));
    }

    [Fact]
    public void BuildParkItemRankingCandidatePipeline_ShouldStreamCandidatesAfterCurrentEligibilityJoins()
    {
        BsonDocument[] pipeline = RatingRepository.BuildParkItemRankingCandidatePipeline(
            ParkItemCategory.Attraction,
            "parkItems",
            "parks");

        int parkItemCategoryMatchIndex = pipeline
            .Select(static (stage, index) => (stage, index))
            .Single(value => value.stage.Contains("$match")
                && value.stage["$match"].AsBsonDocument.Contains("rankingParkItem.category"))
            .index;
        int parentEligibilityMatchIndex = pipeline
            .Select(static (stage, index) => (stage, index))
            .Single(value => value.stage.Contains("$match")
                && value.stage["$match"].AsBsonDocument.Contains("rankingParentPark.status"))
            .index;
        int projectionIndex = pipeline
            .Select(static (stage, index) => (stage, index))
            .Single(value => value.stage.Contains("$project"))
            .index;

        Assert.True(parkItemCategoryMatchIndex < projectionIndex);
        Assert.True(parentEligibilityMatchIndex < projectionIndex);
        Assert.DoesNotContain(pipeline, static stage => stage.Contains("$limit"));
        Assert.DoesNotContain(pipeline, static stage => stage.Contains("$skip"));
    }

    [Fact]
    public void BuildParkRankingCandidatePipeline_ShouldApplyLookAheadAfterCurrentParkEligibility()
    {
        BsonDocument[] pipeline = RatingRepository.BuildParkRankingCandidatePipeline(
            "parks",
            5001);

        int eligibilityMatchIndex = pipeline
            .Select(static (stage, index) => (stage, index))
            .Single(value => value.stage.Contains("$match")
                && value.stage["$match"].AsBsonDocument.Contains("rankingPark.status"))
            .index;
        int limitIndex = pipeline
            .Select(static (stage, index) => (stage, index))
            .Single(value => value.stage.Contains("$limit"))
            .index;

        Assert.True(eligibilityMatchIndex < limitIndex);
        Assert.Equal(5001, pipeline[limitIndex]["$limit"].AsInt32);
    }

    private static UserRatingListItemResult CreateRating(
        string id,
        string targetId,
        string targetName,
        string parkId,
        string parkName,
        double value)
    {
        RatingSummaryResult summary = new RatingSummaryResult(RatingTargetType.ParkItem, targetId, 1, value, value);
        return new UserRatingListItemResult(
            id,
            RatingTargetType.ParkItem,
            targetId,
            targetName,
            parkId,
            parkName,
            null,
            null,
            value,
            DateTime.UtcNow,
            summary);
    }

    private static RatingRankingItemResult CreateParkItemRankingSource(
        string targetId,
        string parkId)
    {
        return new RatingRankingItemResult(
            RatingTargetType.ParkItem,
            targetId,
            targetId,
            parkId,
            parkId,
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            10,
            45d,
            4.5d,
            4d);
    }
}
