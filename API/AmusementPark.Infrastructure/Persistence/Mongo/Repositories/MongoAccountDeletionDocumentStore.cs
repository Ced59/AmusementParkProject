using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoAccountDeletionDocumentStore
{
    private readonly IMongoDatabase database;

    public MongoAccountDeletionDocumentStore(IMongoDatabase database)
    {
        this.database = database;
    }

    public IMongoCollection<BsonDocument> Collection(string collectionName)
    {
        return this.database.GetCollection<BsonDocument>(collectionName);
    }

    public async Task<string[]> ListIdsAsync(
        string collectionName,
        FilterDefinition<BsonDocument> filter,
        CancellationToken cancellationToken)
    {
        List<BsonDocument> documents = await this.Collection(collectionName)
            .Find(filter)
            .Project(Builders<BsonDocument>.Projection.Include("_id"))
            .ToListAsync(cancellationToken);
        return documents
            .Where(static document => document.TryGetValue("_id", out BsonValue? id)
                && id.IsString)
            .Select(static document => document["_id"].AsString)
            .ToArray();
    }

    public async Task<long> DeleteManyAsync(
        string collectionName,
        FilterDefinition<BsonDocument> filter,
        CancellationToken cancellationToken)
    {
        DeleteResult result = await this.Collection(collectionName)
            .DeleteManyAsync(filter, cancellationToken);
        return result.DeletedCount;
    }

    public Task AnonymizeAsync(
        string collectionName,
        string field,
        string userId,
        UpdateDefinition<BsonDocument> update,
        CancellationToken cancellationToken)
    {
        return this.Collection(collectionName).UpdateManyAsync(
            Eq(field, userId),
            update,
            cancellationToken: cancellationToken);
    }

    public static FilterDefinition<BsonDocument> Eq(string field, string value)
    {
        return Builders<BsonDocument>.Filter.Eq(field, value);
    }

    public static FilterDefinition<BsonDocument> And(
        params FilterDefinition<BsonDocument>[] filters)
    {
        return Builders<BsonDocument>.Filter.And(filters);
    }

    public static FilterDefinition<BsonDocument> Or(
        params FilterDefinition<BsonDocument>[] filters)
    {
        return Builders<BsonDocument>.Filter.Or(filters);
    }
}
