namespace AmusementPark.Application.Features.History.Services;

public interface IHistoricalParkRolloutGateAccessService
{
    Task<bool> IsOpenAsync(string parkId, CancellationToken cancellationToken);
}
