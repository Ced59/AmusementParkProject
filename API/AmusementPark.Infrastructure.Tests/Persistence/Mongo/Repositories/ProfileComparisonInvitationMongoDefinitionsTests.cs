using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ProfileComparisonInvitationMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectOpaqueTokensAndExpireOnlyPurgeableRecords()
    {
        IReadOnlyCollection<CreateIndexModel<ProfileComparisonInvitationDocument>> indexes =
            ProfileComparisonInvitationMongoDefinitions.BuildIndexes();

        Assert.Equal(4, indexes.Count);
        CreateIndexModel<ProfileComparisonInvitationDocument> token = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ProfileComparisonInvitationMongoDefinitions.TokenUniqueIndexName);
        Assert.True(token.Options.Unique);
        Assert.Equal(new BsonDocument("token", 1), Render(token.Keys));
        CreateIndexModel<ProfileComparisonInvitationDocument> acceptor = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ProfileComparisonInvitationMongoDefinitions.AcceptorIndexName);
        Assert.Equal(
            new BsonDocument { { "acceptorUserId", 1 }, { "createdAt", -1 } },
            Render(acceptor.Keys));
        Assert.Equal(
            new BsonDocument(
                "acceptorUserId",
                new BsonDocument("$type", "string")),
            Render(acceptor.Options.PartialFilterExpression!));
        CreateIndexModel<ProfileComparisonInvitationDocument> purge = Assert.Single(
            indexes,
            static index => index.Options.Name
                == ProfileComparisonInvitationMongoDefinitions.PurgeIndexName);
        Assert.Equal(TimeSpan.Zero, purge.Options.ExpireAfter);
        Assert.Equal(new BsonDocument("purgeAtUtc", 1), Render(purge.Keys));
    }

    [Fact]
    public void BuildVersionFilter_ShouldFenceOneInvitationVersion()
    {
        BsonDocument filter = Render(
            ProfileComparisonInvitationMongoDefinitions.BuildVersionFilter(
                "invitation-1",
                3));

        Assert.Equal("invitation-1", filter["_id"].AsString);
        Assert.Equal(3, filter["version"].AsInt64);
    }

    [Fact]
    public void BuildAcceptedVersionFilter_ShouldCompensateOnlyTheJustAcceptedVersion()
    {
        BsonDocument filter = Render(
            ProfileComparisonInvitationMongoDefinitions.BuildAcceptedVersionFilter(
                "invitation-1",
                4));

        Assert.Equal("invitation-1", filter["_id"].AsString);
        Assert.Equal(4, filter["version"].AsInt64);
        Assert.Equal("Accepted", filter["status"].AsString);
    }

    [Fact]
    public void BuildParticipantExportFilter_ShouldIncludeCreatorAndAcceptorHistories()
    {
        string json = Render(
            ProfileComparisonInvitationMongoDefinitions.BuildParticipantExportFilter("user-1"))
            .ToJson();

        Assert.Contains("creatorUserId", json, StringComparison.Ordinal);
        Assert.Contains("acceptorUserId", json, StringComparison.Ordinal);
        Assert.Contains("user-1", json, StringComparison.Ordinal);
        Assert.Contains("$or", json, StringComparison.Ordinal);
    }

    private static BsonDocument Render(
        FilterDefinition<ProfileComparisonInvitationDocument> filter)
    {
        IBsonSerializer<ProfileComparisonInvitationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ProfileComparisonInvitationDocument>();
        return filter.Render(
            new RenderArgs<ProfileComparisonInvitationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(
        IndexKeysDefinition<ProfileComparisonInvitationDocument> keys)
    {
        IBsonSerializer<ProfileComparisonInvitationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ProfileComparisonInvitationDocument>();
        return keys.Render(
            new RenderArgs<ProfileComparisonInvitationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));
    }
}
