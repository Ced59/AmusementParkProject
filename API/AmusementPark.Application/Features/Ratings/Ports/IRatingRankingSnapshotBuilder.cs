using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingSnapshotBuilder
{
    Task<RatingRankingSnapshotBuildPlan> BuildAsync(
        RankingScopeDefinition scope,
        CancellationToken cancellationToken);
}
