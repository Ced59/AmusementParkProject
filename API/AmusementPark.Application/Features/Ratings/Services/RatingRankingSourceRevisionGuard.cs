using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

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

    public RatingRankingSourceRevisionGuard(
        IRankingScopeRegistry scopeRegistry,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository)
    {
        this.scopeRegistry = scopeRegistry;
        this.sourceRevisionRepository = sourceRevisionRepository;
    }

    public async Task PrepareMutationAsync(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RankingScopeDefinition> affectedScopes = this.scopeRegistry.Definitions
            .Where(definition => definition.IsAffectedByRatingMutation(targetType, parkItemCategory))
            .OrderBy(static definition => definition.Key.Value, StringComparer.Ordinal)
            .ToArray();

        foreach (RankingScopeDefinition scope in affectedScopes)
        {
            await this.sourceRevisionRepository.IncrementAsync(scope.Key, cancellationToken);
        }
    }
}
