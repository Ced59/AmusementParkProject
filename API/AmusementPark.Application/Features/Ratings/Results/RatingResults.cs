using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingSummaryResult(
    RatingTargetType TargetType,
    string TargetId,
    long RatingCount,
    double AverageRating,
    double BayesianScore);

public sealed record RatingTargetMetadataResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType);

public sealed record UserRatingResult(
    string Id,
    string UserId,
    RatingTargetType TargetType,
    string TargetId,
    string ParkId,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    double Value,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    RatingSummaryResult Summary);

public sealed record UserRatingListItemResult(
    string Id,
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    double Value,
    DateTime UpdatedAtUtc,
    RatingSummaryResult Summary);

public sealed record UserRatingStatBucketResult(
    string Key,
    string Label,
    long Count,
    double AverageRating);

public sealed record UserRatingStatsResult(
    long TotalRatings,
    double AverageRating,
    double HighestRating,
    double LowestRating,
    IReadOnlyCollection<UserRatingStatBucketResult> ByPark,
    IReadOnlyCollection<UserRatingStatBucketResult> ByTargetType,
    IReadOnlyCollection<UserRatingStatBucketResult> ByParkItemCategory);

public sealed record RatingRankingItemResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double RatingSum,
    double AverageRating,
    double BayesianScore);

public sealed record ParkRatingRankingItemResult(
    string TargetId,
    string TargetName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double AverageRating,
    double BayesianScore);

public sealed record ParkRatingRankingCategoryResult(
    ParkItemCategory ParkItemCategory,
    long RatingCount,
    double AverageRating,
    double BayesianScore,
    IReadOnlyCollection<ParkRatingRankingItemResult> Items);

public sealed record ParkRatingRankingResult(
    int Rank,
    string ParkId,
    string ParkName,
    long RatingCount,
    double Score,
    long ParkRatingCount,
    double ParkAverageRating,
    long ItemsRatingCount,
    double ItemsAverageRating,
    IReadOnlyCollection<ParkRatingRankingCategoryResult> Categories);
