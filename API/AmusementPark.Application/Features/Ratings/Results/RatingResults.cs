using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingSummaryResult(
    RatingTargetType TargetType,
    string TargetId,
    long RatingCount,
    double AverageRating,
    double BayesianScore)
{
    public int? Rank { get; init; }
}

public sealed record RatingTargetMetadataResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    bool CanReceiveVisitorRatings);

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

public sealed record ParkItemRatingRankingResult(
    int Rank,
    string TargetId,
    string TargetName,
    string ParkId,
    string ParkName,
    ParkItemCategory ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double AverageRating,
    double BayesianScore);

public sealed record UserParkRatingRankingCategoryResult(
    ParkItemCategory ParkItemCategory,
    double AverageRating,
    IReadOnlyCollection<UserRatingListItemResult> Items);

public sealed record UserParkRatingRankingResult(
    int Rank,
    string ParkId,
    string ParkName,
    int RatingCount,
    double AverageRating,
    UserRatingListItemResult? ParkRating,
    IReadOnlyCollection<UserParkRatingRankingCategoryResult> Categories);

public sealed record UserParkItemRatingRankingResult(
    int Rank,
    UserRatingListItemResult Rating);

public sealed record UserRankingShareSettingsResult(
    bool IsPublic,
    string? ShareId,
    DateTime? PublishedAtUtc);

public sealed record SharedUserRankingProfileResult(
    string OwnerUserId,
    string DisplayName,
    DateTime PublishedAtUtc,
    UserRatingStatsResult Stats);

public sealed record UserRankingSharePreviewItemResult(
    int Rank,
    string Name,
    string? ParkName,
    double Rating);

public sealed record UserRankingSharePreviewResult(
    string DisplayName,
    IReadOnlyCollection<UserRankingSharePreviewItemResult> Items);

public sealed record UserRankingSharePreviewFileResult(
    byte[] Content,
    string ContentType);
