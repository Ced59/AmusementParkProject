using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankProvider
{
    Task<int?> GetRankAsync(RatingAggregate aggregate, CancellationToken cancellationToken);

    void Invalidate();
}
