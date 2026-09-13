using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class SharePublicationSourceCacheInvalidator
{
    private readonly ISharePublicationRepository repository;
    private readonly SharePublicationCacheInvalidationScheduler scheduler;

    public SharePublicationSourceCacheInvalidator(
        ISharePublicationRepository repository,
        SharePublicationCacheInvalidationScheduler scheduler)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
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

            await this.scheduler.ScheduleAsync(
                publication,
                publication.Version,
                cancellationToken,
                publication.ShareToken.Value.Value);
        }
    }
}
