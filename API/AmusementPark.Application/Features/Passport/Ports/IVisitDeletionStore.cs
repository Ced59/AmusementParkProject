using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IVisitDeletionStore
{
    Task<VisitDeletionImpact> GetImpactAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken);

    Task<VisitDeletionReceipt?> GetReceiptAsync(
        VisitId visitId,
        string userId,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<bool> TryTombstoneAsync(
        VisitDeletionTombstoneRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VisitDeletionReconciliationCandidate>>
        ListPendingDeletionReconciliationAsync(
            int maximumCount,
            CancellationToken cancellationToken);

    Task<VisitExportInvalidationClaim?> TryClaimExportInvalidationAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        DateTime claimedAtUtc,
        DateTime claimExpiresAtUtc,
        CancellationToken cancellationToken);

    Task<bool> CompleteExportInvalidationAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        string claimToken,
        DateTime ensuredAtUtc,
        CancellationToken cancellationToken);

    Task<bool> MarkPurgeJobEnsuredAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        DateTime ensuredAtUtc,
        CancellationToken cancellationToken);

    Task<VisitDeletionPurgeResult> PurgeBatchAsync(
        VisitId visitId,
        string userId,
        DateTime nowUtc,
        int maximumDocumentsPerCollection,
        CancellationToken cancellationToken);
}
