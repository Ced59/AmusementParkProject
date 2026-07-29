using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ImageRepositoryCommentDraftFilterTests
{
    [Fact]
    public void BuildActiveCommentDraftsByOwnerFilter_ShouldExcludeDeletionRequestsButKeepReservations()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildActiveCommentDraftsByOwnerFilter("author-1");
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);

        BsonDocument rendered = filter.Render(arguments);

        Assert.Equal("Comment", rendered["category"].AsString);
        Assert.Equal("CommentDraft", rendered["ownerType"].AsString);
        Assert.Equal("author-1", rendered["ownerId"].AsString);
        Assert.False(rendered["isPublished"].AsBoolean);
        BsonArray activeConditions = rendered["$or"].AsBsonArray;
        Assert.Contains(
            activeConditions,
            static condition => condition.AsBsonDocument.Contains("cleanupRequestedAt")
                && condition.AsBsonDocument["cleanupRequestedAt"].IsBsonNull);
        Assert.Contains(
            activeConditions,
            static condition => condition.AsBsonDocument.Contains("pendingCommentId")
                && condition.AsBsonDocument["pendingCommentId"]
                    .AsBsonDocument["$ne"].IsBsonNull);
    }
}
