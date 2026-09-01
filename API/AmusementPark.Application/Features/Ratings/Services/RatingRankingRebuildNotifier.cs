using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Ratings.Services;

/// <summary>
/// Publie, après une mutation de note réussie, la révision source de chaque scope affecté.
/// La reconstruction lourde reste asynchrone et coalescée dans le worker durable.
/// </summary>
public sealed class RatingRankingRebuildNotifier : IRatingRankingMutationNotifier
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly IRankingScopeRegistry scopeRegistry;
    private readonly IRatingRankingSourceRevisionRepository sourceRevisionRepository;
    private readonly IDurableBackgroundJobRepository backgroundJobRepository;
    private readonly ILogger<RatingRankingRebuildNotifier> logger;

    public RatingRankingRebuildNotifier(
        IRankingScopeRegistry scopeRegistry,
        IRatingRankingSourceRevisionRepository sourceRevisionRepository,
        IDurableBackgroundJobRepository backgroundJobRepository,
        ILogger<RatingRankingRebuildNotifier> logger)
    {
        this.scopeRegistry = scopeRegistry;
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.backgroundJobRepository = backgroundJobRepository;
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
                await this.RequestScopeRebuildAsync(scope, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "The committed rating mutation could not schedule ranking scope {RankingScopeKey}.",
                    scope.Key.Value);
            }
        }
    }

    private async Task RequestScopeRebuildAsync(
        RankingScopeDefinition scope,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceRevision sourceRevision = await this.sourceRevisionRepository.IncrementAsync(
            scope.Key,
            cancellationToken);
        RatingRankingRebuildJobPayload payload = new RatingRankingRebuildJobPayload(
            scope.Key.Value,
            sourceRevision.Revision,
            scope.MethodologyVersion.Value);
        JsonElement payloadJson = JsonSerializer.SerializeToElement(payload, JsonOptions);

        await this.backgroundJobRepository.CoalesceAsync(
            new CoalesceBackgroundJobRequest(
                RatingRankingRebuildJobContract.Kind,
                RatingRankingRebuildJobContract.CreateNaturalKey(scope.Key),
                sourceRevision.Revision,
                RatingRankingRebuildJobContract.PayloadVersion,
                payloadJson),
            cancellationToken);
    }
}
