using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingRecoveryCoordinator : IRatingRankingRecoveryCoordinator
{
    private readonly IRatingRepository ratingRepository;
    private readonly IRatingRankProvider ratingRankProvider;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRatingRankingRebuildScheduler rebuildScheduler;
    private readonly ILogger<RatingRankingRecoveryCoordinator> logger;

    public RatingRankingRecoveryCoordinator(
        IRatingRepository ratingRepository,
        IRatingRankProvider ratingRankProvider,
        IParkItemRepository parkItemRepository,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRatingRankingRebuildScheduler rebuildScheduler,
        ILogger<RatingRankingRecoveryCoordinator> logger)
    {
        this.ratingRepository = ratingRepository;
        this.ratingRankProvider = ratingRankProvider;
        this.parkItemRepository = parkItemRepository;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.rebuildScheduler = rebuildScheduler;
        this.logger = logger;
    }

    public async Task<bool> ReconcileRecoveredRatingMutationsAsync(
        CancellationToken cancellationToken)
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RatingRankingSourceRevision? globalRevision = await this.sourceRevisionRepository.GetAsync(
            globalScopeKey,
            cancellationToken);
        IReadOnlyCollection<RatingRankingRecoveredMutation> recoveredMutations =
            globalRevision?.RecoveredMutations ?? Array.Empty<RatingRankingRecoveredMutation>();
        bool allRecovered = true;
        foreach (RatingRankingRecoveredMutation recoveredMutation in recoveredMutations)
        {
            try
            {
                await this.ratingRepository.RepairAggregateAsync(
                    recoveredMutation.TargetType,
                    recoveredMutation.TargetId,
                    cancellationToken);
                this.ratingRankProvider.Invalidate();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                allRecovered = false;
                this.logger.LogError(
                    exception,
                    "Unable to reconcile recovered rating mutation {RecoveryToken} for {TargetType} target {TargetId}.",
                    recoveredMutation.RecoveryToken,
                    recoveredMutation.TargetType,
                    recoveredMutation.TargetId);
            }
        }

        if (!allRecovered)
        {
            return false;
        }

        foreach (RatingRankingRecoveredMutation recoveredMutation in recoveredMutations)
        {
            try
            {
                if (recoveredMutation.TargetType == RatingTargetType.ParkItem)
                {
                    ParkItem? parkItem = await this.parkItemRepository.GetByIdAsync(
                        recoveredMutation.TargetId,
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
                }

                await this.sourceRevisionRepository.AcknowledgeRecoveredMutationAsync(
                    globalScopeKey,
                    recoveredMutation,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                allRecovered = false;
                this.logger.LogError(
                    exception,
                    "Unable to finalize recovered rating mutation {RecoveryToken} for {TargetType} target {TargetId}.",
                    recoveredMutation.RecoveryToken,
                    recoveredMutation.TargetType,
                    recoveredMutation.TargetId);
            }
        }

        return allRecovered;
    }
}
