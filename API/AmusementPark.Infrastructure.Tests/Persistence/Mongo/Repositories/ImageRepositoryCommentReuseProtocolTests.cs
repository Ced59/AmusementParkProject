using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ImageRepositoryCommentReuseProtocolTests
{
    private static readonly DateTime ReconcileAfterUtc =
        new DateTime(2026, 7, 30, 12, 5, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TryPreparePublishedCommentImageForReuseAsync_WhenAcknowledgementIsLost_ShouldFenceNewerCleanupAndWriteMarkerAtomically()
    {
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        FilterDefinition<ImageDocument>? capturedFilter = null;
        UpdateDefinition<ImageDocument>? capturedUpdate = null;
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ImageDocument, ImageDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<ImageDocument> filter,
                UpdateDefinition<ImageDocument> update,
                FindOneAndUpdateOptions<ImageDocument, ImageDocument> _,
                CancellationToken _) =>
            {
                capturedFilter = filter;
                capturedUpdate = update;
            })
            .ThrowsAsync(new TimeoutException("Acknowledgement lost."));
        Mock<IMongoDatabase> database = CreateDatabase(collection);
        using MemoryCache cache =
            new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = CreateRepository(database, cache);

        await Assert.ThrowsAsync<TimeoutException>(
            () => repository.TryPreparePublishedCommentImageForReuseAsync(
                "image-1",
                "comment-1",
                "reservation-token",
                ReconcileAfterUtc,
                5,
                CancellationToken.None));

        BsonDocument filter = Render(Assert.IsAssignableFrom<
            FilterDefinition<ImageDocument>>(capturedFilter));
        Assert.True(ContainsFieldValue(
            filter,
            "cleanupClaimToken",
            BsonNull.Value));
        Assert.True(ContainsFieldComparison(
            filter,
            "cleanupRequestedAt",
            "$ne",
            BsonNull.Value));
        Assert.True(ContainsFieldValue(
            filter,
            "cleanupCommentRevision",
            BsonNull.Value));
        Assert.True(ContainsFieldComparison(
            filter,
            "cleanupCommentRevision",
            "$lte",
            new BsonInt64(5)));
        BsonDocument update = Render(Assert.IsAssignableFrom<
            UpdateDefinition<ImageDocument>>(capturedUpdate));
        Assert.Equal(
            "reservation-token",
            update["$set"]["commentReuseReservationToken"].AsString);
        Assert.Equal(
            5,
            update["$set"]["commentReuseTargetRevision"].AsInt64);
        Assert.Equal(
            new BsonDateTime(ReconcileAfterUtc),
            update["$set"]["commentReuseReconcileAfter"]);
        Assert.True(
            update["$unset"].AsBsonDocument.Contains(
                "cleanupRequestedAt"));
        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task ReleasePublishedCommentImageReuseAsync_WhenAcknowledgementIsLost_ShouldKeepEitherMarkerOrCleanupAtomically()
    {
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        FilterDefinition<ImageDocument>? capturedFilter = null;
        UpdateDefinition<ImageDocument>? capturedUpdate = null;
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
                capturedFilter = filter;
                capturedUpdate = update;
            })
            .ThrowsAsync(new TimeoutException("Acknowledgement lost."));
        Mock<IMongoDatabase> database = CreateDatabase(collection);
        using MemoryCache cache =
            new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = CreateRepository(database, cache);

        await Assert.ThrowsAsync<TimeoutException>(
            () => repository.ReleasePublishedCommentImageReuseAsync(
                "image-1",
                "comment-1",
                "reservation-token",
                ReconcileAfterUtc,
                5,
                CancellationToken.None));

        BsonDocument filter = Render(Assert.IsAssignableFrom<
            FilterDefinition<ImageDocument>>(capturedFilter));
        Assert.True(ContainsFieldValue(
            filter,
            "commentReuseReservationToken",
            new BsonString("reservation-token")));
        Assert.True(ContainsFieldValue(
            filter,
            "cleanupClaimToken",
            BsonNull.Value));
        BsonDocument update = Render(Assert.IsAssignableFrom<
            UpdateDefinition<ImageDocument>>(capturedUpdate));
        Assert.Equal(
            new BsonDateTime(ReconcileAfterUtc),
            update["$max"]["cleanupRequestedAt"]);
        Assert.Equal(
            5,
            update["$max"]["cleanupCommentRevision"].AsInt64);
        Assert.Equal(
            new BsonDateTime(ReconcileAfterUtc),
            update["$max"]["reservationReconcileAfter"]);
        Assert.False(
            update["$set"].AsBsonDocument.Contains(
                "cleanupRequestedAt"));
        BsonDocument unset = update["$unset"].AsBsonDocument;
        Assert.True(unset.Contains("commentReuseReservationToken"));
        Assert.True(unset.Contains("commentReuseReconcileAfter"));
        Assert.True(unset.Contains("commentReuseTargetRevision"));
        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task ResolveClaimedPublishedCommentImageReuseAsync_ShouldPreserveNewerCleanupMarker()
    {
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        FilterDefinition<ImageDocument>? capturedFilter = null;
        UpdateDefinition<ImageDocument>? capturedUpdate = null;
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
                capturedFilter = filter;
                capturedUpdate = update;
            })
            .ReturnsAsync(CreateSuccessfulUpdateResult());
        Mock<IMongoDatabase> database = CreateDatabase(collection);
        using MemoryCache cache =
            new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = CreateRepository(database, cache);

        bool resolved =
            await repository.ResolveClaimedPublishedCommentImageReuseAsync(
                "image-1",
                "comment-1",
                "reservation-token",
                "claim-token",
                CancellationToken.None);

        Assert.True(resolved);
        BsonDocument filter = Render(Assert.IsAssignableFrom<
            FilterDefinition<ImageDocument>>(capturedFilter));
        Assert.True(ContainsFieldValue(
            filter,
            "commentReuseReservationToken",
            new BsonString("reservation-token")));
        Assert.True(ContainsFieldValue(
            filter,
            "cleanupClaimToken",
            new BsonString("claim-token")));
        BsonDocument update = Render(Assert.IsAssignableFrom<
            UpdateDefinition<ImageDocument>>(capturedUpdate));
        BsonDocument unset = update["$unset"].AsBsonDocument;
        Assert.False(unset.Contains("cleanupRequestedAt"));
        Assert.True(unset.Contains("commentReuseReservationToken"));
        Assert.True(unset.Contains("cleanupClaimToken"));
        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task DeferClaimedPublishedCommentImageReuseAsync_ShouldPreserveCleanupAndRevisionFence()
    {
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        UpdateDefinition<ImageDocument>? capturedUpdate = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<ImageDocument> _,
                UpdateDefinition<ImageDocument> update,
                UpdateOptions _,
                CancellationToken _) =>
                capturedUpdate = update)
            .ReturnsAsync(CreateSuccessfulUpdateResult());
        Mock<IMongoDatabase> database = CreateDatabase(collection);
        using MemoryCache cache =
            new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = CreateRepository(database, cache);

        bool deferred =
            await repository.DeferClaimedPublishedCommentImageReuseAsync(
                "image-1",
                "comment-1",
                "reservation-token",
                "claim-token",
                ReconcileAfterUtc,
                CancellationToken.None);

        Assert.True(deferred);
        BsonDocument update = Render(Assert.IsAssignableFrom<
            UpdateDefinition<ImageDocument>>(capturedUpdate));
        BsonDocument unset = update["$unset"].AsBsonDocument;
        Assert.False(unset.Contains("cleanupRequestedAt"));
        Assert.False(unset.Contains("commentReuseReservationToken"));
        Assert.False(unset.Contains("commentReuseTargetRevision"));
        Assert.True(unset.Contains("cleanupClaimToken"));
        Assert.Equal(
            new BsonDateTime(ReconcileAfterUtc),
            update["$set"]["commentReuseReconcileAfter"]);
        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task FinalizePublishedCommentImageReuseAsync_ShouldPreserveNewerCleanupMarker()
    {
        Mock<IMongoCollection<ImageDocument>> collection =
            new Mock<IMongoCollection<ImageDocument>>(MockBehavior.Strict);
        UpdateDefinition<ImageDocument>? capturedUpdate = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageDocument>>(),
                It.IsAny<UpdateDefinition<ImageDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<ImageDocument> _,
                UpdateDefinition<ImageDocument> update,
                UpdateOptions _,
                CancellationToken _) =>
                capturedUpdate = update)
            .ReturnsAsync(CreateSuccessfulUpdateResult());
        Mock<IMongoDatabase> database = CreateDatabase(collection);
        using MemoryCache cache =
            new MemoryCache(new MemoryCacheOptions());
        ImageRepository repository = CreateRepository(database, cache);

        bool finalized =
            await repository.FinalizePublishedCommentImageReuseAsync(
                "image-1",
                "comment-1",
                "reservation-token",
                CancellationToken.None);

        Assert.True(finalized);
        BsonDocument update = Render(Assert.IsAssignableFrom<
            UpdateDefinition<ImageDocument>>(capturedUpdate));
        BsonDocument unset = update["$unset"].AsBsonDocument;
        Assert.False(unset.Contains("cleanupRequestedAt"));
        Assert.True(unset.Contains("commentReuseReservationToken"));
        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public void BuildCommentImageCleanupClaimFilter_ForDurableReuse_ShouldFenceOnObservedTokenAndDeadline()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildCommentImageCleanupClaimFilter(
                "image-1",
                ImageOwnerType.Comment,
                "comment-1",
                ReconcileAfterUtc,
                ReconcileAfterUtc.AddDays(-1),
                "reservation-token",
                ReconcileAfterUtc);

        BsonDocument rendered = Render(filter);

        Assert.True(ContainsFieldValue(
            rendered,
            "commentReuseReservationToken",
            new BsonString("reservation-token")));
        Assert.True(ContainsFieldComparison(
            rendered,
            "commentReuseReconcileAfter",
            "$lte",
            new BsonDateTime(ReconcileAfterUtc)));
    }

    [Fact]
    public void BuildCommentImageReconciliationFilter_ShouldSelectDueDurableReuseMarker()
    {
        FilterDefinition<ImageDocument> filter =
            ImageRepository.BuildCommentImageReconciliationFilter(
                ReconcileAfterUtc,
                ReconcileAfterUtc.AddDays(-1),
                ReconcileAfterUtc);

        BsonDocument rendered = Render(filter);

        Assert.True(ContainsFieldComparison(
            rendered,
            "commentReuseReconcileAfter",
            "$lte",
            new BsonDateTime(ReconcileAfterUtc)));
    }

    [Fact]
    public void BuildCommentImageReconciliationSort_ShouldMoveDeferredMarkersBehindOlderWork()
    {
        SortDefinition<ImageDocument> sort =
            ImageRepository.BuildCommentImageReconciliationSort();

        BsonDocument rendered = Render(sort);

        Assert.Equal(1, rendered["updatedAt"].AsInt32);
        Assert.Equal(1, rendered["createdAt"].AsInt32);
        Assert.False(rendered.Contains("cleanupRequestedAt"));
    }

    private static Mock<IMongoDatabase> CreateDatabase(
        Mock<IMongoCollection<ImageDocument>> collection)
    {
        Mock<IMongoDatabase> database =
            new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<ImageDocument>(
                "images",
                null))
            .Returns(collection.Object);
        return database;
    }

    private static ImageRepository CreateRepository(
        Mock<IMongoDatabase> database,
        IMemoryCache cache)
    {
        return new ImageRepository(
            database.Object,
            new MongoDbSettings { ImagesCollectionName = "images" },
            cache);
    }

    private static UpdateResult CreateSuccessfulUpdateResult()
    {
        Mock<UpdateResult> result =
            new Mock<UpdateResult>(MockBehavior.Strict);
        result.SetupGet(value => value.ModifiedCount).Returns(1);
        result.SetupGet(value => value.MatchedCount).Returns(1);
        return result.Object;
    }

    private static BsonDocument Render(
        FilterDefinition<ImageDocument> filter)
    {
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(
        UpdateDefinition<ImageDocument> update)
    {
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
    }

    private static BsonDocument Render(
        SortDefinition<ImageDocument> sort)
    {
        IBsonSerializer<ImageDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageDocument>();
        RenderArgs<ImageDocument> arguments =
            new RenderArgs<ImageDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return sort.Render(arguments);
    }

    private static bool ContainsFieldValue(
        BsonValue value,
        string fieldName,
        BsonValue expected)
    {
        if (value is BsonDocument document)
        {
            if (document.TryGetValue(fieldName, out BsonValue? actual)
                && actual == expected)
            {
                return true;
            }

            return document.Elements.Any(
                element => ContainsFieldValue(
                    element.Value,
                    fieldName,
                    expected));
        }

        return value is BsonArray array
            && array.Any(item => ContainsFieldValue(
                item,
                fieldName,
                expected));
    }

    private static bool ContainsFieldComparison(
        BsonValue value,
        string fieldName,
        string comparisonOperator,
        BsonValue expected)
    {
        if (value is BsonDocument document)
        {
            if (document.TryGetValue(fieldName, out BsonValue? fieldValue)
                && fieldValue is BsonDocument comparison
                && comparison.TryGetValue(
                    comparisonOperator,
                    out BsonValue? actual)
                && actual == expected)
            {
                return true;
            }

            return document.Elements.Any(
                element => ContainsFieldComparison(
                    element.Value,
                    fieldName,
                    comparisonOperator,
                    expected));
        }

        return value is BsonArray array
            && array.Any(item => ContainsFieldComparison(
                item,
                fieldName,
                comparisonOperator,
                expected));
    }
}
