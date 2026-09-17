using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripPlanRepository
{
    Task<IdempotentTripPlanCreationResult?> ResolveExistingCreationAsync(
        TripPlan requestedTripPlan,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentTripPlanCreationResult> CreateIdempotentAsync(
        TripPlan tripPlan,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripPlan>> ListAccessibleAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<TripPlan?> GetAccessibleAsync(
        string userId,
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripPlan?> GetOwnedAsync(
        string userId,
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<long?> GetProgramReadSequenceAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripPlanWriteResult> ReplaceOwnedAsync(
        TripPlan tripPlan,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<TripPlanWriteResult> ReplaceOwnedUnderChildLeaseAsync(
        TripPlan tripPlan,
        long expectedVersion,
        TripChildMutationLease lease,
        CancellationToken cancellationToken);

    Task<TripPlanWriteResult> DeleteOwnedAsync(
        TripPlan tripPlan,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task PurgeChildrenAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripPlanWriteResult> FinalizeDeletionOwnedAsync(
        TripPlan tripPlan,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripPlan>> ListPendingDeletionAsync(
        int limit,
        CancellationToken cancellationToken);
}
