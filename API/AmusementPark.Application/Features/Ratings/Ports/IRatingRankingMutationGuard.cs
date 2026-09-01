using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Application.Features.Ratings.Models;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingMutationGuard
{
    Task<RatingRankingMutationPreparation> PrepareMutationAsync(
        RatingRankingMutationRecoveryTarget recoveryTarget,
        ParkItemCategory? currentParkItemCategory,
        ParkItemCategory? previousParkItemCategory,
        CancellationToken cancellationToken);

    Task CompleteMutationAsync(
        RatingRankingMutationPreparation preparation,
        bool sourceChanged,
        CancellationToken cancellationToken);
}
