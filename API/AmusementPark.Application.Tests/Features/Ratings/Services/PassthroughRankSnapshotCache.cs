using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

internal sealed class PassthroughRankSnapshotCache : IRatingRankSnapshotCache
{
    public Task<RatingPublishedRankingSnapshot?> GetOrCreatePublishedAsync(
        RankingScopeKey scopeKey,
        RankingSnapshotId snapshotId,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision,
        long pointerVersion,
        Func<CancellationToken, Task<RatingPublishedRankingSnapshot?>> factory,
        CancellationToken cancellationToken)
    {
        return factory(cancellationToken);
    }

    public void Invalidate()
    {
    }
}
