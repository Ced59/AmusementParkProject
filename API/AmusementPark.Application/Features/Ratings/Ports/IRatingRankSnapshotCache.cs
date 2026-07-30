using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankSnapshotCache
{
    Task<IReadOnlyDictionary<string, int>> GetOrCreateAsync(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory,
        Func<CancellationToken, Task<IReadOnlyDictionary<string, int>>> factory,
        CancellationToken cancellationToken);

    void Invalidate();
}
