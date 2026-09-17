using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class WatchlistAccountDeletionService : IWatchlistAccountDeletionService
{
    private readonly IWatchlistAccountDeletionStore store;

    public WatchlistAccountDeletionService(IWatchlistAccountDeletionStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Task<WatchlistAccountDeletionResult> DeleteAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        return this.store.PurgeAsync(normalizedUserId, cancellationToken);
    }
}
