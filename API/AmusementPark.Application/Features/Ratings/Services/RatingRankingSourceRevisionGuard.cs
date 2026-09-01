using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Ratings.Services;

/// <summary>
/// Réserve durablement les scopes avant une mutation de note ou de métadonnées,
/// puis ne rend leur nouvelle révision reconstructible qu'après la fin de l'écriture.
/// Une expiration de lease récupère conservativement les mutations interrompues sur MongoDB standalone.
/// </summary>
public sealed class RatingRankingSourceRevisionGuard :
    IRatingRankingMutationGuard,
    IRatingRankingSourceChangeCoordinator
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

        return await this.PrepareScopesAsync(affectedScopes, cancellationToken);
    }

    public async Task<RatingRankingMutationPreparation> PrepareParkChangesAsync(
        IReadOnlyCollection<Park> previousParks,
        IReadOnlyCollection<Park> currentParks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(previousParks);
        ArgumentNullException.ThrowIfNull(currentParks);
        IReadOnlyDictionary<string, Park> previousById = IndexParks(previousParks);
        IReadOnlyDictionary<string, Park> currentById = IndexParks(currentParks);
        bool affectsRankingSources = previousById.Keys
            .Concat(currentById.Keys)
            .Distinct(StringComparer.Ordinal)
            .Any(parkId => IsParkIncluded(previousById.GetValueOrDefault(parkId))
                != IsParkIncluded(currentById.GetValueOrDefault(parkId)));
        IReadOnlyCollection<RankingScopeDefinition> affectedScopes = affectsRankingSources
            ? this.scopeRegistry.Definitions
            : Array.Empty<RankingScopeDefinition>();
        return await this.PrepareScopesAsync(affectedScopes, cancellationToken);
    }

    public async Task<RatingRankingMutationPreparation> PrepareParkItemChangesAsync(
        IReadOnlyCollection<ParkItem> previousItems,
        IReadOnlyCollection<ParkItem> currentItems,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(previousItems);
        ArgumentNullException.ThrowIfNull(currentItems);
        IReadOnlyDictionary<string, ParkItem> previousById = IndexParkItems(previousItems);
        IReadOnlyDictionary<string, ParkItem> currentById = IndexParkItems(currentItems);
        HashSet<ParkItemCategory> affectedCategories = new HashSet<ParkItemCategory>();
        foreach (string itemId in previousById.Keys
                     .Concat(currentById.Keys)
                     .Distinct(StringComparer.Ordinal))
        {
            previousById.TryGetValue(itemId, out ParkItem? previous);
            currentById.TryGetValue(itemId, out ParkItem? current);
            bool previousIncluded = IsParkItemIncluded(previous);
            bool currentIncluded = IsParkItemIncluded(current);
            bool membershipChanged = previousIncluded != currentIncluded;
            bool placementChanged = previousIncluded
                && currentIncluded
                && (previous!.Category != current!.Category
                    || !string.Equals(previous.ParkId, current.ParkId, StringComparison.Ordinal));
            if (!membershipChanged && !placementChanged)
            {
                continue;
            }

            if (previousIncluded)
            {
                affectedCategories.Add(previous!.Category);
            }

            if (currentIncluded)
            {
                affectedCategories.Add(current!.Category);
            }
        }

        IReadOnlyCollection<RankingScopeDefinition> affectedScopes = affectedCategories.Count == 0
            ? Array.Empty<RankingScopeDefinition>()
            : this.scopeRegistry.Definitions
                .Where(definition => definition.TargetFamily == RankingTargetFamily.Parks
                    || (definition.Filter.ParkItemCategory.HasValue
                        && affectedCategories.Contains(definition.Filter.ParkItemCategory.Value)))
                .ToArray();
        return await this.PrepareScopesAsync(affectedScopes, cancellationToken);
    }

    private async Task<RatingRankingMutationPreparation> PrepareScopesAsync(
        IReadOnlyCollection<RankingScopeDefinition> affectedScopes,
        CancellationToken cancellationToken)
    {
        List<RankingScopeKey> begunScopes = new List<RankingScopeKey>();
        try
        {
            foreach (RankingScopeDefinition scope in affectedScopes
                         .OrderBy(static definition => definition.Key.Value, StringComparer.Ordinal))
            {
                await this.sourceRevisionRepository.BeginMutationAsync(
                    scope.Key,
                    cancellationToken);
                begunScopes.Add(scope.Key);
            }
        }
        catch
        {
            await this.CompleteMutationAsync(
                new RatingRankingMutationPreparation(begunScopes),
                sourceChanged: false,
                CancellationToken.None);
            throw;
        }

        return new RatingRankingMutationPreparation(begunScopes);
    }

    private static IReadOnlyDictionary<string, Park> IndexParks(
        IEnumerable<Park> parks)
    {
        return parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Last(), StringComparer.Ordinal);
    }

    private static IReadOnlyDictionary<string, ParkItem> IndexParkItems(
        IEnumerable<ParkItem> items)
    {
        return items
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
            .GroupBy(static item => item.Id!, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Last(), StringComparer.Ordinal);
    }

    private static bool IsParkIncluded(Park? park)
    {
        return park is not null
            && park.IsVisible
            && park.Status.CanAppearInCurrentRatingRankings();
    }

    private static bool IsParkItemIncluded(ParkItem? item)
    {
        return item is not null
            && item.IsVisible
            && ParkItemStatusNormalizer.CanAppearInCurrentRatingRankings(
                item.Category,
                item.AttractionDetails?.Status);
    }

    public async Task CompleteMutationAsync(
        RatingRankingMutationPreparation preparation,
        bool sourceChanged,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        List<RatingRankingSourceRevision> rebuildableRevisions = new List<RatingRankingSourceRevision>();
        foreach (RankingScopeKey scopeKey in preparation.ScopeKeys)
        {
            try
            {
                RatingRankingSourceRevision sourceRevision =
                    await this.sourceRevisionRepository.CompleteMutationAsync(
                        scopeKey,
                        sourceChanged,
                        cancellationToken);
                if (sourceRevision.IsRebuildable && sourceRevision.Revision > 0)
                {
                    rebuildableRevisions.Add(sourceRevision);
                }
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to settle the ranking source mutation for scope {ScopeKey}; its durable lease will be recovered.",
                    scopeKey.Value);
            }
        }

        foreach (RatingRankingSourceRevision sourceRevision in rebuildableRevisions)
        {
            try
            {
                await this.rebuildScheduler.ScheduleAsync(sourceRevision, cancellationToken);
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
