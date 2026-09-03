using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Persistance privée des occurrences, toujours bornée à la visite et au propriétaire.
/// </summary>
public interface IRideOccurrenceRepository
{
    Task<IdempotentRideOccurrenceCreationResult?> ResolveExistingBatchCreationAsync(
        IReadOnlyList<RideOccurrence> requestedOccurrences,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceCreationResult> CreateBatchIdempotentAsync(
        IReadOnlyList<RideOccurrence> occurrences,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<RideOccurrence?> GetOwnedAsync(
        RideOccurrenceId occurrenceId,
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken);

    Task<RideOccurrencePage> ListOwnedByVisitAsync(
        RideOccurrenceListCriteria criteria,
        CancellationToken cancellationToken);

    Task<long?> GetLastSortPositionAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateOwnedAsync(
        RideOccurrence occurrence,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceReorderResult?> ResolveExistingReorderAsync(
        RideOccurrenceReorderRequest request,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentRideOccurrenceReorderResult> ReorderIdempotentAsync(
        RideOccurrenceReorderRequest request,
        IReadOnlyCollection<RideOccurrenceVersionedChange> changes,
        RideOccurrence resultOccurrence,
        bool wasNormalized,
        DateTime operationAtUtc,
        string clientOperationId,
        CancellationToken cancellationToken);
}
