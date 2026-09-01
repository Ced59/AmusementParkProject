using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RatingRankingRebuildScheduler : IRatingRankingRebuildScheduler
{
    private readonly IDurableBackgroundJobRepository backgroundJobRepository;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRankingSnapshotRepository snapshotRepository;
    private readonly IRankingScopeRegistry scopeRegistry;

    public RatingRankingRebuildScheduler(
        IDurableBackgroundJobRepository backgroundJobRepository,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRankingSnapshotRepository snapshotRepository,
        IRankingScopeRegistry scopeRegistry)
    {
        this.backgroundJobRepository = backgroundJobRepository;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.snapshotRepository = snapshotRepository;
        this.scopeRegistry = scopeRegistry;
    }

    public async Task ScheduleIfOutstandingAsync(
        RatingRankingSourceRevision sourceRevision,
        CancellationToken cancellationToken)
    {
        RankingScopeDefinition? scope = this.scopeRegistry.Definitions
            .SingleOrDefault(definition => definition.Key == sourceRevision.ScopeKey);
        if (scope is null)
        {
            throw new InvalidOperationException(
                $"The ranking scope '{sourceRevision.ScopeKey.Value}' is not registered.");
        }

        if (!sourceRevision.IsRebuildable)
        {
            return;
        }

        bool isCovered = await this.IsCoveredAsync(
            scope,
            sourceRevision,
            sourceRevision.Revision,
            cancellationToken);
        if (isCovered)
        {
            return;
        }

        await this.EnqueueAsync(scope, sourceRevision.Revision, cancellationToken);
    }

    public async Task ScheduleOutstandingAsync(CancellationToken cancellationToken)
    {
        foreach (RankingScopeDefinition scope in this.scopeRegistry.Definitions)
        {
            RatingRankingSourceRevision? sourceRevision = await this.sourceRevisionRepository.GetAsync(
                scope.Key,
                cancellationToken);
            if (sourceRevision is not null && !sourceRevision.IsRebuildable)
            {
                continue;
            }

            long requestedRevision = sourceRevision?.Revision ?? 0;
            bool isCovered = await this.IsCoveredAsync(
                scope,
                sourceRevision,
                requestedRevision,
                cancellationToken);
            if (!isCovered)
            {
                await this.EnqueueAsync(scope, requestedRevision, cancellationToken);
            }
        }
    }

    private async Task<bool> IsCoveredAsync(
        RankingScopeDefinition scope,
        RatingRankingSourceRevision? sourceRevision,
        long requestedRevision,
        CancellationToken cancellationToken)
    {
        if (sourceRevision?.CoversUnavailable(
                scope.MethodologyVersion,
                requestedRevision) == true)
        {
            return true;
        }

        RankingPublicationPointer? pointer = await this.snapshotRepository.GetPointerAsync(
            scope.Key,
            cancellationToken);
        return pointer is not null
            && pointer.MethodologyVersion == scope.MethodologyVersion
            && pointer.HighestPublishedSourceRevision >= requestedRevision;
    }

    private async Task EnqueueAsync(
        RankingScopeDefinition scope,
        long requestedRevision,
        CancellationToken cancellationToken)
    {
        RatingRankingRebuildScopePayload payload = new RatingRankingRebuildScopePayload(
            scope.Key.Value,
            requestedRevision,
            scope.MethodologyVersion.Value);
        JsonElement serializedPayload = JsonSerializer.SerializeToElement(payload);
        CoalesceBackgroundJobRequest request = new CoalesceBackgroundJobRequest(
            RatingRankingRebuildScopeJob.Kind,
            RatingRankingRebuildScopeJob.BuildNaturalKey(scope.Key),
            requestedRevision,
            RatingRankingRebuildScopeJob.PayloadVersion,
            serializedPayload);
        await this.backgroundJobRepository.CoalesceAsync(request, cancellationToken);
    }
}
