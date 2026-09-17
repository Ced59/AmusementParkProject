using AmusementPark.Application.Features.Watchlists.Results;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface IWatchlistAccountDeletionStore
{
    Task<WatchlistAccountDeletionResult> PurgeAsync(
        string userId,
        CancellationToken cancellationToken);
}
