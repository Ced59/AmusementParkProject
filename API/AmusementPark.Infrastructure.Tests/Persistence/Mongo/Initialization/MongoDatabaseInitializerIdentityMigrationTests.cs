using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class MongoDatabaseInitializerIdentityMigrationTests
{
    [Fact]
    public void PublicIdentityMigrationFilter_ShouldOnlySelectLegacyOrIncompleteIdentities()
    {
        FilterDefinition<UserDocument> filter =
            MongoDatabaseInitializer.BuildPublicIdentityMigrationFilter();
        IBsonSerializer<UserDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserDocument>();
        RenderArgs<UserDocument> arguments =
            new RenderArgs<UserDocument>(serializer, BsonSerializer.SerializerRegistry);

        BsonDocument renderedFilter = filter.Render(arguments);
        string renderedJson = renderedFilter.ToJson();

        Assert.Contains("publicAccountNumber", renderedJson, StringComparison.Ordinal);
        Assert.Contains("publicDisplayName", renderedJson, StringComparison.Ordinal);
        Assert.Contains("usesAutomaticPublicDisplayName", renderedJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"roles\"", renderedJson, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyCommentFilter_ShouldIncludeOldPersonalNameEvenWhenPublicNameExists()
    {
        FilterDefinition<CommentDocument> filter =
            MongoDatabaseInitializer.BuildLegacyCommentAuthorSnapshotFilter();
        IBsonSerializer<CommentDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<CommentDocument>();
        RenderArgs<CommentDocument> arguments =
            new RenderArgs<CommentDocument>(serializer, BsonSerializer.SerializerRegistry);

        BsonDocument renderedFilter = filter.Render(arguments);
        string renderedJson = renderedFilter.ToJson();

        Assert.Contains("\"authorDisplayName\"", renderedJson, StringComparison.Ordinal);
        Assert.Contains("\"authorPublicDisplayName\"", renderedJson, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyCommentUpdate_ShouldWritePublicNameAndRemoveOldPersonalName()
    {
        CommentDocument comment = new CommentDocument
        {
            Id = "comment-1",
            AuthorUserId = "user-1",
            UpdatedAt = new DateTime(2026, 7, 29, 18, 30, 0, DateTimeKind.Utc),
        };
        UserDocument author = new UserDocument
        {
            Id = "user-1",
            PublicDisplayName = "  CoasterFan  ",
            AvatarUrl = "/images/avatar-1",
            PublicAccountNumber = 8,
            Roles = new List<Role> { Role.User },
        };
        UpdateDefinition<CommentDocument> update =
            MongoDatabaseInitializer.BuildLegacyCommentAuthorSnapshotUpdate(comment, author);
        IBsonSerializer<CommentDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<CommentDocument>();
        RenderArgs<CommentDocument> arguments =
            new RenderArgs<CommentDocument>(serializer, BsonSerializer.SerializerRegistry);

        BsonDocument renderedUpdate = update.Render(arguments).AsBsonDocument;

        Assert.Equal("CoasterFan", renderedUpdate["$set"]["authorPublicDisplayName"].AsString);
        Assert.Equal("/images/avatar-1", renderedUpdate["$set"]["authorAvatarUrl"].AsString);
        Assert.True(renderedUpdate["$unset"].AsBsonDocument.Contains("authorDisplayName"));
    }

    [Fact]
    public void LegacyCommentUpdate_ShouldUseNeutralFallbackWhenAuthorIsMissing()
    {
        CommentDocument comment = new CommentDocument
        {
            Id = "comment-1",
            AuthorUserId = "deleted-user",
        };
        UpdateDefinition<CommentDocument> update =
            MongoDatabaseInitializer.BuildLegacyCommentAuthorSnapshotUpdate(comment, null);
        IBsonSerializer<CommentDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<CommentDocument>();
        RenderArgs<CommentDocument> arguments =
            new RenderArgs<CommentDocument>(serializer, BsonSerializer.SerializerRegistry);

        BsonDocument renderedUpdate = update.Render(arguments).AsBsonDocument;

        Assert.Equal("User", renderedUpdate["$set"]["authorPublicDisplayName"].AsString);
        Assert.False(renderedUpdate["$set"]["authorAvatarUrl"].IsString);
        Assert.True(renderedUpdate["$unset"].AsBsonDocument.Contains("authorDisplayName"));
    }
}
