using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Ports;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Fait converger les trois niveaux de cache public après la publication ou
/// le retrait d'un snapshot de classement.
/// </summary>
public sealed class RatingRankingPublicationCacheInvalidator : IRatingRankingPublicationCacheInvalidator
{
    private readonly IRatingRankProvider ratingRankProvider;
    private readonly IOutputCacheStore outputCacheStore;
    private readonly ISsrPageCacheInvalidator ssrPageCacheInvalidator;
    private readonly ILogger<RatingRankingPublicationCacheInvalidator> logger;

    public RatingRankingPublicationCacheInvalidator(
        IRatingRankProvider ratingRankProvider,
        IOutputCacheStore outputCacheStore,
        ISsrPageCacheInvalidator ssrPageCacheInvalidator,
        ILogger<RatingRankingPublicationCacheInvalidator> logger)
    {
        this.ratingRankProvider = ratingRankProvider;
        this.outputCacheStore = outputCacheStore;
        this.ssrPageCacheInvalidator = ssrPageCacheInvalidator;
        this.logger = logger;
    }

    public async Task<bool> InvalidateAsync(CancellationToken cancellationToken)
    {
        bool succeeded = true;

        try
        {
            this.ratingRankProvider.Invalidate();
        }
        catch (Exception exception)
        {
            succeeded = false;
            this.logger.LogWarning(exception, "Rating rank memory cache invalidation failed.");
        }

        try
        {
            await this.outputCacheStore.EvictByTagAsync(
                ApiOutputCachePolicyNames.PublicRatingDataTag,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            succeeded = false;
            this.logger.LogWarning(exception, "Rating ranking API output cache invalidation failed.");
        }

        try
        {
            bool ssrSucceeded = await this.ssrPageCacheInvalidator.TryInvalidateAsync(
                SsrPageCacheInvalidationRequest.RatingRankingPages(),
                cancellationToken);
            if (!ssrSucceeded)
            {
                succeeded = false;
                this.logger.LogWarning("Rating ranking SSR page cache invalidation was not confirmed.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            succeeded = false;
            this.logger.LogWarning(exception, "Rating ranking SSR page cache invalidation failed.");
        }

        return succeeded;
    }
}
