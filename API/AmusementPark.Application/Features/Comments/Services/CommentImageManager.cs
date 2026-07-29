using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Comments.Services;

public sealed class CommentImageManager
{
    public const int MaximumImagesPerComment = 12;
    public const int MaximumDraftImagesPerAuthor = 24;
    private static readonly TimeSpan ReconciliationGracePeriod = TimeSpan.FromMinutes(5);
    private readonly IImageRepository imageRepository;
    private readonly IImageBinaryStorage imageBinaryStorage;

    public CommentImageManager(
        IImageRepository imageRepository,
        IImageBinaryStorage imageBinaryStorage)
    {
        this.imageRepository = imageRepository;
        this.imageBinaryStorage = imageBinaryStorage;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<string>>> PublishForCommentAsync(
        string actorUserId,
        string commentId,
        IReadOnlyCollection<string> desiredImageIds,
        CancellationToken cancellationToken)
    {
        List<string> normalizedIds = NormalizeIds(desiredImageIds);
        if (normalizedIds.Count > MaximumImagesPerComment)
        {
            return ApplicationResult<IReadOnlyCollection<string>>.Failure(
                CommentApplicationErrors.TooManyImages());
        }

        if (normalizedIds.Count == 0)
        {
            return ApplicationResult<IReadOnlyCollection<string>>.Success(Array.Empty<string>());
        }

        IReadOnlyCollection<Image> images = await this.imageRepository.GetByIdsAsync(normalizedIds, cancellationToken);
        Dictionary<string, Image> imagesById = images.ToDictionary(static image => image.Id, StringComparer.Ordinal);
        if (imagesById.Count != normalizedIds.Count)
        {
            return ApplicationResult<IReadOnlyCollection<string>>.Failure(
                CommentApplicationErrors.ImageNotAllowed());
        }

        foreach (string imageId in normalizedIds)
        {
            Image image = imagesById[imageId];
            if (!image.CanBeUsedInComment(actorUserId, commentId))
            {
                return ApplicationResult<IReadOnlyCollection<string>>.Failure(
                    CommentApplicationErrors.ImageNotAllowed());
            }
        }

        List<string> reservedImageIds = new List<string>();
        foreach (Image draft in images.Where(static image => image.OwnerType == ImageOwnerType.CommentDraft))
        {
            Image? reserved = await this.imageRepository.ReserveCommentDraftAsync(
                draft.Id,
                actorUserId,
                commentId,
                DateTime.UtcNow.Add(ReconciliationGracePeriod),
                cancellationToken);
            if (reserved is null)
            {
                return ApplicationResult<IReadOnlyCollection<string>>.Failure(
                    CommentApplicationErrors.ImageNotAllowed());
            }

            reservedImageIds.Add(reserved.Id);
        }

        return ApplicationResult<IReadOnlyCollection<string>>.Success(reservedImageIds);
    }

    public async Task FinalizeForCommentAsync(
        string actorUserId,
        string commentId,
        IReadOnlyCollection<string> reservedImageIds)
    {
        foreach (string imageId in NormalizeIds(reservedImageIds))
        {
            try
            {
                await this.imageRepository.FinalizeCommentDraftAsync(
                    imageId,
                    actorUserId,
                    commentId,
                    CancellationToken.None);
            }
            catch
            {
                // Le brouillon réservé reste privé et sera réconcilié par le worker.
            }
        }
    }

    public async Task RequestRemovedCleanupAsync(
        string commentId,
        IReadOnlyCollection<string> removedImageIds,
        CancellationToken cancellationToken)
    {
        List<string> normalizedIds = NormalizeIds(removedImageIds);
        if (normalizedIds.Count == 0)
        {
            return;
        }

        await this.imageRepository.RequestCommentImagesCleanupAsync(
            normalizedIds,
            commentId,
            DateTime.UtcNow.Add(ReconciliationGracePeriod),
            cancellationToken);
    }

    public async Task<ApplicationResult> DeleteOwnedDraftAsync(
        string actorUserId,
        string imageId,
        CancellationToken cancellationToken)
    {
        Image? image = await this.imageRepository.GetByIdAsync(imageId.Trim(), cancellationToken);
        if (image is null
            || !image.IsCommentDraftOwnedBy(actorUserId)
            || !string.IsNullOrWhiteSpace(image.PendingCommentId))
        {
            return ApplicationResult.Failure(CommentApplicationErrors.ImageNotAllowed());
        }

        bool requested = await this.imageRepository.RequestCommentDraftCleanupAsync(
            image.Id,
            actorUserId,
            DateTime.UtcNow,
            cancellationToken);
        return requested
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(CommentApplicationErrors.ImageNotAllowed());
    }

    public async Task<int> DeleteExpiredDraftsAsync(
        DateTime createdBeforeUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Image> drafts = await this.imageRepository.GetExpiredCommentDraftsAsync(
            createdBeforeUtc,
            limit,
            cancellationToken);
        int deletedCount = 0;
        foreach (Image draft in drafts)
        {
            if (await this.DeleteDraftAsync(draft, null, cancellationToken))
            {
                deletedCount++;
            }
        }

        return deletedCount;
    }

    private async Task<bool> DeleteDraftAsync(
        Image image,
        string? ownerId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(image.Path)
            && !await this.imageBinaryStorage.DeleteAsync(image.Path, cancellationToken))
        {
            return false;
        }

        return await this.imageRepository.DeleteCommentDraftAsync(
            image.Id,
            ownerId,
            cancellationToken);
    }

    private static List<string> NormalizeIds(IReadOnlyCollection<string> imageIds)
    {
        return imageIds
            .Where(static imageId => !string.IsNullOrWhiteSpace(imageId))
            .Select(static imageId => imageId.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
