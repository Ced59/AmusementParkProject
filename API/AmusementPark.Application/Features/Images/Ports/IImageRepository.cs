using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Ports;

/// <summary>
/// Port applicatif de persistance des images.
/// </summary>
public interface IImageRepository
{
    Task<IReadOnlyCollection<Image>> GetAllAsync(CancellationToken cancellationToken);
    Task<PagedResult<Image>> GetPageAsync(int page, int pageSize, ImageSearchCriteria criteria, CancellationToken cancellationToken);
    Task<Image?> GetByIdAsync(string imageId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Image>> GetByIdsAsync(IReadOnlyCollection<string> imageIds, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Image>> GetByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory? category, CancellationToken cancellationToken);
    Task<long> CountActiveCommentDraftsByOwnerAsync(string ownerId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Image>> GetByOwnersAsync(ImageOwnerType ownerType, IReadOnlyCollection<string> ownerIds, ImageCategory? category, CancellationToken cancellationToken);
    Task<Image?> GetByOwnerAndSourceUrlAsync(ImageOwnerType ownerType, string ownerId, string sourceUrl, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Image>> GetCommentImagesRequiringReconciliationAsync(
        DateTime dueBeforeUtc,
        DateTime draftCreatedBeforeUtc,
        int limit,
        CancellationToken cancellationToken);
    Task<bool> TryClaimCommentImageCleanupAsync(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        DateTime dueBeforeUtc,
        DateTime draftCreatedBeforeUtc,
        string? observedCommentReuseReservationToken,
        string claimToken,
        DateTime claimUntilUtc,
        CancellationToken cancellationToken);
    Task<PublishedCommentImageReusePreparation> TryPreparePublishedCommentImageForReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        DateTime reconcileAfterUtc,
        long targetCommentRevision,
        CancellationToken cancellationToken);
    Task<bool> FinalizePublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        CancellationToken cancellationToken);
    Task<bool> ReleasePublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        DateTime cleanupRequestedAtUtc,
        long cleanupCommentRevision,
        CancellationToken cancellationToken);
    Task<bool> ResolveClaimedPublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        string claimToken,
        CancellationToken cancellationToken);
    Task<bool> DeferClaimedPublishedCommentImageReuseAsync(
        string imageId,
        string commentId,
        string reservationToken,
        string claimToken,
        DateTime reconcileAfterUtc,
        CancellationToken cancellationToken);
    Task<bool> CancelClaimedCommentImageCleanupAsync(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        DateTime observedCleanupRequestedAtUtc,
        long? observedCleanupCommentRevision,
        string claimToken,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string>> GetMainImageIdsByOwnersAsync(ImageOwnerType ownerType, IReadOnlyCollection<string> ownerIds, ImageCategory category, bool publishedOnly, CancellationToken cancellationToken);
    Task<Image?> GetCurrentByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category, CancellationToken cancellationToken);
    Task<Image> CreateAsync(ImageUploadRequest request, CancellationToken cancellationToken);
    Task<Image?> LinkAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken);
    Task<Image?> ReserveCommentDraftAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string reservationToken,
        long pendingCommentRevision,
        DateTime reconcileAfterUtc,
        CancellationToken cancellationToken);
    Task<Image?> FinalizeCommentDraftAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken,
        CancellationToken cancellationToken);
    Task<bool> ReleaseCommentDraftReservationAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken,
        CancellationToken cancellationToken);
    Task<bool> ReleaseCommentDraftReservationForReconciliationAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken,
        DateTime observedReconcileAfterUtc,
        DateTime nextReconcileAfterUtc,
        CancellationToken cancellationToken);
    Task<bool> ReschedulePendingCommentDraftReconciliationAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string? reservationToken,
        DateTime observedReconcileAfterUtc,
        DateTime nextReconcileAfterUtc,
        CancellationToken cancellationToken);
    Task<bool> RescheduleClaimedCommentDraftReconciliationAsync(
        string imageId,
        string draftOwnerId,
        string claimToken,
        DateTime nextReconcileAfterUtc,
        CancellationToken cancellationToken);
    Task<bool> RescheduleClaimedCommentImageCleanupAsync(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        DateTime observedCleanupRequestedAtUtc,
        long? observedCleanupCommentRevision,
        string claimToken,
        DateTime nextCleanupRequestedAtUtc,
        CancellationToken cancellationToken);
    Task<Image?> RecoverClaimedReferencedCommentDraftAsync(
        string imageId,
        string draftOwnerId,
        string commentId,
        string claimToken,
        DateTime? observedCleanupRequestedAtUtc,
        long? observedCleanupCommentRevision,
        DateTime safetyCleanupRequestedAtUtc,
        CancellationToken cancellationToken);
    Task<bool> RequestCommentDraftCleanupAsync(
        string imageId,
        string draftOwnerId,
        DateTime cleanupRequestedAtUtc,
        CancellationToken cancellationToken);
    Task<int> RequestCommentImagesCleanupAsync(
        IReadOnlyCollection<string> imageIds,
        string commentId,
        long cleanupCommentRevision,
        DateTime cleanupRequestedAtUtc,
        CancellationToken cancellationToken);
    Task<Image?> SetCurrentAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken);
    Task<Image?> UpdateMetadataAsync(string imageId, ImageMetadataUpdate metadata, CancellationToken cancellationToken);
    Task<Image?> MarkWatermarkedAsync(string imageId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string imageId, CancellationToken cancellationToken);
    Task<bool> DeleteClaimedCommentImageAsync(
        string imageId,
        ImageOwnerType ownerType,
        string ownerId,
        string claimToken,
        CancellationToken cancellationToken);
    Task<int> UpdateBulkMetadataAsync(IReadOnlyCollection<string> imageIds, ImageBulkMetadataUpdate metadata, CancellationToken cancellationToken);
}
