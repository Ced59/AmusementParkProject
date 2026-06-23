using AmusementPark.Application.Features.TechnicalStats.Contracts;

namespace AmusementPark.Application.Features.TechnicalStats.Ports;

public interface ITechnicalStatsProvider
{
    Task<TechnicalStatsSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
