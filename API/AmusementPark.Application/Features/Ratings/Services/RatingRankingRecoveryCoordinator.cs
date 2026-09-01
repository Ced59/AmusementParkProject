using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingRecoveryCoordinator : IRatingRankingRecoveryCoordinator
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRatingRankingRebuildScheduler rebuildScheduler;
    private readonly ILogger<RatingRankingRecoveryCoordinator> logger;

    public RatingRankingRecoveryCoordinator(
        IParkItemRepository parkItemRepository,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRatingRankingRebuildScheduler rebuildScheduler,
        ILogger<RatingRankingRecoveryCoordinator> logger)
    {
        this.parkItemRepository = parkItemRepository;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.rebuildScheduler = rebuildScheduler;
        this.logger = logger;
    }

    public async Task ReconcileRecoveredParkItemMutationsAsync(
        CancellationToken cancellationToken)
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RatingRankingSourceRevision? globalRevision = await this.sourceRevisionRepository.GetAsync(
            globalScopeKey,
            cancellationToken);
        IReadOnlyCollection<string> recoveredTargetIds =
            globalRevision?.RecoveredParkItemTargetIds ?? Array.Empty<string>();
        foreach (string targetId in recoveredTargetIds)
        {
            try
            {
                ParkItem? parkItem = await this.parkItemRepository.GetByIdAsync(
                    targetId,
                    false,
                    cancellationToken);
                RankingScopeDefinition? categoryScope = parkItem is null
                    ? null
                    : CanonicalRankingScopes.PublicItemCategories.SingleOrDefault(
                        definition => definition.Filter.ParkItemCategory == parkItem.Category);
                if (categoryScope is not null)
                {
                    RatingRankingMutationLease lease =
                        await this.sourceRevisionRepository.BeginMutationAsync(
                            categoryScope.Key,
                            cancellationToken);
                    RatingRankingSourceRevision categoryRevision =
                        await this.sourceRevisionRepository.CompleteMutationAsync(
                            lease,
                            sourceChanged: true,
                            cancellationToken);
                    if (categoryRevision.IsRebuildable)
                    {
                        await this.rebuildScheduler.ScheduleIfOutstandingAsync(
                            categoryRevision,
                            cancellationToken);
                    }
                }

                await this.sourceRevisionRepository.AcknowledgeRecoveredParkItemTargetAsync(
                    globalScopeKey,
                    targetId,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to reconcile recovered park-item ranking mutation for target {TargetId}.",
                    targetId);
            }
        }
    }
}
