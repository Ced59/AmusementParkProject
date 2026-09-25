using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripAuditReader
{
    Task<IReadOnlyCollection<TripActivityEvent>> ListAsync(
        TripPlanId tripPlanId,
        long? beforeSequence,
        int limit,
        CancellationToken cancellationToken);
}
