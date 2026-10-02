using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalParkRolloutGateCache
{
    Task<HistoricalParkRolloutGate> GetOrCreateAsync(
        string parkId,
        string scopeFingerprint,
        Func<CancellationToken, Task<HistoricalParkRolloutGate>> factory,
        CancellationToken cancellationToken);

    void Invalidate();
}
