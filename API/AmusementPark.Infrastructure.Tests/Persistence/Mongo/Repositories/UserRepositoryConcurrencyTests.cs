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

    private static BsonDocument Render(FilterDefinition<UserDocument> filter)
    {
        IBsonSerializer<UserDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserDocument>();
        RenderArgs<UserDocument> arguments = new RenderArgs<UserDocument>(
            serializer,
            BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
