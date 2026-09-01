using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

internal static class RatingRankingMutationCompletion
{
    private const int MaximumMetadataFenceAttempts = 3;

    public static async Task<RatingRankingPreparedMutation?> PrepareAsync(
        RatingTargetType targetType,
        string targetId,
        RatingTargetMetadataResult? observedMetadata,
        ParkItemCategory? retainedCategory,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IRatingRankingMutationGuard rankingMutationGuard,
        CancellationToken cancellationToken)
    {
        RatingTargetMetadataResult? currentMetadata = observedMetadata;
        for (int attempt = 1; attempt <= MaximumMetadataFenceAttempts; attempt++)
        {
            RatingRankingMutationPreparation preparation =
                await rankingMutationGuard.PrepareMutationAsync(
                    targetType,
                    currentMetadata?.ParkItemCategory,
                    retainedCategory,
                    cancellationToken);
            if (targetType != RatingTargetType.ParkItem)
            {
                return new RatingRankingPreparedMutation(currentMetadata, preparation);
            }

            RatingTargetMetadataResult? authoritativeMetadata;
            try
            {
                authoritativeMetadata = await RatingTargetMetadataResolver.ResolveAsync(
                    targetType,
                    targetId,
                    parkRepository,
                    parkItemRepository,
                    cancellationToken);
            }
            catch
            {
                await rankingMutationGuard.CompleteMutationAsync(
                    preparation,
                    sourceChanged: false,
                    CancellationToken.None);
                throw;
            }

            if (HasEquivalentRankingMetadata(currentMetadata, authoritativeMetadata))
            {
                return new RatingRankingPreparedMutation(authoritativeMetadata, preparation);
            }

            await rankingMutationGuard.CompleteMutationAsync(
                preparation,
                sourceChanged: false,
                CancellationToken.None);
            currentMetadata = authoritativeMetadata;
        }

        return null;
    }

    public static Task CompleteAsync(
        RatingRankingMutationPreparation preparation,
        bool sourceChanged,
        IRatingRankingMutationGuard rankingMutationGuard)
    {
        return rankingMutationGuard.CompleteMutationAsync(
            preparation,
            sourceChanged,
            CancellationToken.None);
    }

    private static bool HasEquivalentRankingMetadata(
        RatingTargetMetadataResult? observed,
        RatingTargetMetadataResult? authoritative)
    {
        if (observed is null || authoritative is null)
        {
            return observed is null && authoritative is null;
        }

        return observed.TargetType == authoritative.TargetType
            && string.Equals(observed.TargetId, authoritative.TargetId, StringComparison.Ordinal)
            && string.Equals(observed.ParkId, authoritative.ParkId, StringComparison.Ordinal)
            && observed.ParkItemCategory == authoritative.ParkItemCategory
            && observed.ParkItemType == authoritative.ParkItemType
            && observed.CanReceiveVisitorRatings == authoritative.CanReceiveVisitorRatings;
    }
}

internal sealed record RatingRankingPreparedMutation(
    RatingTargetMetadataResult? Metadata,
    RatingRankingMutationPreparation Preparation);
