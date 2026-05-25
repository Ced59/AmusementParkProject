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
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
