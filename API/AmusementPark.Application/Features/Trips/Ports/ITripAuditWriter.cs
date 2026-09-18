using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripAuditWriter
{
    Task<TripActivityEvent> AppendAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken);
}
