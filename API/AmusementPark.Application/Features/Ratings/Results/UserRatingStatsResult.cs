namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserRatingStatsResult(
    long TotalRatings,
    double AverageRating,
    double HighestRating,
    double LowestRating,
    IReadOnlyCollection<UserRatingStatBucketResult> ByPark,
    IReadOnlyCollection<UserRatingStatBucketResult> ByTargetType,
    IReadOnlyCollection<UserRatingStatBucketResult> ByParkItemCategory);
