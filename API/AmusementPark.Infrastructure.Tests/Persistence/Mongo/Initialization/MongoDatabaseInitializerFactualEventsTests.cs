using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class MongoDatabaseInitializerFactualEventsTests
{
    [Fact]
    public void BuildFactualChangeOutboxIndexes_ShouldEnforceOneSourceRevision()
    {
        IReadOnlyCollection<CreateIndexModel<FactualChangeOutboxDocument>> indexes =
            MongoDatabaseInitializer.BuildFactualChangeOutboxIndexes();
        CreateIndexModel<FactualChangeOutboxDocument> logicalRevision = Assert.Single(
            indexes,
            static index => index.Options.Name == "idx_factual_outbox_logical_revision_unique");
        CreateIndexModel<FactualChangeOutboxDocument> pending = Assert.Single(
            indexes,
            static index => index.Options.Name == "idx_factual_outbox_pending");

        Assert.True(logicalRevision.Options.Unique);
        Assert.Equal(
            new BsonDocument { { "deduplicationKey", 1 }, { "sourceRevision", 1 } },
            Render(logicalRevision.Keys));
        Assert.Equal(
            new BsonDocument
            {
                { "materializedAtUtc", 1 },
                { "createdAt", 1 },
                { "_id", 1 },
            },
            Render(pending.Keys));
        Assert.Null(pending.Options.PartialFilterExpression);
    }

    [Fact]
    public void BuildFactualChangeEventIndexes_ShouldEnforceOneLogicalEvent()
    {
        IReadOnlyCollection<CreateIndexModel<FactualChangeEventDocument>> indexes =
            MongoDatabaseInitializer.BuildFactualChangeEventIndexes();
        CreateIndexModel<FactualChangeEventDocument> logicalRevision = Assert.Single(
            indexes,
            static index => index.Options.Name == "idx_factual_events_logical_revision_unique");

        Assert.True(logicalRevision.Options.Unique);
        Assert.Equal(
            new BsonDocument { { "deduplicationKey", 1 }, { "revision", 1 } },
            Render(logicalRevision.Keys));
        Assert.Contains(
            indexes,
            static index => index.Options.Name == "idx_factual_events_target_created");
    }

    private static BsonDocument Render<TDocument>(IndexKeysDefinition<TDocument> keys)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        RenderArgs<TDocument> arguments =
            new RenderArgs<TDocument>(serializer, BsonSerializer.SerializerRegistry);
        return keys.Render(arguments);
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        RenderArgs<TDocument> arguments =
            new RenderArgs<TDocument>(serializer, BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
