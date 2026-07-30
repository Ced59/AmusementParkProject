using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Comments.Services;

public sealed class CommentImageReconciler
{
    private static readonly TimeSpan CleanupClaimLease = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ReconciliationRetryDelay = TimeSpan.FromMinutes(5);
    private readonly ICommentRepository commentRepository;
    private readonly IImageRepository imageRepository;
    private readonly IImageBinaryStorage imageBinaryStorage;

    public CommentImageReconciler(
        ICommentRepository commentRepository,
        IImageRepository imageRepository,
        IImageBinaryStorage imageBinaryStorage)
    {
        this.commentRepository = commentRepository;
        this.imageRepository = imageRepository;
        this.imageBinaryStorage = imageBinaryStorage;
    }

    public async Task<int> ReconcileAsync(
        DateTime dueBeforeUtc,
        DateTime draftCreatedBeforeUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Image> candidates =
            await this.imageRepository.GetCommentImagesRequiringReconciliationAsync(
                dueBeforeUtc,
                draftCreatedBeforeUtc,
                limit,
                cancellationToken);
        int reconciledCount = 0;
        foreach (Image image in candidates)
        {
            bool reconciled = image.OwnerType switch
            {
                ImageOwnerType.CommentDraft => await this.ReconcileDraftAsync(
                    image,
                    dueBeforeUtc,
                    draftCreatedBeforeUtc,
                    cancellationToken),
                ImageOwnerType.Comment => await this.ReconcilePublishedAsync(
                    image,
                    dueBeforeUtc,
                    draftCreatedBeforeUtc,
                    cancellationToken),
                _ => false,
            };
            if (reconciled)
            {
                reconciledCount++;
            }
        }

        return reconciledCount;
    }

    private async Task<bool> ReconcileDraftAsync(
        Image image,
        DateTime dueBeforeUtc,
        DateTime draftCreatedBeforeUtc,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(image.PendingCommentId))
        {
            string draftOwnerId = image.DraftOwnerId ?? image.OwnerId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(draftOwnerId)
                || !image.ReservationReconcileAfterUtc.HasValue)
            {
                return false;
            }

            Comment? comment = await this.commentRepository.GetByIdAsync(
                image.PendingCommentId,
                cancellationToken);
            bool isReferenced =
                comment?.ImageIds.Contains(image.Id, StringComparer.Ordinal)
                == true;
            if (isReferenced)
            {
                Image? finalized =
                    await this.imageRepository.FinalizeCommentDraftAsync(
                        image.Id,
                        draftOwnerId,
                        image.PendingCommentId,
                        image.PendingReservationToken,
                        cancellationToken);
                return finalized is not null;
            }

            bool revisionFenceReached =
                image.PendingCommentRevision.HasValue
                && comment is not null
                && comment.Revision >= image.PendingCommentRevision.Value;
            bool cleanupFenceReached =
                image.CleanupRequestedAtUtc.HasValue
                && image.CleanupRequestedAtUtc.Value <= dueBeforeUtc
                && image.CleanupCommentRevision.HasValue
                && (comment is null
                    || comment.Revision
                        >= image.CleanupCommentRevision.Value);
            bool retentionExpired =
                image.CreatedAtUtc < draftCreatedBeforeUtc;
            bool reservationHardExpired =
                image.PendingReservationExpiresAtUtc.HasValue
                    ? image.PendingReservationExpiresAtUtc.Value <= dueBeforeUtc
                    : retentionExpired;
            bool reservationExplicitlyAborted =
                !string.IsNullOrWhiteSpace(image.PendingReservationToken)
                && image.AbortedReservationTokens.Contains(
                    image.PendingReservationToken,
                    StringComparer.Ordinal);
            // La barrière de révision protège les écritures Mongo encore en vol,
            // tandis que l'échéance dure garantit qu'une tentative sans
            // commentaire visible ne bloque pas le brouillon indéfiniment.
            if (!revisionFenceReached
                && !cleanupFenceReached
                && !reservationExplicitlyAborted
                && !reservationHardExpired)
            {
                return await this.imageRepository
                    .ReschedulePendingCommentDraftReconciliationAsync(
                        image.Id,
                        draftOwnerId,
                        image.PendingCommentId,
                        image.PendingReservationToken,
                        image.ReservationReconcileAfterUtc.Value,
                        dueBeforeUtc.Add(ReconciliationRetryDelay),
                        cancellationToken);
            }

            return await this.imageRepository
                .ReleaseCommentDraftReservationForReconciliationAsync(
                    image.Id,
                    draftOwnerId,
                    image.PendingCommentId,
                    image.PendingReservationToken,
                    image.ReservationReconcileAfterUtc.Value,
                    dueBeforeUtc.Add(ReconciliationRetryDelay),
                    cancellationToken);
        }

        string? ownerId = Normalize(image.OwnerId);
        if (ownerId is null)
        {
            return false;
        }

        string claimToken = Guid.NewGuid().ToString("N");
        bool claimed = await this.imageRepository.TryClaimCommentImageCleanupAsync(
            image.Id,
            ImageOwnerType.CommentDraft,
            ownerId,
            dueBeforeUtc,
            draftCreatedBeforeUtc,
            null,
            claimToken,
            dueBeforeUtc.Add(CleanupClaimLease),
            cancellationToken);
        if (!claimed)
        {
            return false;
        }

        string? referencingCommentId =
            await this.commentRepository.GetReferencingCommentIdAsync(
                image.Id,
                cancellationToken);
        if (!string.IsNullOrWhiteSpace(referencingCommentId))
        {
            Image? recovered = await this.imageRepository
                .RecoverClaimedReferencedCommentDraftAsync(
                    image.Id,
                    ownerId,
                    referencingCommentId,
                    claimToken,
                    image.CleanupRequestedAtUtc,
                    image.CleanupCommentRevision,
                    dueBeforeUtc.Add(ReconciliationRetryDelay),
                    cancellationToken);
            return recovered is not null;
        }

        bool cleanupIsDue =
            image.CleanupRequestedAtUtc.HasValue
            && image.CleanupRequestedAtUtc.Value <= dueBeforeUtc;
        bool unreservedRetentionExpired =
            image.CreatedAtUtc < draftCreatedBeforeUtc;
        if (!cleanupIsDue && !unreservedRetentionExpired)
        {
            return await this.imageRepository
                .RescheduleClaimedCommentDraftReconciliationAsync(
                    image.Id,
                    ownerId,
                    claimToken,
                    dueBeforeUtc.Add(ReconciliationRetryDelay),
                    cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(image.Path)
            && !await this.imageBinaryStorage.DeleteAsync(image.Path, cancellationToken))
        {
            return false;
        }

        return await this.imageRepository.DeleteClaimedCommentImageAsync(
            image.Id,
            ImageOwnerType.CommentDraft,
            ownerId,
            claimToken,
            cancellationToken);
    }

    private async Task<bool> ReconcilePublishedAsync(
        Image image,
        DateTime dueBeforeUtc,
        DateTime draftCreatedBeforeUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(image.OwnerId)
            || (image.CleanupRequestedAtUtc is null
                && string.IsNullOrWhiteSpace(
                    image.CommentReuseReservationToken)))
        {
            return false;
        }

        string claimToken = Guid.NewGuid().ToString("N");
        bool claimed = await this.imageRepository.TryClaimCommentImageCleanupAsync(
            image.Id,
            ImageOwnerType.Comment,
            image.OwnerId,
            dueBeforeUtc,
            draftCreatedBeforeUtc,
            image.CommentReuseReservationToken,
            claimToken,
            dueBeforeUtc.Add(CleanupClaimLease),
            cancellationToken);
        if (!claimed)
        {
            return false;
        }

        bool hasReuseReservation = !string.IsNullOrWhiteSpace(
            image.CommentReuseReservationToken);
        Comment? ownerComment = await this.commentRepository.GetByIdAsync(
            image.OwnerId,
            cancellationToken);
        bool isReferenced = ownerComment?.ImageIds.Contains(
            image.Id,
            StringComparer.Ordinal) == true;
        if (!isReferenced)
        {
            isReferenced =
                await this.commentRepository.IsImageReferencedAsync(
                    image.Id,
                    cancellationToken);
        }

        if (!hasReuseReservation
            && ownerComment is not null
            && image.CleanupCommentRevision.HasValue
            && ownerComment.Revision < image.CleanupCommentRevision.Value)
        {
            return await this.imageRepository
                .RescheduleClaimedCommentImageCleanupAsync(
                    image.Id,
                    ImageOwnerType.Comment,
                    image.OwnerId,
                    image.CleanupRequestedAtUtc!.Value,
                    image.CleanupCommentRevision,
                    claimToken,
                    dueBeforeUtc.Add(ReconciliationRetryDelay),
                    cancellationToken);
        }

        if (isReferenced)
        {
            if (hasReuseReservation)
            {
                return await this.imageRepository
                    .ResolveClaimedPublishedCommentImageReuseAsync(
                        image.Id,
                        image.OwnerId,
                        image.CommentReuseReservationToken!,
                        claimToken,
                        cancellationToken);
            }

            if (image.CleanupRequestedAtUtc is null)
            {
                return false;
            }

            return await this.imageRepository.CancelClaimedCommentImageCleanupAsync(
                image.Id,
                ImageOwnerType.Comment,
                image.OwnerId,
                image.CleanupRequestedAtUtc.Value,
                image.CleanupCommentRevision,
                claimToken,
                cancellationToken);
        }

        bool reuseReservationHardExpired =
            image.CommentReuseExpiresAtUtc.HasValue
                ? image.CommentReuseExpiresAtUtc.Value <= dueBeforeUtc
                : image.CreatedAtUtc < draftCreatedBeforeUtc;
        if (hasReuseReservation
            && image.CommentReuseTargetRevision.HasValue
            && !reuseReservationHardExpired
            && (ownerComment is null
                || ownerComment.Revision
                    < image.CommentReuseTargetRevision.Value))
        {
            return await this.imageRepository
                .DeferClaimedPublishedCommentImageReuseAsync(
                    image.Id,
                    image.OwnerId,
                    image.CommentReuseReservationToken!,
                    claimToken,
                    dueBeforeUtc.Add(ReconciliationRetryDelay),
                    cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(image.Path)
            && !await this.imageBinaryStorage.DeleteAsync(image.Path, cancellationToken))
        {
            return false;
        }

        return await this.imageRepository.DeleteClaimedCommentImageAsync(
            image.Id,
            ImageOwnerType.Comment,
            image.OwnerId,
            claimToken,
            cancellationToken);
    }

    private static string? Normalize(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
