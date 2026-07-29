using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class EntityMongoMappersCommentsAndUsersTests
{
    [Fact]
    public void UserRoundTrip_ShouldPreserveThePublicDisplayName()
    {
        User source = new User
        {
            Id = "user-1",
            PublicDisplayName = "CoasterFan",
            UsesAutomaticPublicDisplayName = false,
            Roles = new List<Role> { Role.User },
        };
        source.AssignPublicAccountNumber(42);

        User result = source.ToDocument().ToDomain();

        Assert.Equal("CoasterFan", result.PublicDisplayName);
        Assert.Equal(42, result.PublicAccountNumber);
        Assert.False(result.UsesAutomaticPublicDisplayName);
    }

    [Fact]
    public void CommentRoundTrip_ShouldUseThePublicSnapshotFields()
    {
        Comment source = new Comment
        {
            Id = "comment-1",
            AuthorUserId = "user-1",
            AuthorDisplayName = "CoasterFan",
            AuthorAvatarUrl = "/images/avatar-1",
            AuthorRole = Role.Moderator,
        };

        CommentDocument document = source.ToDocument();
        BsonDocument bson = document.ToBsonDocument();
        Comment result = document.ToDomain();

        Assert.True(bson.Contains("authorPublicDisplayName"));
        Assert.False(bson.Contains("authorDisplayName"));
        Assert.Equal("CoasterFan", result.AuthorDisplayName);
        Assert.Equal("/images/avatar-1", result.AuthorAvatarUrl);
    }

    [Fact]
    public void LegacyCommentWithoutPublicSnapshot_ShouldNotExposeTheOldPersonalName()
    {
        BsonDocument legacyBson = new BsonDocument
        {
            ["_id"] = "comment-1",
            ["authorUserId"] = "user-1",
            ["authorDisplayName"] = "Alice Martin",
            ["authorRole"] = Role.Admin.ToString(),
        };
        CommentDocument document = MongoDB.Bson.Serialization.BsonSerializer
            .Deserialize<CommentDocument>(legacyBson);

        Comment result = document.ToDomain();

        Assert.Equal("User", result.AuthorDisplayName);
    }
}
