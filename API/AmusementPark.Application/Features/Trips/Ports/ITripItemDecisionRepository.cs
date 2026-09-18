using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripItemDecisionRepository
{
    Task<IReadOnlyCollection<TripItemDecision>> ListAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripItemDecision?> GetAsync(
        TripPlanId tripPlanId,
        string parkItemId,
        CancellationToken cancellationToken);

    Task<TripItemDecisionWriteResult> CreateAsync(
        TripItemDecision decision,
        TripChildMutationLease lease,
        CancellationToken cancellationToken);

    Task<TripItemDecisionWriteResult> ReplaceAsync(
        TripItemDecision decision,
        long expectedVersion,
        TripChildMutationLease lease,
        CancellationToken cancellationToken);
}
