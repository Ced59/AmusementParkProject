using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalParkPublicTimelineAccessService :
    IHistoricalParkPublicTimelineAccessService
{
    private readonly PublicParkHistoricalDataLoader dataLoader;

    public HistoricalParkPublicTimelineAccessService(PublicParkHistoricalDataLoader dataLoader)
    {
        this.dataLoader = dataLoader;
    }

    public async Task<bool> IsAvailableAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        PublicParkHistoricalScope? scope = await this.dataLoader.LoadScopeAsync(
            parkId,
            cancellationToken);
        if (scope is null)
        {
            return false;
        }

        HistoricalParkRolloutGate rolloutGate = await this.dataLoader.AssessRolloutGateAsync(
            scope,
            cancellationToken);
        return rolloutGate.IsOpen
            || await this.dataLoader.HasLegacyPublicTimelineAsync(scope, cancellationToken);
    }
}
