using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Ports;

public interface IParkFitPilotMetricsRepository
{
    Task IncrementAsync(
        DateOnly dateUtc,
        ParkFitPilotObservation observation,
        CancellationToken cancellationToken);

    Task<ParkFitPilotMetricsSnapshot> ReadAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
