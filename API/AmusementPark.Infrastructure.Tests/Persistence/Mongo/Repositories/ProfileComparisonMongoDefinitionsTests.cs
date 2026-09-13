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
    }

    [Fact]
    public void BuildVersionFilter_ShouldFenceOneComparisonVersion()
    {
        BsonDocument filter = Render(
            ProfileComparisonMongoDefinitions.BuildVersionFilter("comparison-1", 2));

        Assert.Equal("comparison-1", filter["_id"].AsString);
        Assert.Equal(2, filter["version"].AsInt64);
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
