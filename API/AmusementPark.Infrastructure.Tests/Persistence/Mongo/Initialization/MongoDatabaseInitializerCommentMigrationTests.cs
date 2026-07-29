using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class MongoDatabaseInitializerCommentMigrationTests
{
    [Fact]
    public void LegacyRevisionMigration_ShouldSelectOnlyMissingFieldAndInitializeItToZero()
    {
        IBsonSerializer<CommentDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<CommentDocument>();
        RenderArgs<CommentDocument> arguments =
            new RenderArgs<CommentDocument>(serializer, BsonSerializer.SerializerRegistry);

        BsonDocument renderedFilter =
            MongoDatabaseInitializer.BuildLegacyCommentRevisionFilter()
                .Render(arguments);
        BsonDocument renderedUpdate =
            MongoDatabaseInitializer.BuildLegacyCommentRevisionUpdate()
                .Render(arguments)
                .AsBsonDocument;

        Assert.False(renderedFilter["revision"]["$exists"].AsBoolean);
        Assert.Equal(0L, renderedUpdate["$set"]["revision"].AsInt64);
    }
}
