using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Persistance privée des occurrences, toujours bornée à la visite et au propriétaire.
/// </summary>
public interface IRideOccurrenceRepository
{
    Task<PendingPassportMutationVisit?> GetPendingMutationAsync(
        string userId,
        VisitId visitId,
        CancellationToken cancellationToken);

    Task<PendingPassportMutationVisit?> GetPendingMutationFencedAsync(
        string userId,
        VisitId visitId,
        long contentFenceToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PendingPassportMutationVisit>>
        ListPendingAuditMutationVisitsAsync(
            int maximumVisitCount,
            CancellationToken cancellationToken);

    Task<int> ReconcileProvisionalCreationAllocationsAsync(
        int maximumDocumentCount,
        CancellationToken cancellationToken);

    Task<bool> TryCompletePendingMutationAsync(
        PendingPassportMutationVisit mutation,
        CancellationToken cancellationToken);

    Task<bool> TryRejectPendingMutationAsync(
        PendingPassportMutationVisit mutation,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken);

    Task<RideOccurrenceCreationKeyReservationResult>
        ResolveBatchCreationKeyReservationAsync(
            RideOccurrenceCreationRequest request,
            string clientOperationId,
            CancellationToken cancellationToken);

    Task<RideOccurrenceCreationKeyReservationResult> ReserveBatchCreationKeyAsync(
        RideOccurrenceCreationRequest request,
        RideOccurrenceCreationPreparation preparation,
        string clientOperationId,
        DateTime reservedAtUtc,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceCreationResult?> ResolveExistingBatchCreationAsync(
        RideOccurrenceCreationRequest request,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAsync(
        RideOccurrenceCreationRequest request,
        IReadOnlyList<RideOccurrence> occurrences,
        long? expectedLastSortPosition,
        bool wasOrderNormalized,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAuditedAsync(
        RideOccurrenceCreationRequest request,
        IReadOnlyList<RideOccurrence> occurrences,
        long? expectedLastSortPosition,
        bool wasOrderNormalized,
        string clientOperationId,
        IReadOnlyCollection<PassportAuditEvent> pendingAuditEvents,
        CancellationToken cancellationToken);

    Task<RideOccurrence?> GetOwnedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken);

    Task<RideOccurrence?> GetOwnedByIdAsync(
        RideOccurrenceId occurrenceId,
        string userId,
        CancellationToken cancellationToken);

    Task<RideOccurrencePage> ListOwnedByVisitAsync(
        RideOccurrenceListCriteria criteria,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RideOccurrence>> ListAllOwnedForExportAsync(
        string userId,
        IReadOnlyCollection<VisitId> activeVisitIds,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken);

    Task<RideOccurrenceAppendState> GetAppendStateAsync(
        VisitId visitId,
        string userId,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateOwnedAuditedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateOwnedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateOwnedAuditedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        long contentFenceToken,
        CancellationToken cancellationToken);

    Task<bool> TryConfirmOwnedVersionAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<bool> TryConfirmOwnedVersionFencedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken);

    Task<bool> TryDeleteOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<bool> TryDeleteOwnedAuditedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken);

    Task<bool> TryDeleteOwnedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        long contentFenceToken,
        CancellationToken cancellationToken);

    Task<bool> TryDeleteOwnedAuditedFencedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        long contentFenceToken,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceReorderResult?> ResolveExistingReorderAsync(
        RideOccurrenceReorderRequest request,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        string? relatedCreationClientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentAuditedAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        IReadOnlyCollection<RideOccurrenceOrderGuard> guards,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        string? relatedCreationClientOperationId,
        IReadOnlyCollection<PassportAuditEvent> pendingAuditEvents,
        CancellationToken cancellationToken);
}
