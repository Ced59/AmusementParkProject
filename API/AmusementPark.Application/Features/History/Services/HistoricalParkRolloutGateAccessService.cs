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
        PublicParkHistoricalScope? scope = await this.dataLoader.LoadScopeAsync(
            parkId,
            cancellationToken);
        return scope is not null
            && (await this.dataLoader.AssessRolloutGateAsync(scope, cancellationToken)).IsOpen;
    }
}
