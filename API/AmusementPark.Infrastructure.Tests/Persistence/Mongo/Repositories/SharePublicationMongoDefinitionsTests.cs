using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class SharePublicationMongoDefinitionsTests
{
    [Fact]
    public void BuildResolvableTokenFilter_ShouldRequireTheExactTokenAndResolvableLifecycle()
    {
        FilterDefinition<SharePublicationDocument> filter =
            SharePublicationMongoDefinitions.BuildResolvableTokenFilter("token-value");

        BsonDocument rendered = Render(filter);
        string json = rendered.ToJson();

        Assert.Contains("\"shareToken\" : \"token-value\"", json, StringComparison.Ordinal);
        Assert.Contains("\"status\" : \"Published\"", json, StringComparison.Ordinal);
        Assert.Contains("\"visibility\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Unlisted\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Public\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ownerUserId", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildOwnedVersionFilter_ShouldFenceTheOwnerAndPersistenceVersion()
    {
        FilterDefinition<SharePublicationDocument> filter =
            SharePublicationMongoDefinitions.BuildOwnedVersionFilter(
                "publication-1",
                "user-1",
                6);

        BsonDocument rendered = Render(filter);

        Assert.Equal("publication-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["ownerUserId"].AsString);
        Assert.Equal(6, rendered["version"].AsInt64);
    }

    [Fact]
    public void BuildSharePublicationIndexes_ShouldProtectTokensWithoutCreatingAPublicListingIndex()
    {
        IReadOnlyCollection<CreateIndexModel<SharePublicationDocument>> indexes =
            SharePublicationMongoDefinitions.BuildIndexes();

        Assert.Equal(3, indexes.Count);
        CreateIndexModel<SharePublicationDocument> token = Assert.Single(
            indexes,
            static index => string.Equals(
                index.Options.Name,
                SharePublicationMongoDefinitions.ShareTokenUniqueIndexName,
                StringComparison.Ordinal));
        CreateIndexOptions<SharePublicationDocument> tokenOptions =
            Assert.IsType<CreateIndexOptions<SharePublicationDocument>>(token.Options);
        Assert.True(tokenOptions.Unique);
        Assert.Equal(new BsonDocument("shareToken", 1), Render(token.Keys));
        Assert.Equal(
            new BsonDocument("shareToken", new BsonDocument("$type", "string")),
            Render(tokenOptions.PartialFilterExpression!));

        CreateIndexModel<SharePublicationDocument> owner = Assert.Single(
            indexes,
            static index => string.Equals(
                index.Options.Name,
                SharePublicationMongoDefinitions.OwnerLifecycleIndexName,
                StringComparison.Ordinal));
        Assert.Equal(
            new BsonDocument
            {
                { "ownerUserId", 1 },
                { "type", 1 },
                { "updatedAt", -1 },
            },
            Render(owner.Keys));
        Assert.All(indexes, static index => Assert.Null(index.Options.ExpireAfter));
        Assert.DoesNotContain(
            indexes,
            static index => Render(index.Keys).Names.SequenceEqual(
                new[] { "status", "visibility" }));
    }

    private static BsonDocument Render(FilterDefinition<SharePublicationDocument> filter)
    {
        IBsonSerializer<SharePublicationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<SharePublicationDocument>();
        return filter.Render(
            new RenderArgs<SharePublicationDocument>(serializer, BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(IndexKeysDefinition<SharePublicationDocument> keys)
    {
        IBsonSerializer<SharePublicationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<SharePublicationDocument>();
        return keys.Render(
            new RenderArgs<SharePublicationDocument>(serializer, BsonSerializer.SerializerRegistry));
    }
}
