using AmusementPark.Application.Features.Trips.Models;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripPilotMetricsRepository
{
    Task<TripPilotMetricsSnapshot> ReadAsync(CancellationToken cancellationToken);
}
