using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class SharePublicationSourceCacheInvalidator
{
    private readonly ISharePublicationRepository repository;
    private readonly ISharePublicationCacheInvalidationQueue? invalidationQueue;
    private readonly ILogger<SharePublicationSourceCacheInvalidator> logger;

    public SharePublicationSourceCacheInvalidator(
        ISharePublicationRepository repository,
        ILogger<SharePublicationSourceCacheInvalidator> logger,
        ISharePublicationCacheInvalidationQueue? invalidationQueue = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.invalidationQueue = invalidationQueue;
    }

    public async Task InvalidateVisitDeletionAsync(
        string ownerUserId,
        string visitId,
        int? visitYear,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        string normalizedVisitId = visitId?.Trim() ?? string.Empty;
        if (normalizedOwnerUserId.Length == 0 || normalizedVisitId.Length == 0)
        {
            return;
        }

        try
        {
            List<(SharePublicationType Type, string ScopeKey)> sources =
                new List<(SharePublicationType Type, string ScopeKey)>
                {
                    (
                        SharePublicationType.VisitRecap,
                        VisitRecapShareSourceScope.Create(normalizedOwnerUserId, normalizedVisitId)),
                    (
                        SharePublicationType.PassportProfile,
                        PassportProfileShareSourceScope.Create(normalizedOwnerUserId)),
                    (
                        SharePublicationType.PersonalRanking,
                        PersonalRankingShareSourceScope.Create(normalizedOwnerUserId)),
                };
            if (visitYear.HasValue)
            {
                sources.Add((
                    SharePublicationType.YearRecap,
                    YearRecapShareSourceScope.Create(normalizedOwnerUserId, visitYear.Value)));
            }

            foreach ((SharePublicationType type, string scopeKey) in sources)
            {
                SharePublication? publication = await this.repository.GetOwnedBySourceAsync(
                    normalizedOwnerUserId,
                    type,
                    scopeKey,
                    cancellationToken);
                if (publication?.ShareToken is null)
                {
                    continue;
                }

                SharePublicationCacheInvalidationDispatcher.Enqueue(
                    this.invalidationQueue,
                    publication,
                    publication.ShareToken.Value.Value);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Unable to schedule share cache invalidation after deleting visit {VisitId} for {OwnerUserId}.",
                normalizedVisitId,
                normalizedOwnerUserId);
        }
    }
}
