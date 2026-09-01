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

    Task<RatingRankingMutationPreparation> PreparePotentialParkItemMutationAsync(
        CancellationToken cancellationToken);

    Task CompleteMutationAsync(
        RatingRankingMutationPreparation preparation,
        bool sourceChanged,
        CancellationToken cancellationToken);

    Task CompletePotentialParkItemMutationAsync(
        RatingRankingMutationPreparation preparation,
        IReadOnlyCollection<ParkItemCategory?> affectedCategories,
        bool sourceChanged,
        CancellationToken cancellationToken);
}
