using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRepositoryConcurrencyTests
{
    [Fact]
    public void BuildUnchangedFilter_ShouldRequireExactUserAndObservedUpdateDate()
    {
        DateTime expectedUpdatedAtUtc = new DateTime(
            2026,
            9,
            6,
            10,
            15,
            30,
            DateTimeKind.Utc);

        FilterDefinition<UserDocument> filter = UserRepository.BuildUnchangedFilter(
            "user-1",
            expectedUpdatedAtUtc);

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["_id"].AsString);
        Assert.Equal(expectedUpdatedAtUtc, rendered["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public void BuildAvatarUrlUpdate_ShouldTouchOnlyTheAvatarAndUpdateDate()
    {
        DateTime updatedAtUtc = new DateTime(
            2026,
            9,
            6,
            16,
            45,
            0,
            DateTimeKind.Utc);

        UpdateDefinition<UserDocument> update = UserRepository.BuildAvatarUrlUpdate(
            " /images/avatar-2 ",
            updatedAtUtc);

        BsonDocument rendered = Render(update);

        BsonDocument set = rendered["$set"].AsBsonDocument;
        Assert.Equal("/images/avatar-2", set["avatarUrl"].AsString);
        Assert.Equal(updatedAtUtc, set["updatedAt"].ToUniversalTime());
        Assert.Equal(2, set.ElementCount);
        Assert.False(rendered.Contains("roles"));
        Assert.False(rendered.Contains("isBlocked"));
    }

    [Fact]
    public void BuildAvatarUrlUpdate_WhenAvatarIsRemoved_ShouldUnsetOnlyTheAvatar()
    {
        DateTime updatedAtUtc = new DateTime(
            2026,
            9,
            6,
            16,
            50,
            0,
            DateTimeKind.Utc);

        UpdateDefinition<UserDocument> update = UserRepository.BuildAvatarUrlUpdate(
            null,
            updatedAtUtc);

        BsonDocument rendered = Render(update);

        Assert.Single(rendered["$unset"].AsBsonDocument);
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("avatarUrl"));
        BsonDocument set = rendered["$set"].AsBsonDocument;
        Assert.Single(set);
        Assert.Equal(updatedAtUtc, set["updatedAt"].ToUniversalTime());
    }

    private static BsonDocument Render(FilterDefinition<UserDocument> filter)
    {
        IBsonSerializer<UserDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserDocument>();
        RenderArgs<UserDocument> arguments = new RenderArgs<UserDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(UpdateDefinition<UserDocument> update)
    {
        IBsonSerializer<UserDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserDocument>();
        RenderArgs<UserDocument> arguments = new RenderArgs<UserDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
    }
}
