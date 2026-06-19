using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRepository
{
    Task<UserRating?> GetUserRatingAsync(string userId, RatingTargetType targetType, string targetId, CancellationToken cancellationToken);

    Task<UserRating> UpsertUserRatingAsync(UserRating rating, CancellationToken cancellationToken);

    Task<RatingAggregate?> GetAggregateAsync(RatingTargetType targetType, string targetId, CancellationToken cancellationToken);

    Task<RatingAggregate?> RecalculateAggregateAsync(RatingTargetMetadataResult metadata, CancellationToken cancellationToken);

    Task<PagedResult<UserRatingListItemResult>> GetUserRatingsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);

    Task<UserRatingStatsResult> GetUserRatingStatsAsync(string userId, CancellationToken cancellationToken);

    Task<PagedResult<RatingRankingItemResult>> GetRankingsAsync(
        RatingTargetType? targetType,
        ParkItemCategory? parkItemCategory,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
