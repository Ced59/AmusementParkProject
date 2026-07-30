using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using AmusementPark.Core.Domain.Comments;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class CommentRepositoryRevisionFenceTests
{
    [Fact]
    public void BuildRevisionFenceFilter_ShouldRequireExactCommentAndRevision()
    {
        FilterDefinition<CommentDocument> filter =
            CommentRepository.BuildRevisionFenceFilter(
                " comment-1 ",
                4);

        BsonDocument rendered = Render(filter);

        Assert.Equal("comment-1", rendered["_id"].AsString);
        Assert.Equal(4, rendered["revision"].AsInt64);
    }

    [Fact]
    public void BuildRevisionFenceUpdate_ShouldAdvanceOnlyRevision()
    {
        UpdateDefinition<CommentDocument> update =
            CommentRepository.BuildRevisionFenceUpdate(4);

        BsonDocument rendered = Render(update);

        Assert.Single(rendered);
        BsonDocument set = rendered["$set"].AsBsonDocument;
        Assert.Single(set);
        Assert.Equal(5, set["revision"].AsInt64);
        Assert.False(set.Contains("updatedAt"));
    }

    [Theory]
    [InlineData(CommentTargetType.Park)]
    [InlineData(CommentTargetType.ParkItem)]
    public void BuildPublishedTargetAndLanguageFilter_ShouldRequireExactNonEmptyBody(
        CommentTargetType targetType)
    {
        FilterDefinition<CommentDocument> filter =
            CommentRepository.BuildPublishedTargetAndLanguageFilter(
                targetType,
                " target-1 ",
                " FR ");

        BsonDocument rendered = Render(filter);

        Assert.Equal(targetType.ToString(), rendered["targetType"].AsString);
        Assert.Equal("target-1", rendered["targetId"].AsString);
        Assert.Equal("Published", rendered["moderationStatus"].AsString);
        BsonDocument localizedFilter =
            rendered["bodies"].AsBsonDocument["$elemMatch"].AsBsonDocument;
        Assert.Equal("fr", localizedFilter["languageCode"].AsString);
        Assert.Equal(
            "\\S",
            localizedFilter["value"].AsBsonRegularExpression.Pattern);
    }

    private static BsonDocument Render(
        FilterDefinition<CommentDocument> filter)
    {
        IBsonSerializer<CommentDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<CommentDocument>();
        RenderArgs<CommentDocument> arguments =
            new RenderArgs<CommentDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(
        UpdateDefinition<CommentDocument> update)
    {
        IBsonSerializer<CommentDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<CommentDocument>();
        RenderArgs<CommentDocument> arguments =
            new RenderArgs<CommentDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
    }
}
