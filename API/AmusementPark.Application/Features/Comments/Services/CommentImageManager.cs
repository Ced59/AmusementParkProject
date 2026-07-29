using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Comments.Services;

public sealed class CommentImageManager
{
    public const int MaximumImagesPerComment = 12;
    public const int MaximumDraftImagesPerAuthor = 24;
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

        List<string> publishedImageIds = new List<string>();
        try
        {
            foreach (Image draft in images.Where(static image => image.OwnerType == ImageOwnerType.CommentDraft))
            {
                Image? published = await this.imageRepository.PublishCommentDraftAsync(
                    draft.Id,
                    actorUserId,
                    commentId,
                    cancellationToken);
                if (published is null)
                {
                    await this.RollbackPublishedAsync(commentId, publishedImageIds);
                    return ApplicationResult<IReadOnlyCollection<string>>.Failure(
                        CommentApplicationErrors.ImageNotAllowed());
                }

                publishedImageIds.Add(published.Id);
            }
        }
        catch
        {
            await this.RollbackPublishedAsync(commentId, publishedImageIds);
            throw;
        }

        return ApplicationResult<IReadOnlyCollection<string>>.Success(publishedImageIds);
    }

    public Task RollbackPublishedAsync(
        string commentId,
        IReadOnlyCollection<string> publishedImageIds)
    {
        return this.RollbackPublishedCoreAsync(commentId, publishedImageIds);
    }

    public async Task DeleteRemovedAsync(
        string commentId,
        IReadOnlyCollection<string> removedImageIds,
        CancellationToken cancellationToken)
    {
        List<string> normalizedIds = NormalizeIds(removedImageIds);
        if (normalizedIds.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<Image> images = await this.imageRepository.GetByIdsAsync(normalizedIds, cancellationToken);
        foreach (Image image in images)
        {
            if (!image.IsOwnedByComment(commentId))
            {
                continue;
            }

            await this.DeleteCommentImageAsync(image, commentId, cancellationToken);
        }
    }

    public async Task<ApplicationResult> DeleteOwnedDraftAsync(
        string actorUserId,
        string imageId,
        CancellationToken cancellationToken)
    {
        Image? image = await this.imageRepository.GetByIdAsync(imageId.Trim(), cancellationToken);
        if (image is null || !image.IsCommentDraftOwnedBy(actorUserId))
        {
            return ApplicationResult.Failure(CommentApplicationErrors.ImageNotAllowed());
        }

        bool deleted = await this.DeleteDraftAsync(image, actorUserId, cancellationToken);
        return deleted
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
        bool deleted = await this.imageRepository.DeleteCommentDraftAsync(
            image.Id,
            ownerId,
            cancellationToken);
        if (deleted && !string.IsNullOrWhiteSpace(image.Path))
        {
            await this.imageBinaryStorage.DeleteAsync(image.Path, cancellationToken);
        }

        return deleted;
    }

    private async Task DeleteCommentImageAsync(
        Image image,
        string commentId,
        CancellationToken cancellationToken)
    {
        bool deleted = await this.imageRepository.DeleteCommentImageAsync(
            image.Id,
            commentId,
            cancellationToken);
        if (deleted && !string.IsNullOrWhiteSpace(image.Path))
        {
            await this.imageBinaryStorage.DeleteAsync(image.Path, cancellationToken);
        }
    }

    private async Task RollbackPublishedCoreAsync(
        string commentId,
        IReadOnlyCollection<string> publishedImageIds)
    {
        List<string> normalizedIds = NormalizeIds(publishedImageIds);
        if (normalizedIds.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<Image> images;
        try
        {
            images = await this.imageRepository.GetByIdsAsync(
                normalizedIds,
                CancellationToken.None);
        }
        catch
        {
            // Best effort: the original persistence/publication failure must remain observable.
            return;
        }

        foreach (Image image in images)
        {
            if (!image.IsOwnedByComment(commentId))
            {
                continue;
            }

            try
            {
                await this.DeleteCommentImageAsync(image, commentId, CancellationToken.None);
            }
            catch
            {
                // Continue rolling back the remaining images.
            }
        }
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
