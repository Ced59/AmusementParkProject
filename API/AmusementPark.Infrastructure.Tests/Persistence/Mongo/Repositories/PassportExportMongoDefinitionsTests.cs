using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportExportMongoDefinitionsTests
{
    [Fact]
    public void ExportAndChunkIndexes_EnforceOwnerQueriesOrderingAndShortRetention()
    {
        IReadOnlyCollection<CreateIndexModel<PassportExportDocument>> exportIndexes =
            PassportExportMongoDefinitions.BuildExportIndexes();
        IReadOnlyCollection<CreateIndexModel<PassportExportChunkDocument>> chunkIndexes =
            PassportExportMongoDefinitions.BuildChunkIndexes();

        Assert.Contains(exportIndexes, index =>
            index.Options.Name == "idx_passport_exports_owner_created"
            && Render(index.Keys).Equals(new BsonDocument
            {
                { "userId", 1 },
                { "createdAt", -1 },
            }));
        Assert.Contains(exportIndexes, index =>
            index.Options.Name == "idx_passport_exports_expiry_ttl"
            && index.Options.ExpireAfter == TimeSpan.Zero);
        Assert.Contains(chunkIndexes, index =>
            index.Options.Name == "idx_passport_export_chunks_order"
            && index.Options.Unique == true
            && Render(index.Keys).Equals(new BsonDocument
            {
                { "exportId", 1 },
                { "generationId", 1 },
                { "index", 1 },
            }));
        Assert.Contains(chunkIndexes, index =>
            index.Options.Name == "idx_passport_export_chunks_expiry_ttl"
            && index.Options.ExpireAfter == TimeSpan.Zero);
    }

    [Fact]
    public void MongoSettings_UseDedicatedPassportExportCollectionsByDefault()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal("passport-exports", settings.PassportExportsCollectionName);
        Assert.Equal("passport-export-chunks", settings.PassportExportChunksCollectionName);
    }

    private static BsonDocument Render<TDocument>(IndexKeysDefinition<TDocument> keys)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        RenderArgs<TDocument> arguments = new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);
        return keys.Render(arguments);
    }
}
