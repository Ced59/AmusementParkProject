using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
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
}
