using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Ratings.Services;

/// <summary>
/// Incrémente durablement les révisions des scopes avant une mutation de note.
/// Sur MongoDB standalone, cette écriture anticipée peut provoquer une reconstruction superflue,
/// mais empêche qu'une mutation validée soit invisible pour le futur réconciliateur.
/// </summary>
public sealed class RatingRankingSourceRevisionGuard : IRatingRankingMutationGuard
{
    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IRatingRankingRebuildScheduler rebuildScheduler;
    private readonly ILogger<RatingRankingSourceRevisionGuard> logger;

    public RatingRankingSourceRevisionGuard(
        IRankingScopeRegistry scopeRegistry,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IRatingRankingRebuildScheduler rebuildScheduler,
        ILogger<RatingRankingSourceRevisionGuard> logger)
    {
        this.scopeRegistry = scopeRegistry;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.rebuildScheduler = rebuildScheduler;
        this.logger = logger;
    }

    public async Task<RatingRankingMutationPreparation> PrepareMutationAsync(
        RatingTargetType targetType,
        ParkItemCategory? currentParkItemCategory,
        ParkItemCategory? previousParkItemCategory,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItemCategory?> affectedCategories = new[]
        {
            currentParkItemCategory,
            previousParkItemCategory,
        }
            .Distinct()
            .ToArray();
        IReadOnlyCollection<RankingScopeDefinition> affectedScopes = this.scopeRegistry.Definitions
            .Where(definition => affectedCategories.Any(
                category => definition.IsAffectedByRatingMutation(targetType, category)))
            .OrderBy(static definition => definition.Key.Value, StringComparer.Ordinal)
            .ToArray();

        List<RatingRankingSourceRevision> sourceRevisions = new List<RatingRankingSourceRevision>();
        foreach (RankingScopeDefinition scope in affectedScopes)
        {
            RatingRankingSourceRevision sourceRevision = await this.sourceRevisionRepository.IncrementAsync(
                scope.Key,
                cancellationToken);
            sourceRevisions.Add(sourceRevision);
        }

        return new RatingRankingMutationPreparation(sourceRevisions);
    }

    public async Task ScheduleRebuildsAsync(
        RatingRankingMutationPreparation preparation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        foreach (RatingRankingSourceRevision sourceRevision in preparation.SourceRevisions)
        {
            try
            {
                await this.rebuildScheduler.ScheduleAsync(sourceRevision, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to schedule the recoverable ranking rebuild for scope {ScopeKey} at revision {SourceRevision}.",
                    sourceRevision.ScopeKey.Value,
                    sourceRevision.Revision);
            }
        }
    }
}
