using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Ratings.Services;

/// <summary>
/// Invalide, après une mutation de note réussie, la révision source de chaque scope affecté.
/// La planification reste désactivée tant que son handler durable n'est pas livré.
/// </summary>
public sealed class RatingRankingSourceRevisionNotifier : IRatingRankingMutationNotifier
{
    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly ILogger<RatingRankingSourceRevisionNotifier> logger;

    public RatingRankingSourceRevisionNotifier(
        IRankingScopeRegistry scopeRegistry,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        ILogger<RatingRankingSourceRevisionNotifier> logger)
    {
        this.scopeRegistry = scopeRegistry;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.logger = logger;
    }

    public async Task NotifyMutationAsync(
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
            try
            {
                await this.sourceRevisionRepository.IncrementAsync(scope.Key, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "The committed rating mutation could not invalidate ranking scope {RankingScopeKey}.",
                    scope.Key.Value);
            }
        }
    }
}
