using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkFitOperationsMongoDefinitionsTests
{
    [Fact]
    public void SourceReportIndexes_ShouldSupportTheQueueAndParkCounters()
    {
        IReadOnlyCollection<CreateIndexModel<ParkFitSourceReportDocument>> indexes =
            ParkFitSourceReportMongoDefinitions.BuildIndexes();

        CreateIndexModel<ParkFitSourceReportDocument> queue = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ParkFitSourceReportMongoDefinitions.StatusQueueIndexName);
        Assert.Equal(
            new BsonDocument
            {
                { "status", 1 },
                { "submittedAtUtc", -1 },
                { "_id", -1 },
            },
            Render(queue.Keys));
        CreateIndexModel<ParkFitSourceReportDocument> byPark = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ParkFitSourceReportMongoDefinitions.ParkStatusIndexName);
        Assert.Equal(
            new BsonDocument
            {
                { "parkId", 1 },
                { "status", 1 },
                { "submittedAtUtc", -1 },
            },
            Render(byPark.Keys));
    }

    [Fact]
    public void OperationalStatusIndex_ShouldSupportStateMonitoring()
    {
        CreateIndexModel<ParkFitOperationalStatusDocument> index = Assert.Single(
            ParkFitOperationalStatusMongoDefinitions.BuildIndexes());

        Assert.Equal(ParkFitOperationalStatusMongoDefinitions.StateUpdatedIndexName, index.Options.Name);
        Assert.Equal(
            new BsonDocument
            {
                { "state", 1 },
                { "updatedAt", -1 },
            },
            Render(index.Keys));
    }

    [Fact]
    public void MongoSettings_ShouldUseDedicatedParkFitCollectionNames()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal("park-fit-source-reports", settings.ParkFitSourceReportsCollectionName);
        Assert.Equal(
            "park-fit-operational-statuses",
            settings.ParkFitOperationalStatusesCollectionName);
    }

    private static BsonDocument Render<TDocument>(IndexKeysDefinition<TDocument> keys)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return keys.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
