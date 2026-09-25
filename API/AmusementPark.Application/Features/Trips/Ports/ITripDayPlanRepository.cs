using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripDayPlanRepository
{
    Task<IReadOnlyCollection<TripDayPlan>> ListAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripDayPlanWriteResult> PutAsync(
        TripDayPlan dayPlan,
        long? expectedVersion,
        TripChildMutationLease lease,
        string requestHash,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<TripDayPlanWriteResult> DeleteAsync(
        TripPlanId tripPlanId,
        DateOnly localDate,
        long expectedVersion,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);
}
