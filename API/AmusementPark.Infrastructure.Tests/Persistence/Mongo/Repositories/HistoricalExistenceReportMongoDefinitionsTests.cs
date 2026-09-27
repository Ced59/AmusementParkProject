using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class HistoricalExistenceReportMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldSupportPrivateVisitsReviewQueueAndPendingDeduplication()
    {
        IReadOnlyCollection<CreateIndexModel<HistoricalExistenceReportDocument>> indexes =
            HistoricalExistenceReportMongoDefinitions.BuildIndexes();

        CreateIndexModel<HistoricalExistenceReportDocument> duplicate = Assert.Single(
            indexes,
            static index => index.Options.Name
                == HistoricalExistenceReportMongoDefinitions.PendingDuplicateIndexName);
        Assert.True(duplicate.Options.Unique);
        Assert.NotNull(duplicate.Options.PartialFilterExpression);
        Assert.Equal(
            new BsonDocument
            {
                { "ownerUserId", 1 },
                { "visitId", 1 },
                { "normalizedClaimedName", 1 },
            },
            Render(duplicate.Keys));
        Assert.Contains(
            HistoricalExistenceReportStatus.Pending.ToString(),
            Render(duplicate.Options.PartialFilterExpression!).ToJson(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void MongoSettings_ShouldUseADedicatedCollection()
    {
        MongoDbSettings settings = new();

        Assert.Equal(
            "historical-existence-reports",
            settings.HistoricalExistenceReportsCollectionName);
    }

    private static BsonDocument Render<TDocument>(IndexKeysDefinition<TDocument> keys)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return keys.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return filter.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
