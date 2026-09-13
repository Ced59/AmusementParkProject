using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

/// <summary>
/// Coupe puis purge le périmètre de partage d'un compte déjà verrouillé par le coordinateur
/// de suppression. La coupure publique précède toujours la première suppression physique.
/// </summary>
public sealed class ShareAccountDeletionService : IShareAccountDeletionService
{
    private const int MaximumWriteAttempts = 5;
    private const int ComparisonPageSize = 100;

    private readonly ISharePublicationRepository publicationRepository;
    private readonly IProfileComparisonInvitationRepository invitationRepository;
    private readonly IProfileComparisonRepository comparisonRepository;
    private readonly IShareAccountDeletionStore deletionStore;
    private readonly SharePublicationCacheInvalidationScheduler invalidationScheduler;
    private readonly IShareSocialImageCacheInvalidator socialImageCacheInvalidator;
    private readonly ISsrPageCacheInvalidator ssrPageCacheInvalidator;
    private readonly TimeProvider timeProvider;

    public ShareAccountDeletionService(
        ISharePublicationRepository publicationRepository,
        IProfileComparisonInvitationRepository invitationRepository,
        IProfileComparisonRepository comparisonRepository,
        IShareAccountDeletionStore deletionStore,
        SharePublicationCacheInvalidationScheduler invalidationScheduler,
        IShareSocialImageCacheInvalidator socialImageCacheInvalidator,
        ISsrPageCacheInvalidator ssrPageCacheInvalidator,
        TimeProvider? timeProvider = null)
    {
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.invitationRepository = invitationRepository
            ?? throw new ArgumentNullException(nameof(invitationRepository));
        this.comparisonRepository = comparisonRepository
            ?? throw new ArgumentNullException(nameof(comparisonRepository));
        this.deletionStore = deletionStore
            ?? throw new ArgumentNullException(nameof(deletionStore));
        this.invalidationScheduler = invalidationScheduler
            ?? throw new ArgumentNullException(nameof(invalidationScheduler));
        this.socialImageCacheInvalidator = socialImageCacheInvalidator
            ?? throw new ArgumentNullException(nameof(socialImageCacheInvalidator));
        this.ssrPageCacheInvalidator = ssrPageCacheInvalidator
            ?? throw new ArgumentNullException(nameof(ssrPageCacheInvalidator));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ShareAccountDeletionResult>> DeleteAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        DateTime deletedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        List<string> invalidatedShareIds = new List<string>();

        IReadOnlyCollection<SharePublication> publications =
            await this.publicationRepository.ListOwnedAsync(
                normalizedOwnerUserId,
                cancellationToken);
        int revokedPublicationCount = 0;
        foreach (SharePublication publication in publications)
        {
            if (publication.Status is SharePublicationStatus.Draft
                or SharePublicationStatus.Revoked)
            {
                continue;
            }

            string? shareId = publication.ShareToken?.Value;
            bool revoked = await this.RevokePublicationAsync(
                publication,
                deletedAtUtc,
                cancellationToken);
            if (!revoked)
            {
                return Conflict();
            }

            revokedPublicationCount++;
            if (!string.IsNullOrWhiteSpace(shareId))
            {
                invalidatedShareIds.Add(shareId);
            }
        }

        int revokedComparisonCount = await this.RevokeComparisonsAsync(
            normalizedOwnerUserId,
            deletedAtUtc,
            invalidatedShareIds,
            cancellationToken);
        if (revokedComparisonCount < 0)
        {
            return Conflict();
        }

        long expiredInvitationCount =
            await this.invitationRepository.DeletePendingCreatedByAsync(
                normalizedOwnerUserId,
                cancellationToken);

        string[] distinctShareIds = invalidatedShareIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        this.socialImageCacheInvalidator.Invalidate(distinctShareIds);
        await this.ssrPageCacheInvalidator.InvalidateAllAsync(cancellationToken);

        long purgedDocumentCount = await this.deletionStore.PurgeAsync(
            normalizedOwnerUserId,
            cancellationToken);
        return ApplicationResult<ShareAccountDeletionResult>.Success(
            new ShareAccountDeletionResult(
                revokedPublicationCount,
                expiredInvitationCount,
                revokedComparisonCount,
                purgedDocumentCount));
    }

    private async Task<bool> RevokePublicationAsync(
        SharePublication initialPublication,
        DateTime deletedAtUtc,
        CancellationToken cancellationToken)
    {
        SharePublication publication = initialPublication;
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            if (publication.Status is SharePublicationStatus.Draft
                or SharePublicationStatus.Revoked)
            {
                return true;
            }

            string? shareId = publication.ShareToken?.Value;
            long expectedVersion = publication.Version;
            DateTime revokedAtUtc = publication.UpdatedAtUtc > deletedAtUtc
                ? publication.UpdatedAtUtc
                : deletedAtUtc;
            publication.Revoke(publication.PublicationVersion, revokedAtUtc);
            await this.invalidationScheduler.ScheduleAsync(
                publication,
                publication.Version,
                cancellationToken,
                shareId);
            SharePublicationWriteOutcome outcome =
                await this.publicationRepository.ReplaceAsync(
                    publication,
                    expectedVersion,
                    cancellationToken);
            if (outcome == SharePublicationWriteOutcome.Success)
            {
                return true;
            }

            SharePublication? current = await this.publicationRepository.GetOwnedAsync(
                publication.Id,
                publication.OwnerUserId,
                cancellationToken);
            if (current is null)
            {
                return true;
            }

            publication = current;
        }

        return false;
    }

    private async Task<int> RevokeComparisonsAsync(
        string ownerUserId,
        DateTime deletedAtUtc,
        ICollection<string> invalidatedShareIds,
        CancellationToken cancellationToken)
    {
        int revokedCount = 0;
        ProfileComparisonListCursor? cursor = null;
        while (true)
        {
            IReadOnlyCollection<ProfileComparison> comparisons =
                await this.comparisonRepository.ListActiveByParticipantAsync(
                    ownerUserId,
                    cursor,
                    ComparisonPageSize,
                    cancellationToken);
            if (comparisons.Count == 0)
            {
                return revokedCount;
            }

            foreach (ProfileComparison comparison in comparisons)
            {
                invalidatedShareIds.Add(comparison.ShareToken.Value);
                bool revoked = await this.RevokeComparisonAsync(
                    comparison,
                    ownerUserId,
                    deletedAtUtc,
                    cancellationToken);
                if (!revoked)
                {
                    return -1;
                }

                revokedCount++;
            }

            if (comparisons.Count < ComparisonPageSize)
            {
                return revokedCount;
            }

            ProfileComparison last = comparisons.Last();
            cursor = new ProfileComparisonListCursor(last.CreatedAtUtc, last.Id.Value);
        }
    }

    private async Task<bool> RevokeComparisonAsync(
        ProfileComparison initialComparison,
        string ownerUserId,
        DateTime deletedAtUtc,
        CancellationToken cancellationToken)
    {
        ProfileComparison comparison = initialComparison;
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            if (!comparison.IsActive)
            {
                return true;
            }

            long expectedVersion = comparison.Version;
            DateTime revokedAtUtc = comparison.UpdatedAtUtc > deletedAtUtc
                ? comparison.UpdatedAtUtc
                : deletedAtUtc;
            comparison.Revoke(ownerUserId, revokedAtUtc);
            await this.invalidationScheduler.ScheduleProfileComparisonAsync(
                comparison,
                cancellationToken);
            ProfileComparisonWriteOutcome outcome =
                await this.comparisonRepository.ReplaceAsync(
                    comparison,
                    expectedVersion,
                    cancellationToken);
            if (outcome == ProfileComparisonWriteOutcome.Success)
            {
                return true;
            }

            ProfileComparison? current = await this.comparisonRepository.GetByIdAsync(
                comparison.Id,
                cancellationToken);
            if (current is null)
            {
                return true;
            }

            comparison = current;
        }

        return false;
    }

    private static ApplicationResult<ShareAccountDeletionResult> Conflict()
    {
        return ApplicationResult<ShareAccountDeletionResult>.Failure(
            SharingApplicationErrors.AccountShareDeletionConflict());
    }
}
