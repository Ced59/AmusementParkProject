using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private const string AdminFieldModeItemProgressCollectionName = "adminFieldModeItemProgress";

    private async Task InitializeAdminFieldModeItemProgressAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> collection = this.database.GetCollection<BsonDocument>(AdminFieldModeItemProgressCollectionName);

        CreateIndexModel<BsonDocument>[] indexes =
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending("parkId")
                    .Ascending("itemId"),
                new CreateIndexOptions
                {
                    Name = "ux_admin_field_mode_progress_park_item",
                    Unique = true,
                }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending("parkId")
                    .Ascending("isProcessed"),
                new CreateIndexOptions
                {
                    Name = "ix_admin_field_mode_progress_park_processed",
                }),
        ];

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }
}
