namespace AmusementPark.Application.Features.History.Services;

public interface IHistoricalParkPublicTimelineAccessService
{
    Task<bool> IsAvailableAsync(string parkId, CancellationToken cancellationToken);
}
