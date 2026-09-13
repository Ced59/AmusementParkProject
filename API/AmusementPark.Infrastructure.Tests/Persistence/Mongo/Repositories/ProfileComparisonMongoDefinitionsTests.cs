using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ProfileComparisonMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectTokenInvitationAndBothParticipantLookups()
    {
        IReadOnlyCollection<CreateIndexModel<ProfileComparisonDocument>> indexes =
            ProfileComparisonMongoDefinitions.BuildIndexes();

        Assert.Equal(4, indexes.Count);
        CreateIndexModel<ProfileComparisonDocument> token = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ProfileComparisonMongoDefinitions.ShareTokenUniqueIndexName);
        Assert.True(token.Options.Unique);
        Assert.Equal(new BsonDocument("shareToken", 1), Render(token.Keys));
        CreateIndexModel<ProfileComparisonDocument> invitation = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ProfileComparisonMongoDefinitions.InvitationUniqueIndexName);
        Assert.True(invitation.Options.Unique);
        CreateIndexModel<ProfileComparisonDocument> creator = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ProfileComparisonMongoDefinitions.CreatorIndexName);
        Assert.Equal(
            new BsonDocument
            {
                { "creatorUserId", 1 },
                { "status", 1 },
                { "createdAt", -1 },
                { "_id", -1 },
            },
            Render(creator.Keys));
        CreateIndexModel<ProfileComparisonDocument> acceptor = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ProfileComparisonMongoDefinitions.AcceptorIndexName);
        Assert.Equal(
            new BsonDocument
            {
                { "acceptorUserId", 1 },
                { "status", 1 },
                { "createdAt", -1 },
                { "_id", -1 },
            },
            Render(acceptor.Keys));
    }

    [Fact]
    public void BuildVersionFilter_ShouldFenceOneComparisonVersion()
    {
        BsonDocument filter = Render(
            ProfileComparisonMongoDefinitions.BuildVersionFilter("comparison-1", 2));

        Assert.Equal("comparison-1", filter["_id"].AsString);
        Assert.Equal(2, filter["version"].AsInt64);
    }

    [Fact]
    public void BuildActiveParticipantPageFilter_WithCursor_ShouldUseStableSortTuple()
    {
        DateTime createdAtUtc = new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

        BsonDocument filter = Render(
            ProfileComparisonMongoDefinitions.BuildActiveParticipantPageFilter(
                "user-1",
                new ProfileComparisonListCursor(createdAtUtc, "comparison-9")));
        string json = filter.ToJson();

        Assert.Contains("createdAt", json, StringComparison.Ordinal);
        Assert.Contains("$lt", json, StringComparison.Ordinal);
        Assert.Contains("comparison-9", json, StringComparison.Ordinal);
        Assert.Contains("_id", json, StringComparison.Ordinal);
    }

    private static BsonDocument Render(FilterDefinition<ProfileComparisonDocument> filter)
    {
        IBsonSerializer<ProfileComparisonDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ProfileComparisonDocument>();
        return filter.Render(new RenderArgs<ProfileComparisonDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(IndexKeysDefinition<ProfileComparisonDocument> keys)
    {
        IBsonSerializer<ProfileComparisonDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ProfileComparisonDocument>();
        return keys.Render(new RenderArgs<ProfileComparisonDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }
}
