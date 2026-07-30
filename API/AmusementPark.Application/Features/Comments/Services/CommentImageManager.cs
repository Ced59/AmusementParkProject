using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Comments.Services;

public sealed class CommentImageManager
{
    public const int MaximumImagesPerComment = 12;
    public const int MaximumDraftImagesPerAuthor = 24;
    private static readonly TimeSpan ReconciliationGracePeriod = TimeSpan.FromMinutes(5);
    private readonly IImageRepository imageRepository;
    private readonly ILogger<CommentImageManager>? logger;

    public CommentImageManager(
        IImageRepository imageRepository,
        ILogger<CommentImageManager>? logger = null)
    {
        this.imageRepository = imageRepository;
        this.logger = logger;
    }

    public async Task<ApplicationResult<CommentImageReservationBatch>> PublishForCommentAsync(
        string actorUserId,
        string commentId,
        IReadOnlyCollection<string> desiredImageIds,
        CancellationToken cancellationToken,
        long pendingCommentRevision)
    {
        List<string> normalizedIds = NormalizeIds(desiredImageIds);
        string reservationToken = Guid.NewGuid().ToString("N");
        if (normalizedIds.Count > MaximumImagesPerComment)
        {
            return ApplicationResult<CommentImageReservationBatch>.Failure(
                CommentApplicationErrors.TooManyImages());
        }

        if (normalizedIds.Count == 0)
        {
            return ApplicationResult<CommentImageReservationBatch>.Success(
                new CommentImageReservationBatch(
                    Array.Empty<string>(),
                    reservationToken,
                    Array.Empty<string>(),
                    pendingCommentRevision));
        }

        IReadOnlyCollection<Image> images = await this.imageRepository.GetByIdsAsync(normalizedIds, cancellationToken);
        Dictionary<string, Image> imagesById = images.ToDictionary(static image => image.Id, StringComparer.Ordinal);
        if (imagesById.Count != normalizedIds.Count)
        {
            return ApplicationResult<CommentImageReservationBatch>.Failure(
                CommentApplicationErrors.ImageNotAllowed());
        }

        foreach (string imageId in normalizedIds)
        {
            Image image = imagesById[imageId];
            if (!image.CanBeUsedInComment(actorUserId, commentId))
            {
                return ApplicationResult<CommentImageReservationBatch>.Failure(
                    CommentApplicationErrors.ImageNotAllowed());
            }
        }

        List<string> preparedCleanupImageIds = new List<string>();
        string? preparingPublishedImageId = null;
        try
        {
            foreach (Image published in images.Where(
                static image => image.OwnerType == ImageOwnerType.Comment))
            {
                preparingPublishedImageId = published.Id;
                PublishedCommentImageReusePreparation preparation =
                    await this.imageRepository.TryPreparePublishedCommentImageForReuseAsync(
                        published.Id,
                        commentId,
                        reservationToken,
                        DateTime.UtcNow.Add(ReconciliationGracePeriod),
                        pendingCommentRevision,
                        cancellationToken);
                if (preparation == PublishedCommentImageReusePreparation.Rejected)
                {
                    await this.RestorePreparedPublishedCleanupAsync(
                        commentId,
                        reservationToken,
                        pendingCommentRevision,
                        preparedCleanupImageIds);
                    return ApplicationResult<CommentImageReservationBatch>.Failure(
                        CommentApplicationErrors.ImageNotAllowed());
                }

                if (preparation
                    == PublishedCommentImageReusePreparation.PreparedAndCleanupCleared)
                {
                    preparedCleanupImageIds.Add(published.Id);
                }

                preparingPublishedImageId = null;
            }
        }
        catch
        {
            List<string> cleanupImageIdsToRestore =
                new List<string>(preparedCleanupImageIds);
            if (!string.IsNullOrWhiteSpace(preparingPublishedImageId))
            {
                cleanupImageIdsToRestore.Add(preparingPublishedImageId);
            }

            await this.RestorePreparedPublishedCleanupAsync(
                commentId,
                reservationToken,
                pendingCommentRevision,
                cleanupImageIdsToRestore);
            throw;
        }

        List<string> reservedImageIds = new List<string>();
        string? reservationInFlightImageId = null;
        try
        {
            foreach (Image draft in images.Where(static image => image.OwnerType == ImageOwnerType.CommentDraft))
            {
                reservationInFlightImageId = draft.Id;
                Image? reserved = await this.imageRepository.ReserveCommentDraftAsync(
                    draft.Id,
                    actorUserId,
                    commentId,
                    reservationToken,
                    pendingCommentRevision,
                    DateTime.UtcNow.Add(ReconciliationGracePeriod),
                    cancellationToken);
                if (reserved is null)
                {
                    _ = await this.ReleaseReservationsForCommentAsync(
                        actorUserId,
                        commentId,
                        new CommentImageReservationBatch(
                            reservedImageIds,
                            reservationToken,
                            preparedCleanupImageIds,
                            pendingCommentRevision));
                    return ApplicationResult<CommentImageReservationBatch>.Failure(
                        CommentApplicationErrors.ImageNotAllowed());
                }

                reservedImageIds.Add(reserved.Id);
                reservationInFlightImageId = null;
            }
        }
        catch
        {
            List<string> rollbackImageIds = new List<string>(reservedImageIds);
            if (!string.IsNullOrWhiteSpace(reservationInFlightImageId))
            {
                rollbackImageIds.Add(reservationInFlightImageId);
            }

            _ = await this.ReleaseReservationsForCommentAsync(
                actorUserId,
                commentId,
                new CommentImageReservationBatch(
                    rollbackImageIds,
                    reservationToken,
                    preparedCleanupImageIds,
                    pendingCommentRevision));
            throw;
        }

        return ApplicationResult<CommentImageReservationBatch>.Success(
            new CommentImageReservationBatch(
                reservedImageIds,
                reservationToken,
                preparedCleanupImageIds,
                pendingCommentRevision));
    }

    public async Task<IReadOnlyCollection<string>> FinalizeForCommentAsync(
        string actorUserId,
        string commentId,
        CommentImageReservationBatch reservationBatch)
    {
        List<string> failedImageIds = new List<string>();
        foreach (string imageId in NormalizeIds(reservationBatch.ReservedImageIds))
        {
            try
            {
                Image? finalized = await this.imageRepository.FinalizeCommentDraftAsync(
                    imageId,
                    actorUserId,
                    commentId,
                    reservationBatch.ReservationToken,
                    CancellationToken.None);
                if (finalized is null)
                {
                    failedImageIds.Add(imageId);
                    this.logger?.LogWarning(
                        "Unable to finalize reserved comment image {ImageId} for comment {CommentId}.",
                        imageId,
                        commentId);
                }
            }
            catch (Exception exception)
            {
                failedImageIds.Add(imageId);
                this.logger?.LogWarning(
                    exception,
                    "Unable to finalize reserved comment image {ImageId} for comment {CommentId}.",
                    imageId,
                    commentId);
                // Le brouillon réservé reste privé et sera réconcilié par le worker.
            }
        }

        foreach (string imageId in NormalizeIds(
            reservationBatch.PreparedCleanupImageIds))
        {
            try
            {
                bool finalized =
                    await this.imageRepository.FinalizePublishedCommentImageReuseAsync(
                        imageId,
                        commentId,
                        reservationBatch.ReservationToken,
                        CancellationToken.None);
                if (!finalized)
                {
                    failedImageIds.Add(imageId);
                    this.logger?.LogWarning(
                        "Unable to finalize prepared comment image {ImageId} for comment {CommentId}.",
                        imageId,
                        commentId);
                }
            }
            catch (Exception exception)
            {
                failedImageIds.Add(imageId);
                this.logger?.LogWarning(
                    exception,
                    "Unable to finalize prepared comment image {ImageId} for comment {CommentId}.",
                    imageId,
                    commentId);
                // L'état durable sera réconcilié avec la référence du commentaire.
            }
        }

        return failedImageIds;
    }

    public async Task RequestRemovedCleanupAsync(
        string commentId,
        long cleanupCommentRevision,
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
            cleanupCommentRevision,
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

    public async Task<IReadOnlyCollection<string>> ReleaseReservationsForCommentAsync(
        string actorUserId,
        string commentId,
        CommentImageReservationBatch reservationBatch)
    {
        List<string> failedImageIds = new List<string>();
        foreach (string imageId in NormalizeIds(reservationBatch.ReservedImageIds))
        {
            try
            {
                bool released = await this.imageRepository.ReleaseCommentDraftReservationAsync(
                    imageId,
                    actorUserId,
                    commentId,
                    reservationBatch.ReservationToken,
                    CancellationToken.None);
                if (!released)
                {
                    failedImageIds.Add(imageId);
                    this.logger?.LogWarning(
                        "Unable to release reserved comment image {ImageId} for comment {CommentId}.",
                        imageId,
                        commentId);
                }
            }
            catch (Exception exception)
            {
                failedImageIds.Add(imageId);
                this.logger?.LogWarning(
                    exception,
                    "Unable to release reserved comment image {ImageId} for comment {CommentId}.",
                    imageId,
                    commentId);
                // La réconciliation libérera la réservation si le rollback échoue.
            }
        }

        IReadOnlyCollection<string> cleanupRestoreFailures =
            await this.RestorePreparedPublishedCleanupAsync(
                commentId,
                reservationBatch.ReservationToken,
                reservationBatch.PendingCommentRevision,
                reservationBatch.PreparedCleanupImageIds);
        failedImageIds.AddRange(cleanupRestoreFailures);

        return failedImageIds;
    }

    public Task<IReadOnlyCollection<string>> RestorePreparedCleanupForCommentAsync(
        string commentId,
        CommentImageReservationBatch reservationBatch)
    {
        return this.RestorePreparedPublishedCleanupAsync(
            commentId,
            reservationBatch.ReservationToken,
            reservationBatch.PendingCommentRevision,
            reservationBatch.PreparedCleanupImageIds);
    }

    private async Task<IReadOnlyCollection<string>> RestorePreparedPublishedCleanupAsync(
        string commentId,
        string reservationToken,
        long cleanupCommentRevision,
        IReadOnlyCollection<string> preparedPublishedImageIds)
    {
        if (preparedPublishedImageIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        List<string> failedImageIds = new List<string>();
        DateTime cleanupRequestedAtUtc =
            DateTime.UtcNow.Add(ReconciliationGracePeriod);
        foreach (string imageId in NormalizeIds(preparedPublishedImageIds))
        {
            try
            {
                bool released =
                    await this.imageRepository.ReleasePublishedCommentImageReuseAsync(
                        imageId,
                        commentId,
                        reservationToken,
                        cleanupRequestedAtUtc,
                        cleanupCommentRevision,
                        CancellationToken.None);
                if (!released)
                {
                    failedImageIds.Add(imageId);
                    this.logger?.LogWarning(
                        "Unable to release prepared comment image {ImageId} for comment {CommentId}.",
                        imageId,
                        commentId);
                }
            }
            catch (Exception exception)
            {
                failedImageIds.Add(imageId);
                this.logger?.LogWarning(
                    exception,
                    "Unable to release prepared comment image {ImageId} for comment {CommentId}.",
                    imageId,
                    commentId);
                // Le marqueur durable reste sélectionnable par le reconciler.
            }
        }

        return failedImageIds;
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
