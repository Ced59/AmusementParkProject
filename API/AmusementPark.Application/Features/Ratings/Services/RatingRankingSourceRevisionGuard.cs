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
        string targetId,
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
        RatingRankingMutationRecoveryTarget? recoveryTarget = targetType == RatingTargetType.ParkItem
            ? new RatingRankingMutationRecoveryTarget(targetType, targetId)
            : null;
        return await this.PrepareScopesAsync(
            affectedScopes,
            recoveryTarget,
            cancellationToken);
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
        bool affectsAllRankingSources = false;
        bool affectsParkRankingSources = false;
        foreach (string parkId in previousById.Keys
                     .Concat(currentById.Keys)
                     .Distinct(StringComparer.Ordinal))
        {
            previousById.TryGetValue(parkId, out Park? previous);
            currentById.TryGetValue(parkId, out Park? current);
            bool previousIncluded = IsParkIncluded(previous);
            bool currentIncluded = IsParkIncluded(current);
            if (previousIncluded != currentIncluded)
            {
                affectsAllRankingSources = true;
                break;
            }

            if (previousIncluded
                && currentIncluded
                && !NamesHaveEquivalentRankingOrder(previous!.Name, current!.Name))
            {
                affectsParkRankingSources = true;
            }
        }

        IReadOnlyCollection<RankingScopeDefinition> affectedScopes = this.scopeRegistry.Definitions
            .Where(definition => affectsAllRankingSources
                || (affectsParkRankingSources
                    && definition.TargetFamily == RankingTargetFamily.Parks))
            .ToArray();
        return await this.PrepareScopesAsync(affectedScopes, null, cancellationToken);
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
        bool affectsParkRankingSources = false;
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
            bool parkCompositionChanged = previousIncluded
                && currentIncluded
                && previous!.Type != current!.Type;
            bool rankingNameChanged = previousIncluded
                && currentIncluded
                && !NamesHaveEquivalentRankingOrder(previous!.Name, current!.Name);
            if (!membershipChanged
                && !placementChanged
                && !parkCompositionChanged
                && !rankingNameChanged)
            {
                continue;
            }

            if (membershipChanged || placementChanged || parkCompositionChanged)
            {
                affectsParkRankingSources = true;
            }

            if (membershipChanged || placementChanged || rankingNameChanged)
            {
                if (previousIncluded)
                {
                    affectedCategories.Add(previous!.Category);
                }

                if (currentIncluded)
                {
                    affectedCategories.Add(current!.Category);
                }
            }
        }

        IReadOnlyCollection<RankingScopeDefinition> affectedScopes = affectedCategories.Count == 0
            && !affectsParkRankingSources
            ? Array.Empty<RankingScopeDefinition>()
            : this.scopeRegistry.Definitions
                .Where(definition => (affectsParkRankingSources
                        && definition.TargetFamily == RankingTargetFamily.Parks)
                    || (definition.Filter.ParkItemCategory.HasValue
                        && affectedCategories.Contains(definition.Filter.ParkItemCategory.Value)))
                .ToArray();
        return await this.PrepareScopesAsync(affectedScopes, null, cancellationToken);
    }

    private async Task<RatingRankingMutationPreparation> PrepareScopesAsync(
        IReadOnlyCollection<RankingScopeDefinition> affectedScopes,
        RatingRankingMutationRecoveryTarget? recoveryTarget,
        CancellationToken cancellationToken)
    {
        List<RatingRankingMutationLease> mutationLeases = new List<RatingRankingMutationLease>();
        try
        {
            foreach (RankingScopeDefinition scope in affectedScopes
                         .OrderBy(static definition => definition.Key.Value, StringComparer.Ordinal))
            {
                RatingRankingMutationLease mutationLease = recoveryTarget is not null
                    && scope.Key == CanonicalRankingScopes.GlobalParks.Key
                    ? await this.sourceRevisionRepository.BeginMutationAsync(
                        scope.Key,
                        recoveryTarget,
                        cancellationToken)
                    : await this.sourceRevisionRepository.BeginMutationAsync(
                        scope.Key,
                        cancellationToken);
                mutationLeases.Add(mutationLease);
            }
        }
        catch
        {
            await this.CompleteMutationAsync(
                new RatingRankingMutationPreparation(mutationLeases),
                sourceChanged: false,
                CancellationToken.None);
            throw;
        }

        return new RatingRankingMutationPreparation(mutationLeases);
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

    private static bool NamesHaveEquivalentRankingOrder(string? previousName, string? currentName)
    {
        return string.Equals(
            previousName?.Trim(),
            currentName?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task CompleteMutationAsync(
        RatingRankingMutationPreparation preparation,
        bool sourceChanged,
        CancellationToken cancellationToken)
    {
        await this.CompleteMutationAsync(
            preparation,
            _ => sourceChanged,
            cancellationToken);
    }

    private async Task CompleteMutationAsync(
        RatingRankingMutationPreparation preparation,
        Func<RankingScopeKey, bool> sourceChangedByScope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        ArgumentNullException.ThrowIfNull(sourceChangedByScope);
        List<RatingRankingSourceRevision> rebuildableRevisions = new List<RatingRankingSourceRevision>();
        foreach (RatingRankingMutationLease mutationLease in preparation.MutationLeases)
        {
            try
            {
                RatingRankingSourceRevision sourceRevision =
                    await this.sourceRevisionRepository.CompleteMutationAsync(
                        mutationLease,
                        sourceChangedByScope(mutationLease.ScopeKey),
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
                    mutationLease.ScopeKey.Value);
            }
        }

        foreach (RatingRankingSourceRevision sourceRevision in rebuildableRevisions)
        {
            try
            {
                await this.rebuildScheduler.ScheduleIfOutstandingAsync(
                    sourceRevision,
                    cancellationToken);
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
