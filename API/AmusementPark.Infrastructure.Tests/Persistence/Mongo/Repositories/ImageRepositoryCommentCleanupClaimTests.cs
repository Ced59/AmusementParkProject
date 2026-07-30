using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ImageRepositoryCommentCleanupClaimTests
{
    [Fact]
    public async Task CancelClaimedCommentImageCleanupAsync_WhenRequestChanged_ShouldPreserveItAndReleaseClaim()
    {
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        Mock<IMongoDatabase> database =
            new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<ImageDocument>(
                "images",
                null))
            .Returns(collection.Object);
        Mock<UpdateResult> timestampChanged =
            new Mock<UpdateResult>(MockBehavior.Strict);
        timestampChanged.SetupGet(value => value.MatchedCount).Returns(0);
        Mock<UpdateResult> claimReleased =
            new Mock<UpdateResult>(MockBehavior.Strict);
        claimReleased.SetupGet(value => value.MatchedCount).Returns(1);
        claimReleased.SetupGet(value => value.ModifiedCount).Returns(1);
        List<UpdateDefinition<ImageDocument>> updates =
            new List<UpdateDefinition<ImageDocument>>();
        List<FilterDefinition<ImageDocument>> filters =
            new List<FilterDefinition<ImageDocument>>();
        int callCount = 0;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<ImageDocument> filter,
                UpdateDefinition<ImageDocument> update,
                UpdateOptions _,
                CancellationToken _) =>
            {
                filters.Add(filter);
                updates.Add(update);
            })
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? timestampChanged.Object
                    : claimReleased.Object;
            });
        using MemoryCache cache =
            new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = new ImageRepository(
            database.Object,
            new MongoDbSettings { ImagesCollectionName = "images" },
            cache);

        bool result = await repository.CancelClaimedCommentImageCleanupAsync(
            "image-1",
            ImageOwnerType.Comment,
            "comment-1",
            new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc),
            null,
            "claim-owner",
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(2, filters.Count);
        Assert.Equal(2, updates.Count);
        BsonDocument fallbackFilter = Render(filters[1]);
        Assert.Equal(
            "claim-owner",
            fallbackFilter["cleanupClaimToken"].AsString);
        BsonDocument fallbackUpdate = Render(updates[1]);
        BsonDocument unset = fallbackUpdate["$unset"].AsBsonDocument;
        Assert.True(unset.Contains("cleanupClaimToken"));
        Assert.True(unset.Contains("cleanupClaimedUntil"));
        Assert.False(unset.Contains("cleanupRequestedAt"));
        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task CancelClaimedCommentImageCleanupAsync_WhenRequestIsUnchanged_ShouldCancelInOneUpdate()
    {
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        Mock<IMongoDatabase> database =
            new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<ImageDocument>(
                "images",
                null))
            .Returns(collection.Object);
        Mock<UpdateResult> canceled =
            new Mock<UpdateResult>(MockBehavior.Strict);
        canceled.SetupGet(value => value.MatchedCount).Returns(1);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(canceled.Object);
        using MemoryCache cache =
            new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = new ImageRepository(
            database.Object,
            new MongoDbSettings { ImagesCollectionName = "images" },
            cache);

        bool result = await repository.CancelClaimedCommentImageCleanupAsync(
            "image-1",
            ImageOwnerType.Comment,
            "comment-1",
            new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc),
            null,
            "claim-owner",
            CancellationToken.None);

        Assert.True(result);
        collection.Verify(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None),
            Times.Once);
        collection.VerifyAll();
        database.VerifyAll();
    }

    private static BsonDocument Render(
        FilterDefinition<ImageDocument> filter)
    {
        MongoDB.Bson.Serialization.IBsonSerializer<ImageDocument> serializer =
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry
                .GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(
        UpdateDefinition<ImageDocument> update)
    {
        MongoDB.Bson.Serialization.IBsonSerializer<ImageDocument> serializer =
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry
                .GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
    }
}
