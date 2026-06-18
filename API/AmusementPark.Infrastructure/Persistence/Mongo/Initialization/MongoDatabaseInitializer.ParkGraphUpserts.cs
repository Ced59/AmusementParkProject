using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkGraphUpserts;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeParkGraphUpsertHistoryIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkGraphUpsertHistoryDocument> collection = this.database.GetCollection<ParkGraphUpsertHistoryDocument>(this.settings.ParkGraphUpsertHistoryCollectionName);
        List<CreateIndexModel<ParkGraphUpsertHistoryDocument>> indexes = new List<CreateIndexModel<ParkGraphUpsertHistoryDocument>>
        {
            new CreateIndexModel<ParkGraphUpsertHistoryDocument>(
                Builders<ParkGraphUpsertHistoryDocument>.IndexKeys.Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_park_graph_upsert_history_created_at_desc" }),
            new CreateIndexModel<ParkGraphUpsertHistoryDocument>(
                Builders<ParkGraphUpsertHistoryDocument>.IndexKeys.Ascending(item => item.TargetParkId).Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_park_graph_upsert_history_park_created_at" }),
            new CreateIndexModel<ParkGraphUpsertHistoryDocument>(
                Builders<ParkGraphUpsertHistoryDocument>.IndexKeys.Ascending(item => item.OperationKind).Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_park_graph_upsert_history_kind_created_at" }),
            new CreateIndexModel<ParkGraphUpsertHistoryDocument>(
                Builders<ParkGraphUpsertHistoryDocument>.IndexKeys.Ascending(item => item.ExpiresAt),
                new CreateIndexOptions
                {
                    Name = "idx_park_graph_upsert_history_expires_at_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);

        DateTime fallbackExpirationUtc = DateTime.UtcNow.AddDays(Math.Max(1, this.settings.ParkGraphUpsertHistoryRetentionDays));
        FilterDefinition<ParkGraphUpsertHistoryDocument> missingExpirationFilter =
            Builders<ParkGraphUpsertHistoryDocument>.Filter.Exists(item => item.ExpiresAt, false);
        UpdateDefinition<ParkGraphUpsertHistoryDocument> setExpirationUpdate =
            Builders<ParkGraphUpsertHistoryDocument>.Update.Set(item => item.ExpiresAt, fallbackExpirationUtc);

        await collection.UpdateManyAsync(missingExpirationFilter, setExpirationUpdate, cancellationToken: cancellationToken);
    }
}
