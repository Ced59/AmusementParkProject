using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public sealed record RatingAggregateTarget(
    RatingTargetType TargetType,
    string TargetId,
    string ParkId,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType);

public sealed record UserRatingMutationResult(
    UserRating Rating,
    RatingAggregate? Aggregate);

public sealed record UserRatingDeletionResult(
    UserRating? DeletedRating,
    RatingAggregate? Aggregate)
{
    public bool WasDeleted => this.DeletedRating is not null;
}

public interface IRatingRepository
{
    Task<UserRating?> GetUserRatingAsync(string userId, RatingTargetType targetType, string targetId, CancellationToken cancellationToken);

    Task<UserRatingMutationResult> UpsertUserRatingAndRecalculateAggregateAsync(
        UserRating rating,
        RatingAggregateTarget aggregateTarget,
        CancellationToken cancellationToken);

    Task<UserRatingDeletionResult> DeleteUserRatingAndRecalculateAggregateAsync(
        string userId,
        RatingTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);

    Task<RatingAggregate?> GetAggregateAsync(RatingTargetType targetType, string targetId, CancellationToken cancellationToken);

    Task<PagedResult<UserRatingListItemResult>> GetUserRatingsAsync(string userId, int page, int pageSize, string? parkSearch, CancellationToken cancellationToken);

    Task<UserRatingStatsResult> GetUserRatingStatsAsync(string userId, CancellationToken cancellationToken);

    Task<UserRatingStatsResult> GetVisibleUserRatingStatsAsync(string userId, CancellationToken cancellationToken);

    Task<RatingRankingSourceBatch> GetVisibleRankingSourcesAsync(
        ParkItemCategory? parkItemCategory,
        int maxItems,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RatingRankingItemResult>> GetVisibleParkItemRankingSourcesAsync(
        ParkItemCategory parkItemCategory,
        int maxItems,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserRatingListItemResult>> GetUserRankingSourcesAsync(
        string userId,
        int maxItems,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserRatingListItemResult>> GetVisibleUserRankingSourcesAsync(
        string userId,
        int maxItems,
        CancellationToken cancellationToken);
}
