using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkFitGroupProfileMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectOwnerAliasAndStableRecentOrdering()
    {
        IReadOnlyCollection<CreateIndexModel<ParkFitGroupProfileDocument>> indexes =
            ParkFitGroupProfileMongoDefinitions.BuildIndexes();

        CreateIndexModel<ParkFitGroupProfileDocument> alias = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ParkFitGroupProfileMongoDefinitions.OwnerAliasUniqueIndexName);
        Assert.True(alias.Options.Unique);
        Assert.Equal(
            new BsonDocument { { "ownerUserId", 1 }, { "normalizedAlias", 1 } },
            Render(alias.Keys));
        CreateIndexModel<ParkFitGroupProfileDocument> ordering = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ParkFitGroupProfileMongoDefinitions.OwnerUpdatedIndexName);
        Assert.Equal(
            new BsonDocument
            {
                { "ownerUserId", 1 },
                { "updatedAt", -1 },
                { "_id", 1 },
            },
            Render(ordering.Keys));
    }

    [Fact]
    public void BuildOwnedVersionFilter_ShouldFenceOwnerIdAndVersion()
    {
        BsonDocument filter = Render(
            ParkFitGroupProfileMongoDefinitions.BuildOwnedVersionFilter(
                "profile-1",
                "user-1",
                4));
        string json = filter.ToJson();

        Assert.Contains("profile-1", json, StringComparison.Ordinal);
        Assert.Contains("user-1", json, StringComparison.Ordinal);
        Assert.Contains("version", json, StringComparison.Ordinal);
        Assert.Contains("4", json, StringComparison.Ordinal);
    }

    private static BsonDocument Render(FilterDefinition<ParkFitGroupProfileDocument> filter)
    {
        IBsonSerializer<ParkFitGroupProfileDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkFitGroupProfileDocument>();
        return filter.Render(new RenderArgs<ParkFitGroupProfileDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(IndexKeysDefinition<ParkFitGroupProfileDocument> keys)
    {
        IBsonSerializer<ParkFitGroupProfileDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkFitGroupProfileDocument>();
        return keys.Render(new RenderArgs<ParkFitGroupProfileDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
