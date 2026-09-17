using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class WatchlistAccountDeletionService : IWatchlistAccountDeletionService
{
    private readonly IWatchlistAccountDeletionFence deletionFence;
    private readonly IWatchlistAccountDeletionStore store;

    public WatchlistAccountDeletionService(
        IWatchlistAccountDeletionFence deletionFence,
        IWatchlistAccountDeletionStore store)
    {
        this.deletionFence = deletionFence ?? throw new ArgumentNullException(nameof(deletionFence));
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<WatchlistAccountDeletionResult> DeleteAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        await this.deletionFence.BlockAsync(normalizedUserId, cancellationToken);
        return await this.store.PurgeAsync(normalizedUserId, cancellationToken);
    }
}
