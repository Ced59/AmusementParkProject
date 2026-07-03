using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;
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
