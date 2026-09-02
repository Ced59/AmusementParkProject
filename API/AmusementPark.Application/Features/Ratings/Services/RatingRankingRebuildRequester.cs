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
            RatingRankingSourceRevision sourceRevision =
                await this.GetOrCreateSourceRevisionAsync(scope.Key, cancellationToken);

            RatingRankingRebuildScheduleDisposition disposition =
                await this.rebuildScheduler.ScheduleForcedAsync(
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

    private async Task<RatingRankingSourceRevision> GetOrCreateSourceRevisionAsync(
        RankingScopeKey scopeKey,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceRevision? existing = await this.sourceRevisionRepository.GetAsync(
            scopeKey,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        RatingRankingMutationLease lease = await this.sourceRevisionRepository.BeginMutationAsync(
            scopeKey,
            cancellationToken);
        try
        {
            return await this.sourceRevisionRepository.CompleteMutationAsync(
                lease,
                sourceChanged: false,
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
    }
}
