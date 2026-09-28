using AmusementPark.Application.Features.History.Handlers;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalParkRolloutGateAccessService : IHistoricalParkRolloutGateAccessService
{
    private readonly PublicParkHistoricalDataLoader dataLoader;

    public HistoricalParkRolloutGateAccessService(PublicParkHistoricalDataLoader dataLoader)
    {
        this.dataLoader = dataLoader;
    }

    public async Task<bool> IsOpenAsync(string parkId, CancellationToken cancellationToken)
    {
        PublicParkHistoricalData? data = await this.dataLoader.LoadAsync(parkId, cancellationToken);
        return data?.RolloutGate?.IsOpen == true;
    }
}
