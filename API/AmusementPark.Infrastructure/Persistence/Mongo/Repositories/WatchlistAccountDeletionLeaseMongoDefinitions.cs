using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class WatchlistAccountDeletionLeaseMongoDefinitions
{
    internal const string LookupIndexName = "idx_watchlist_deletion_lease_owner_expiry";
    internal const string RetentionIndexName = "idx_watchlist_deletion_lease_retention";

    internal static IEnumerable<CreateIndexModel<WatchlistAccountDeletionLeaseDocument>> BuildIndexes()
    {
        yield return new CreateIndexModel<WatchlistAccountDeletionLeaseDocument>(
            Builders<WatchlistAccountDeletionLeaseDocument>.IndexKeys
                .Ascending(static document => document.UserKey)
                .Ascending(static document => document.ExpiresAt),
            new CreateIndexOptions { Name = LookupIndexName });
        yield return new CreateIndexModel<WatchlistAccountDeletionLeaseDocument>(
            Builders<WatchlistAccountDeletionLeaseDocument>.IndexKeys
                .Ascending(static document => document.ExpiresAt),
            new CreateIndexOptions
            {
                Name = RetentionIndexName,
                ExpireAfter = TimeSpan.Zero,
            });
    }
}
