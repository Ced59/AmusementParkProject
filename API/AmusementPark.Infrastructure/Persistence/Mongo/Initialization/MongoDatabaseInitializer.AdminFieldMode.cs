using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private const string AdminFieldModeItemProgressCollectionName = "adminFieldModeItemProgress";

    private async Task InitializeAdminFieldModeItemProgressAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> collection = this.database.GetCollection<BsonDocument>(AdminFieldModeItemProgressCollectionName);

        await this.CleanupAdminFieldModeItemProgressAsync(collection, cancellationToken);

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

    private async Task CleanupAdminFieldModeItemProgressAsync(IMongoCollection<BsonDocument> collection, CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument> invalidFilter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Type("parkId", BsonType.String)),
            Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Type("itemId", BsonType.String)),
            Builders<BsonDocument>.Filter.Eq("parkId", string.Empty),
            Builders<BsonDocument>.Filter.Eq("itemId", string.Empty));

        await collection.DeleteManyAsync(invalidFilter, cancellationToken);

        List<BsonDocument> documents = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync(cancellationToken);
        BsonValue[] duplicateIds = documents
            .Where(static document => document.GetValue("_id", BsonNull.Value) != BsonNull.Value)
            .GroupBy(static document => string.Concat(document.GetValue("parkId").AsString, "|", document.GetValue("itemId").AsString), StringComparer.Ordinal)
            .SelectMany(static group => group
                .OrderByDescending(static document => document.GetValue("updatedAtUtc", BsonNull.Value).IsValidDateTime
                    ? document.GetValue("updatedAtUtc").ToUniversalTime()
                    : DateTime.MinValue)
                .Skip(1)
                .Select(static document => document.GetValue("_id")))
            .ToArray();

        if (duplicateIds.Length == 0)
        {
            return;
        }

        await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", duplicateIds), cancellationToken);
    }
}
