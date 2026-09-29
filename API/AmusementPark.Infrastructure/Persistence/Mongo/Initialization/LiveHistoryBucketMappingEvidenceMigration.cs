using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

internal sealed class LiveHistoryBucketMappingEvidenceMigration
{
    private readonly IMongoCollection<BsonDocument> collection;

    public LiveHistoryBucketMappingEvidenceMigration(IMongoCollection<BsonDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<long> MigrateAsync(CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteManyAsync(
            BuildFilter(),
            cancellationToken);
        return result.DeletedCount;
    }

    internal static BsonDocument BuildFilter()
    {
        return new BsonDocument(
            "samples",
            new BsonDocument(
                "$elemMatch",
                new BsonDocument(
                    "$or",
                    new BsonArray
                    {
                        new BsonDocument(
                            "externalTargetId",
                            new BsonDocument("$exists", false)),
                        new BsonDocument(
                            "mappingVersion",
                            new BsonDocument("$exists", false)),
                        new BsonDocument(
                            "externalTargetId",
                            new BsonDocument("$in", new BsonArray { BsonNull.Value, string.Empty })),
                        new BsonDocument(
                            "mappingVersion",
                            new BsonDocument("$in", new BsonArray { BsonNull.Value, string.Empty })),
                    })));
    }
}
