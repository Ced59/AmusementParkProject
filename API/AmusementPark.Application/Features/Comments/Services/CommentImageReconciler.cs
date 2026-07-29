using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Comments.Services;

public sealed class CommentImageReconciler
{
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
                ImageOwnerType.CommentDraft => await this.ReconcileDraftAsync(image, cancellationToken),
                ImageOwnerType.Comment => await this.ReconcilePublishedAsync(image, cancellationToken),
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
                    cancellationToken);
                return finalized is not null;
            }

            if (!string.IsNullOrWhiteSpace(draftOwnerId))
            {
                return await this.imageRepository.ReleaseCommentDraftReservationAsync(
                    image.Id,
                    draftOwnerId,
                    image.PendingCommentId,
                    cancellationToken);
            }
        }

        if (!string.IsNullOrWhiteSpace(image.Path)
            && !await this.imageBinaryStorage.DeleteAsync(image.Path, cancellationToken))
        {
            return false;
        }

        return await this.imageRepository.DeleteCommentDraftAsync(
            image.Id,
            image.OwnerId,
            cancellationToken);
    }

    private async Task<bool> ReconcilePublishedAsync(
        Image image,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(image.OwnerId)
            || image.CleanupRequestedAtUtc is null)
        {
            return false;
        }

        bool isReferenced = await this.commentRepository.IsImageReferencedAsync(
            image.Id,
            cancellationToken);
        if (isReferenced)
        {
            return await this.imageRepository.ClearCommentImageCleanupAsync(
                image.Id,
                image.OwnerId,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(image.Path)
            && !await this.imageBinaryStorage.DeleteAsync(image.Path, cancellationToken))
        {
            return false;
        }

        return await this.imageRepository.DeleteCommentImageAsync(
            image.Id,
            image.OwnerId,
            cancellationToken);
    }
}
