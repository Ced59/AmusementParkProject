using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileRevisionRideOccurrenceRepository
    : IRideOccurrenceRepository
{
    private readonly IRideOccurrenceRepository inner;
    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportProfileShareSourceRevisionGuard revisionGuard;

    public PassportProfileRevisionRideOccurrenceRepository(
        IRideOccurrenceRepository inner,
        IUserVisitRepository visitRepository,
        IPassportProfileShareSourceRevisionGuard revisionGuard)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.visitRepository = visitRepository
            ?? throw new ArgumentNullException(nameof(visitRepository));
        this.revisionGuard = revisionGuard ?? throw new ArgumentNullException(nameof(revisionGuard));
    }

    public Task<PendingPassportMutationVisit?> GetPendingMutationAsync(
        string userId,
        VisitId visitId,
        CancellationToken cancellationToken)
    {
        return this.inner.GetPendingMutationAsync(userId, visitId, cancellationToken);
    }

    public Task<PendingPassportMutationVisit?> GetPendingMutationFencedAsync(
        string userId,
        VisitId visitId,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return this.inner.GetPendingMutationFencedAsync(
            userId,
            visitId,
            contentFenceToken,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<PendingPassportMutationVisit>>
        ListPendingAuditMutationVisitsAsync(
            int maximumVisitCount,
            CancellationToken cancellationToken)
    {
        return this.inner.ListPendingAuditMutationVisitsAsync(
            maximumVisitCount,
            cancellationToken);
    }

    public Task<int> ReconcileProvisionalCreationAllocationsAsync(
        int maximumDocumentCount,
        CancellationToken cancellationToken)
    {
        return this.inner.ReconcileProvisionalCreationAllocationsAsync(
            maximumDocumentCount,
            cancellationToken);
    }

    public Task<bool> TryCompletePendingMutationAsync(
        PendingPassportMutationVisit mutation,
        CancellationToken cancellationToken)
    {
        return this.inner.TryCompletePendingMutationAsync(mutation, cancellationToken);
    }

    public Task<bool> TryRejectPendingMutationAsync(
        PendingPassportMutationVisit mutation,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken)
    {
        return this.inner.TryRejectPendingMutationAsync(
            mutation,
            rejectedAtUtc,
            cancellationToken);
    }

    public Task<RideOccurrenceCreationKeyReservationResult>
        ResolveBatchCreationKeyReservationAsync(
            RideOccurrenceCreationRequest request,
            string clientOperationId,
            CancellationToken cancellationToken)
    {
        return this.inner.ResolveBatchCreationKeyReservationAsync(
            request,
            clientOperationId,
            cancellationToken);
    }

    public Task<RideOccurrenceCreationKeyReservationResult> ReserveBatchCreationKeyAsync(
        RideOccurrenceCreationRequest request,
        RideOccurrenceCreationPreparation preparation,
        string clientOperationId,
        DateTime reservedAtUtc,
        CancellationToken cancellationToken)
    {
        return this.inner.ReserveBatchCreationKeyAsync(
            request,
            preparation,
            clientOperationId,
            reservedAtUtc,
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceCreationResult?> ResolveExistingBatchCreationAsync(
        RideOccurrenceCreationRequest request,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.inner.ResolveExistingBatchCreationAsync(
            request,
            clientOperationId,
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAsync(
        RideOccurrenceCreationRequest request,
        IReadOnlyList<RideOccurrence> occurrences,
        long? expectedLastSortPosition,
        bool wasOrderNormalized,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.ExecuteVisitMutationAsync(
            request.UserId,
            request.VisitId,
            token => this.inner.CreateBatchIdempotentAsync(
                request,
                occurrences,
                expectedLastSortPosition,
                wasOrderNormalized,
                clientOperationId,
                token),
            static result => result.Status == IdempotentRideOccurrenceCreationStatus.Created,
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAuditedAsync(
        RideOccurrenceCreationRequest request,
        IReadOnlyList<RideOccurrence> occurrences,
        long? expectedLastSortPosition,
        bool wasOrderNormalized,
        string clientOperationId,
        IReadOnlyCollection<PassportAuditEvent> pendingAuditEvents,
        CancellationToken cancellationToken)
    {
        return this.ExecuteVisitMutationAsync(
            request.UserId,
            request.VisitId,
            token => this.inner.CreateBatchIdempotentAuditedAsync(
                request,
                occurrences,
                expectedLastSortPosition,
                wasOrderNormalized,
                clientOperationId,
                pendingAuditEvents,
                token),
            static result => result.Status == IdempotentRideOccurrenceCreationStatus.Created,
            cancellationToken);
    }

    public Task<RideOccurrence?> GetOwnedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        return this.inner.GetOwnedAsync(
            occurrenceId,
            visitId,
            userId,
            cancellationToken);
    }

    public Task<RideOccurrence?> GetOwnedByIdAsync(
        RideOccurrenceId occurrenceId,
        string userId,
        CancellationToken cancellationToken)
    {
        return this.inner.GetOwnedByIdAsync(occurrenceId, userId, cancellationToken);
    }

    public Task<RideOccurrencePage> ListOwnedByVisitAsync(
        RideOccurrenceListCriteria criteria,
        CancellationToken cancellationToken)
    {
        return this.inner.ListOwnedByVisitAsync(criteria, cancellationToken);
    }

    public Task<IReadOnlyCollection<RideOccurrence>> ListAllOwnedForExportAsync(
        string userId,
        IReadOnlyCollection<VisitId> activeVisitIds,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        return this.inner.ListAllOwnedForExportAsync(
            userId,
            activeVisitIds,
            sourceBudget,
            cancellationToken);
    }

    public Task<RideOccurrenceAppendState> GetAppendStateAsync(
        VisitId visitId,
        string userId,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.inner.GetAppendStateAsync(
            visitId,
            userId,
            clientOperationId,
            cancellationToken);
    }

    public Task<bool> TryUpdateOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.UpdateAsync(
            occurrence,
            token => this.inner.TryUpdateOwnedAsync(occurrence, expectedVersion, token),
            cancellationToken);
    }

    public Task<bool> TryUpdateOwnedAuditedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        return this.UpdateAsync(
            occurrence,
            token => this.inner.TryUpdateOwnedAuditedAsync(
                occurrence,
                expectedVersion,
                pendingAuditEvent,
                token),
            cancellationToken);
    }

    public Task<bool> TryUpdateOwnedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return this.UpdateAsync(
            occurrence,
            token => this.inner.TryUpdateOwnedFencedAsync(
                occurrence,
                expectedVersion,
                contentFenceToken,
                token),
            cancellationToken);
    }

    public Task<bool> TryUpdateOwnedAuditedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return this.UpdateAsync(
            occurrence,
            token => this.inner.TryUpdateOwnedAuditedFencedAsync(
                occurrence,
                expectedVersion,
                pendingAuditEvent,
                contentFenceToken,
                token),
            cancellationToken);
    }

    public Task<bool> TryConfirmOwnedVersionAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.inner.TryConfirmOwnedVersionAsync(
            occurrenceId,
            visitId,
            userId,
            expectedVersion,
            cancellationToken);
    }

    public Task<bool> TryConfirmOwnedVersionFencedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return this.inner.TryConfirmOwnedVersionFencedAsync(
            occurrenceId,
            visitId,
            userId,
            expectedVersion,
            contentFenceToken,
            cancellationToken);
    }

    public Task<bool> TryDeleteOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.UpdateAsync(
            occurrence,
            token => this.inner.TryDeleteOwnedAsync(occurrence, expectedVersion, token),
            cancellationToken);
    }

    public Task<bool> TryDeleteOwnedAuditedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        return this.UpdateAsync(
            occurrence,
            token => this.inner.TryDeleteOwnedAuditedAsync(
                occurrence,
                expectedVersion,
                pendingAuditEvent,
                token),
            cancellationToken);
    }

    public Task<bool> TryDeleteOwnedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return this.UpdateAsync(
            occurrence,
            token => this.inner.TryDeleteOwnedFencedAsync(
                occurrence,
                expectedVersion,
                contentFenceToken,
                token),
            cancellationToken);
    }

    public Task<bool> TryDeleteOwnedAuditedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        long contentFenceToken,
        CancellationToken cancellationToken)
    {
        return this.UpdateAsync(
            occurrence,
            token => this.inner.TryDeleteOwnedAuditedFencedAsync(
                occurrence,
                expectedVersion,
                pendingAuditEvent,
                contentFenceToken,
                token),
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceReorderResult?> ResolveExistingReorderAsync(
        RideOccurrenceReorderRequest request,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.inner.ResolveExistingReorderAsync(
            request,
            clientOperationId,
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        string? relatedCreationClientOperationId,
        CancellationToken cancellationToken)
    {
        return this.ExecuteVisitMutationAsync(
            request.UserId,
            request.VisitId,
            token => this.inner.ReorderIdempotentAsync(
                request,
                changes,
                guards,
                resultOccurrence,
                wasNormalized,
                operationAtUtc,
                clientOperationId,
                relatedCreationClientOperationId,
                token),
            static result => result.Status == IdempotentRideOccurrenceReorderStatus.Applied,
            cancellationToken);
    }

    public Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentAuditedAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        string? relatedCreationClientOperationId,
        IReadOnlyCollection<PassportAuditEvent> pendingAuditEvents,
        CancellationToken cancellationToken)
    {
        return this.ExecuteVisitMutationAsync(
            request.UserId,
            request.VisitId,
            token => this.inner.ReorderIdempotentAuditedAsync(
                request,
                changes,
                guards,
                resultOccurrence,
                wasNormalized,
                operationAtUtc,
                clientOperationId,
                relatedCreationClientOperationId,
                pendingAuditEvents,
                token),
            static result => result.Status == IdempotentRideOccurrenceReorderStatus.Applied,
            cancellationToken);
    }

    private Task<bool> UpdateAsync(
        RideOccurrence occurrence,
        Func<CancellationToken, Task<bool>> mutation,
        CancellationToken cancellationToken)
    {
        return this.ExecuteVisitMutationAsync(
            occurrence.UserId,
            occurrence.VisitId,
            mutation,
            static updated => updated,
            cancellationToken);
    }

    private async Task<TResult> ExecuteVisitMutationAsync<TResult>(
        string ownerUserId,
        VisitId visitId,
        Func<CancellationToken, Task<TResult>> mutation,
        Func<TResult, bool> sourceChanged,
        CancellationToken cancellationToken)
    {
        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            ownerUserId,
            cancellationToken);
        bool canChangeSource = visit is not null
            && PassportProfileSourceMutationPolicy.CanChangeCompletedVisitProjection(
                visit.Status);
        return await this.ExecuteMutationAsync(
            ownerUserId,
            canChangeSource
                ? new[] { (visit!.ParkId, visit.Date.Year) }
                : Array.Empty<(string ParkId, int Year)>(),
            mutation,
            sourceChanged,
            cancellationToken);
    }

    private async Task<TResult> ExecuteMutationAsync<TResult>(
        string ownerUserId,
        IReadOnlyCollection<(string ParkId, int Year)> segments,
        Func<CancellationToken, Task<TResult>> mutation,
        Func<TResult, bool> sourceChanged,
        CancellationToken cancellationToken)
    {
        if (segments.Count == 0)
        {
            return await mutation(cancellationToken);
        }

        IReadOnlyCollection<ShareSourceMutationLease> mutationLeases =
            await this.revisionGuard.TryBeginMutationAsync(
                ownerUserId,
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
            TResult result = await mutation(guardedCancellationToken);
            await this.revisionGuard.CompleteMutationAsync(
                mutationLeases,
                sourceChanged(result),
                CancellationToken.None);
            return result;
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
}
