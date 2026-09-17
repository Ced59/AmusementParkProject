namespace AmusementPark.Application.Features.FactualEvents.Ports;

public interface IFactualChangeEventDistributionStateReader
{
    Task<bool> IsInitialDistributionCompletedAsync(
        string eventId,
        CancellationToken cancellationToken);
}
