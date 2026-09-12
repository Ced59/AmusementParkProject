using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileRevisionVisitDeletionStore : IVisitDeletionStore
{
    private readonly IVisitDeletionStore inner;
    private readonly IPassportProfileShareSourceRevisionGuard revisionGuard;

    public PassportProfileRevisionVisitDeletionStore(
        IVisitDeletionStore inner,
        IPassportProfileShareSourceRevisionGuard revisionGuard)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.revisionGuard = revisionGuard ?? throw new ArgumentNullException(nameof(revisionGuard));
    }

    public Task<VisitDeletionImpact> GetImpactAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        return this.inner.GetImpactAsync(visitId, userId, cancellationToken);
    }

    public Task<VisitDeletionReceipt?> GetReceiptAsync(
        VisitId visitId,
        string userId,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.inner.GetReceiptAsync(
            visitId,
            userId,
            clientOperationId,
            cancellationToken);
    }

    public async Task<bool> TryTombstoneAsync(
        VisitDeletionTombstoneRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.AuditEvent.PreviousVisitStatus.HasValue
            || !PassportProfileSourceMutationPolicy.CanChangeCompletedVisitProjection(
                request.AuditEvent.PreviousVisitStatus.Value))
        {
            return await this.inner.TryTombstoneAsync(request, cancellationToken);
        }

        IReadOnlyCollection<(string ParkId, int Year)> segments =
            request.AuditEvent.PreviousVisitDate is not null
                ? new[]
                {
                    (request.AuditEvent.ParkId, request.AuditEvent.PreviousVisitDate.Year),
                }
                : Array.Empty<(string ParkId, int Year)>();
        IReadOnlyCollection<ShareSourceMutationLease> mutationLeases =
            await this.revisionGuard.TryBeginMutationAsync(
                request.UserId,
                segments,
                cancellationToken);
        CancellationToken[] leaseCancellationTokens = mutationLeases
            .Select(static lease => lease.LeaseCancellationToken)
            .Prepend(cancellationToken)
            .ToArray();
        using CancellationTokenSource? linkedCancellation = mutationLeases.Count == 0
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(leaseCancellationTokens);
        CancellationToken guardedCancellationToken =
            linkedCancellation?.Token ?? cancellationToken;
        try
        {
            bool deleted = await this.inner.TryTombstoneAsync(
                request,
                guardedCancellationToken);
            await this.revisionGuard.CompleteMutationAsync(
                mutationLeases,
                deleted,
                CancellationToken.None);
            return deleted;
        }
        catch
        {
            await this.revisionGuard.CompleteMutationAsync(
                mutationLeases,
                true,
                CancellationToken.None);
            throw;
        }
    }

    public Task<IReadOnlyCollection<VisitDeletionReconciliationCandidate>>
        ListPendingDeletionReconciliationAsync(
            DateTime nowUtc,
            int maximumCount,
            CancellationToken cancellationToken)
    {
        return this.inner.ListPendingDeletionReconciliationAsync(
            nowUtc,
            maximumCount,
            cancellationToken);
    }

    public Task<VisitExportInvalidationClaim?> TryClaimExportInvalidationAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        DateTime claimedAtUtc,
        DateTime claimExpiresAtUtc,
        CancellationToken cancellationToken)
    {
        return this.inner.TryClaimExportInvalidationAsync(
            visitId,
            userId,
            deletionVersion,
            claimedAtUtc,
            claimExpiresAtUtc,
            cancellationToken);
    }

    public Task<bool> CompleteExportInvalidationAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        string claimToken,
        DateTime ensuredAtUtc,
        CancellationToken cancellationToken)
    {
        return this.inner.CompleteExportInvalidationAsync(
            visitId,
            userId,
            deletionVersion,
            claimToken,
            ensuredAtUtc,
            cancellationToken);
    }

    public Task<bool> MarkPurgeJobEnsuredAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        DateTime ensuredAtUtc,
        CancellationToken cancellationToken)
    {
        return this.inner.MarkPurgeJobEnsuredAsync(
            visitId,
            userId,
            deletionVersion,
            ensuredAtUtc,
            cancellationToken);
    }

    public Task<VisitDeletionPurgeResult> PurgeBatchAsync(
        VisitId visitId,
        string userId,
        DateTime nowUtc,
        int maximumDocumentsPerCollection,
        CancellationToken cancellationToken)
    {
        return this.inner.PurgeBatchAsync(
            visitId,
            userId,
            nowUtc,
            maximumDocumentsPerCollection,
            cancellationToken);
    }
}
