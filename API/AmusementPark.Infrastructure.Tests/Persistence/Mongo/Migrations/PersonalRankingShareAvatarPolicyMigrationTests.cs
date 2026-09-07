using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Migrations;

public sealed class PersonalRankingShareAvatarPolicyMigrationTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 7, 2, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Definitions_ShouldPreserveDraftPublicationVersion()
    {
        IBsonSerializer<SharePublicationDocument> serializer =
            BsonSerializer.LookupSerializer<SharePublicationDocument>();
        BsonDocument filter = PersonalRankingShareAvatarPolicyMigration
            .BuildDraftPolicyFilter()
            .Render(new RenderArgs<SharePublicationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));
        BsonDocument update = PersonalRankingShareAvatarPolicyMigration
            .BuildDraftCorrectionUpdate(NowUtc)
            .Render(new RenderArgs<SharePublicationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry))
            .AsBsonDocument;

        Assert.Equal("PersonalRanking", filter["type"].AsString);
        Assert.Equal(
            (int)ShareContentField.Avatar,
            filter["contentPolicy.includedFields"].AsInt32);
        Assert.Equal("Draft", filter["status"].AsString);
        Assert.Equal(
            (int)ShareContentField.Avatar,
            update["$pull"]["contentPolicy.includedFields"].AsInt32);
        Assert.False(update["$inc"].AsBsonDocument.Contains("publicationVersion"));
        Assert.Equal(1, update["$inc"]["version"].AsInt64);
        Assert.Equal(NowUtc, update["$max"]["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public void Definitions_ShouldAdvanceAnExistingLinkPublicationVersion()
    {
        IBsonSerializer<SharePublicationDocument> serializer =
            BsonSerializer.LookupSerializer<SharePublicationDocument>();
        BsonDocument filter = PersonalRankingShareAvatarPolicyMigration
            .BuildVersionedPolicyFilter()
            .Render(new RenderArgs<SharePublicationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));
        BsonDocument update = PersonalRankingShareAvatarPolicyMigration
            .BuildVersionedCorrectionUpdate(NowUtc)
            .Render(new RenderArgs<SharePublicationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry))
            .AsBsonDocument;

        Assert.Equal("Draft", filter["status"]["$ne"].AsString);
        Assert.Equal(1, update["$inc"]["publicationVersion"].AsInt64);
        Assert.Equal(1, update["$inc"]["version"].AsInt64);
    }
}
