using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingRebuildRequester
{
    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRatingRankingRebuildScheduler rebuildScheduler;
    private readonly TimeProvider timeProvider;

    public RatingRankingRebuildRequester(
        IRankingScopeRegistry scopeRegistry,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRatingRankingRebuildScheduler rebuildScheduler,
        TimeProvider? timeProvider = null)
    {
        this.scopeRegistry = scopeRegistry;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.rebuildScheduler = rebuildScheduler;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<RatingRankingRebuildRequestResult> RequestRebuildAsync(
        CancellationToken cancellationToken)
    {
        List<RatingRankingScheduledScopeResult> scheduledScopes =
            new List<RatingRankingScheduledScopeResult>();
        foreach (RankingScopeDefinition scope in this.scopeRegistry.Definitions
                     .OrderBy(static definition => definition.Key.Value, StringComparer.Ordinal))
        {
            RatingRankingMutationLease lease = await this.sourceRevisionRepository.BeginMutationAsync(
                scope.Key,
                cancellationToken);
            RatingRankingSourceRevision sourceRevision;
            try
            {
                sourceRevision = await this.sourceRevisionRepository.CompleteMutationAsync(
                    lease,
                    sourceChanged: true,
                    cancellationToken);
            }
            catch
            {
                await this.sourceRevisionRepository.CompleteMutationAsync(
                    lease,
                    sourceChanged: false,
                    CancellationToken.None);
                throw;
            }

            RatingRankingRebuildScheduleDisposition disposition =
                await this.rebuildScheduler.ScheduleIfOutstandingAsync(
                    sourceRevision,
                    cancellationToken);
            if (disposition == RatingRankingRebuildScheduleDisposition.Scheduled)
            {
                scheduledScopes.Add(new RatingRankingScheduledScopeResult(
                    scope.Key.Value,
                    sourceRevision.Revision));
            }
        }

        return new RatingRankingRebuildRequestResult(
            this.timeProvider.GetUtcNow().UtcDateTime,
            scheduledScopes.Count,
            scheduledScopes.AsReadOnly());
    }
}
