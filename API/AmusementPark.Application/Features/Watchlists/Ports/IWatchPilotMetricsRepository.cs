using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface IWatchPilotMetricsRepository
{
    Task IncrementInteractionAsync(
        DateOnly dateUtc,
        WatchPilotInteractionKind interactionKind,
        CancellationToken cancellationToken);

    Task<WatchPilotMetricsSnapshot> ReadAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
