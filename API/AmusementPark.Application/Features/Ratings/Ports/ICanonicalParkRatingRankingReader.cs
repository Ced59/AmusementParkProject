using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface ICanonicalParkRatingRankingReader
{
    Task<PagedResult<ParkRatingRankingResult>> ReadAsync(
        int page,
        int pageSize,
        string? parkSearch,
        CancellationToken cancellationToken);
}
