using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripChildMutationLeaseRepository
{
    Task<TripChildMutationLease?> TryAcquireOwnedAsync(
        TripPlanId tripPlanId,
        string ownerUserId,
        TripMemberId actorMemberId,
        long expectedPlanVersion,
        long childMutationEpoch,
        string operationId,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        TripPlanId tripPlanId,
        TripChildMutationLease lease,
        CancellationToken cancellationToken);
}
