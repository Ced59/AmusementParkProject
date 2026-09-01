using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Application.Features.Ratings.Models;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingMutationGuard
{
    Task<RatingRankingMutationPreparation> PrepareMutationAsync(
        RatingTargetType targetType,
        ParkItemCategory? currentParkItemCategory,
        ParkItemCategory? previousParkItemCategory,
        CancellationToken cancellationToken);

    Task ScheduleRebuildsAsync(
        RatingRankingMutationPreparation preparation,
        CancellationToken cancellationToken);
}
