using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

internal static class RatingRankingMutationCompletion
{
    public static Task<RatingRankingMutationPreparation> PrepareAsync(
        RatingTargetType targetType,
        ParkItemCategory? observedCategory,
        ParkItemCategory? retainedCategory,
        IRatingRankingMutationGuard rankingMutationGuard,
        CancellationToken cancellationToken)
    {
        return targetType == RatingTargetType.ParkItem
            ? rankingMutationGuard.PreparePotentialParkItemMutationAsync(cancellationToken)
            : rankingMutationGuard.PrepareMutationAsync(
                targetType,
                observedCategory,
                retainedCategory,
                cancellationToken);
    }

    public static async Task<ParkItemCategory?> ResolveAuthoritativeParkItemCategoryAsync(
        RatingTargetType targetType,
        string targetId,
        IParkItemRepository parkItemRepository)
    {
        if (targetType != RatingTargetType.ParkItem)
        {
            return null;
        }

        ParkItem? currentParkItem = await parkItemRepository.GetByIdAsync(
            targetId,
            includeHidden: false,
            cancellationToken: CancellationToken.None);
        return currentParkItem?.Category;
    }

    public static Task CompleteAsync(
        RatingTargetType targetType,
        RatingRankingMutationPreparation preparation,
        bool sourceChanged,
        ParkItemCategory? observedCategory,
        ParkItemCategory? retainedCategory,
        ParkItemCategory? authoritativeCategory,
        IRatingRankingMutationGuard rankingMutationGuard)
    {
        return targetType == RatingTargetType.ParkItem
            ? rankingMutationGuard.CompletePotentialParkItemMutationAsync(
                preparation,
                new[] { observedCategory, retainedCategory, authoritativeCategory },
                sourceChanged,
                CancellationToken.None)
            : rankingMutationGuard.CompleteMutationAsync(
                preparation,
                sourceChanged,
                CancellationToken.None);
    }
}
