using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Comments.Services;

public sealed class CommentImageReconciler
{
    private static readonly TimeSpan CleanupClaimLease = TimeSpan.FromMinutes(10);
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
            bool isReferenced = await this.commentRepository.IsImageReferencedAsync(
                image.Id,
                cancellationToken);
            string draftOwnerId = image.DraftOwnerId ?? image.OwnerId ?? string.Empty;
            if (isReferenced && !string.IsNullOrWhiteSpace(draftOwnerId))
            {
                Image? finalized = await this.imageRepository.FinalizeCommentDraftAsync(
                    image.Id,
                    draftOwnerId,
                    image.PendingCommentId,
                    image.PendingReservationToken,
                    cancellationToken);
                return finalized is not null;
            }

            if (!string.IsNullOrWhiteSpace(draftOwnerId))
            {
                return await this.imageRepository.ReleaseCommentDraftReservationAsync(
                    image.Id,
                    draftOwnerId,
                    image.PendingCommentId,
                    image.PendingReservationToken,
                    cancellationToken);
            }
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
            claimToken,
            dueBeforeUtc.Add(CleanupClaimLease),
            cancellationToken);
        if (!claimed)
        {
            return false;
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
            || image.CleanupRequestedAtUtc is null)
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
            claimToken,
            dueBeforeUtc.Add(CleanupClaimLease),
            cancellationToken);
        if (!claimed)
        {
            return false;
        }

        bool isReferenced = await this.commentRepository.IsImageReferencedAsync(
            image.Id,
            cancellationToken);
        if (isReferenced)
        {
            return await this.imageRepository.CancelClaimedCommentImageCleanupAsync(
                image.Id,
                ImageOwnerType.Comment,
                image.OwnerId,
                claimToken,
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
