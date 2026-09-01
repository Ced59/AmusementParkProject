using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface ICanonicalParkItemRatingRankingReader
{
    Task<PagedResult<ParkItemRatingRankingResult>> ReadAsync(
        ParkItemCategory category,
        int page,
        int pageSize,
        string? search,
        ParkItemType? parkItemType,
        CancellationToken cancellationToken);
}
