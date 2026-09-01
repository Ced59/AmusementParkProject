using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

internal static class RatingRankingMutationCompletion
{
    public static async Task<RatingRankingMutationPreparation?> PrepareAuthoritativeParkItemCategoryAsync(
        RatingTargetType targetType,
        string targetId,
        ParkItemCategory? observedCategory,
        ParkItemCategory? retainedCategory,
        IParkItemRepository parkItemRepository,
        IRatingRankingMutationGuard rankingMutationGuard)
    {
        if (targetType != RatingTargetType.ParkItem)
        {
            return null;
        }

        ParkItem? currentParkItem = await parkItemRepository.GetByIdAsync(
            targetId,
            includeHidden: false,
            cancellationToken: CancellationToken.None);
        ParkItemCategory? authoritativeCategory = currentParkItem?.Category;
        if (!authoritativeCategory.HasValue
            || authoritativeCategory == observedCategory
            || authoritativeCategory == retainedCategory)
        {
            return null;
        }

        return await rankingMutationGuard.PrepareMutationAsync(
            RatingTargetType.ParkItem,
            authoritativeCategory,
            previousParkItemCategory: null,
            cancellationToken: CancellationToken.None);
    }
}
