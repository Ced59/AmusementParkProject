using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingSourceRevisionRepository
{
    Task<RatingRankingSourceRevision> IncrementAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken);

    Task<RatingRankingSourceRevision?> GetAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken);
}
