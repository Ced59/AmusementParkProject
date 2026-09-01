using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankProvider
{
    Task<RatingPublishedRank?> GetRankAsync(
        RatingAggregate aggregate,
        CancellationToken cancellationToken);

    Task<RatingPublishedRankingSnapshot?> GetCanonicalSnapshotAsync(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory,
        CancellationToken cancellationToken);

    void Invalidate();
}
