using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class MongoImageCurrentMutationLockTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSerializeOperationsWithinTheSameOwnerScope()
    {
        object synchronization = new object();
        string? currentToken = null;
        TaskCompletionSource secondAcquisitionBlocked = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IMongoCollection<ImageCurrentMutationLockDocument>> collection =
            new Mock<IMongoCollection<ImageCurrentMutationLockDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ImageCurrentMutationLockDocument, ImageCurrentMutationLockDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<ImageCurrentMutationLockDocument> _,
                UpdateDefinition<ImageCurrentMutationLockDocument> update,
                FindOneAndUpdateOptions<ImageCurrentMutationLockDocument, ImageCurrentMutationLockDocument> _,
                CancellationToken _) =>
            {
                lock (synchronization)
                {
                    if (currentToken is not null)
                    {
                        secondAcquisitionBlocked.TrySetResult();
                        return Task.FromResult<ImageCurrentMutationLockDocument>(null!);
                    }

                    currentToken = Render(update)["$set"].AsBsonDocument["token"].AsString;
                    return Task.FromResult(new ImageCurrentMutationLockDocument
                    {
                        ScopeKey = "User:7:owner-1:Avatar",
                        Token = currentToken,
                    });
                }
            });
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Returns((
                FilterDefinition<ImageCurrentMutationLockDocument> _,
                UpdateDefinition<ImageCurrentMutationLockDocument> _,
                UpdateOptions _,
                CancellationToken _) =>
            {
                lock (synchronization)
                {
                    currentToken = null;
                }

                return Task.FromResult<UpdateResult>(
                    new UpdateResult.Acknowledged(1, 1, null));
            });
        MongoImageCurrentMutationLock mutationLock = new MongoImageCurrentMutationLock(
            collection.Object,
            TimeProvider.System,
            NullLogger<MongoImageCurrentMutationLock>.Instance);
        TaskCompletionSource firstStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Task<string> first = mutationLock.ExecuteAsync(
            ImageOwnerType.User,
            "owner-1",
            ImageCategory.Avatar,
            async cancellationToken =>
            {
                firstStarted.SetResult();
                await releaseFirst.Task.WaitAsync(cancellationToken);
                return "first";
            },
            CancellationToken.None);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Task<string> second = mutationLock.ExecuteAsync(
            ImageOwnerType.User,
            "owner-1",
            ImageCategory.Avatar,
            _ =>
            {
                secondStarted.SetResult();
                return Task.FromResult("second");
            },
            CancellationToken.None);

        await secondAcquisitionBlocked.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(secondStarted.Task.IsCompleted);

        releaseFirst.SetResult();
        Assert.Equal("first", await first.WaitAsync(TimeSpan.FromSeconds(2)));
        await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal("second", await second.WaitAsync(TimeSpan.FromSeconds(2)));
        collection.VerifyAll();
    }

    [Fact]
    public void BuildAcquireFilter_ShouldAllowOnlyAnAvailableOrExpiredScope()
    {
        DateTime nowUtc = new DateTime(2026, 9, 6, 16, 0, 0, DateTimeKind.Utc);

        BsonDocument rendered = Render(
            MongoImageCurrentMutationLock.BuildAcquireFilter(
                "User:7:owner-1:Avatar",
                nowUtc));

        Assert.Equal("User:7:owner-1:Avatar", rendered["_id"].AsString);
        BsonArray alternatives = rendered["$or"].AsBsonArray;
        Assert.Contains(
            alternatives,
            candidate => candidate.AsBsonDocument.Contains("token"));
        Assert.Contains(
            alternatives,
            candidate => candidate.AsBsonDocument.Contains("expiresAtUtc"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenHeartbeatFailsOnce_ShouldRetryWithoutCancellingThePromotion()
    {
        TaskCompletionSource retryObserved = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int heartbeatCount = 0;
        Mock<IMongoCollection<ImageCurrentMutationLockDocument>> collection =
            new Mock<IMongoCollection<ImageCurrentMutationLockDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ImageCurrentMutationLockDocument, ImageCurrentMutationLockDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<ImageCurrentMutationLockDocument> _,
                UpdateDefinition<ImageCurrentMutationLockDocument> update,
                FindOneAndUpdateOptions<ImageCurrentMutationLockDocument, ImageCurrentMutationLockDocument> _,
                CancellationToken _) => Task.FromResult(new ImageCurrentMutationLockDocument
            {
                ScopeKey = "User:7:owner-1:Avatar",
                Token = Render(update)["$set"].AsBsonDocument["token"].AsString,
            }));
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<ImageCurrentMutationLockDocument> _,
                UpdateDefinition<ImageCurrentMutationLockDocument> update,
                UpdateOptions _,
                CancellationToken _) =>
            {
                BsonDocument rendered = Render(update);
                bool isRelease = rendered["$set"].AsBsonDocument.Contains("token");
                if (isRelease)
                {
                    return Task.FromResult<UpdateResult>(
                        new UpdateResult.Acknowledged(1, 1, null));
                }

                int call = Interlocked.Increment(ref heartbeatCount);
                if (call == 1)
                {
                    return Task.FromException<UpdateResult>(
                        new TimeoutException("Transient MongoDB timeout."));
                }

                retryObserved.TrySetResult();
                return Task.FromResult<UpdateResult>(
                    new UpdateResult.Acknowledged(1, 1, null));
            });
        MongoImageCurrentMutationLock mutationLock = new MongoImageCurrentMutationLock(
            collection.Object,
            TimeProvider.System,
            NullLogger<MongoImageCurrentMutationLock>.Instance,
            TimeSpan.FromMilliseconds(10));

        string result = await mutationLock.ExecuteAsync(
            ImageOwnerType.User,
            "owner-1",
            ImageCategory.Avatar,
            async cancellationToken =>
            {
                await retryObserved.Task.WaitAsync(cancellationToken);
                return "promoted";
            },
            CancellationToken.None);

        Assert.Equal("promoted", result);
        Assert.Equal(2, heartbeatCount);
        Assert.Equal(
            TimeSpan.FromMilliseconds(10),
            MongoImageCurrentMutationLock.GetHeartbeatRetryInterval(
                TimeSpan.FromMilliseconds(10)));
        Assert.Equal(
            TimeSpan.FromSeconds(5),
            MongoImageCurrentMutationLock.GetHeartbeatRetryInterval(
                MongoImageCurrentMutationLock.HeartbeatInterval));
        collection.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerCancelsAfterAcquisition_ShouldFinishCriticalSection()
    {
        Mock<IMongoCollection<ImageCurrentMutationLockDocument>> collection =
            new Mock<IMongoCollection<ImageCurrentMutationLockDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ImageCurrentMutationLockDocument, ImageCurrentMutationLockDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<ImageCurrentMutationLockDocument> _,
                UpdateDefinition<ImageCurrentMutationLockDocument> update,
                FindOneAndUpdateOptions<ImageCurrentMutationLockDocument, ImageCurrentMutationLockDocument> _,
                CancellationToken _) => Task.FromResult(new ImageCurrentMutationLockDocument
            {
                ScopeKey = "User:7:owner-1:Avatar",
                Token = Render(update)["$set"].AsBsonDocument["token"].AsString,
            }));
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        MongoImageCurrentMutationLock mutationLock = new MongoImageCurrentMutationLock(
            collection.Object,
            TimeProvider.System,
            NullLogger<MongoImageCurrentMutationLock>.Instance);
        using CancellationTokenSource callerCancellation = new CancellationTokenSource();

        string result = await mutationLock.ExecuteAsync(
            ImageOwnerType.User,
            "owner-1",
            ImageCategory.Avatar,
            operationCancellationToken =>
            {
                callerCancellation.Cancel();
                Assert.False(operationCancellationToken.IsCancellationRequested);
                return Task.FromResult("reconciled");
            },
            callerCancellation.Token);

        Assert.Equal("reconciled", result);
        collection.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenHeartbeatCannotRenewBeforeDeadline_ShouldCancelPromotion()
    {
        int heartbeatCount = 0;
        Mock<IMongoCollection<ImageCurrentMutationLockDocument>> collection =
            new Mock<IMongoCollection<ImageCurrentMutationLockDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ImageCurrentMutationLockDocument, ImageCurrentMutationLockDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<ImageCurrentMutationLockDocument> _,
                UpdateDefinition<ImageCurrentMutationLockDocument> update,
                FindOneAndUpdateOptions<ImageCurrentMutationLockDocument, ImageCurrentMutationLockDocument> _,
                CancellationToken _) => Task.FromResult(new ImageCurrentMutationLockDocument
            {
                ScopeKey = "User:7:owner-1:Avatar",
                Token = Render(update)["$set"].AsBsonDocument["token"].AsString,
            }));
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateDefinition<ImageCurrentMutationLockDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<ImageCurrentMutationLockDocument> _,
                UpdateDefinition<ImageCurrentMutationLockDocument> update,
                UpdateOptions _,
                CancellationToken _) =>
            {
                BsonDocument rendered = Render(update);
                if (rendered["$set"].AsBsonDocument.Contains("token"))
                {
                    return Task.FromResult<UpdateResult>(
                        new UpdateResult.Acknowledged(1, 1, null));
                }

                Interlocked.Increment(ref heartbeatCount);
                return Task.FromException<UpdateResult>(
                    new TimeoutException("MongoDB remains unavailable."));
            });
        MongoImageCurrentMutationLock mutationLock = new MongoImageCurrentMutationLock(
            collection.Object,
            TimeProvider.System,
            NullLogger<MongoImageCurrentMutationLock>.Instance,
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mutationLock.ExecuteAsync(
                    ImageOwnerType.User,
                    "owner-1",
                    ImageCategory.Avatar,
                    async operationCancellationToken =>
                    {
                        await Task.Delay(
                            Timeout.InfiniteTimeSpan,
                            operationCancellationToken);
                        return "stale-promotion";
                    },
                    CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(2)));

        Assert.True(heartbeatCount > 0);
        collection.VerifyAll();
    }

    private static BsonDocument Render(
        FilterDefinition<ImageCurrentMutationLockDocument> filter)
    {
        IBsonSerializer<ImageCurrentMutationLockDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageCurrentMutationLockDocument>();
        return filter.Render(
            new RenderArgs<ImageCurrentMutationLockDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(
        UpdateDefinition<ImageCurrentMutationLockDocument> update)
    {
        IBsonSerializer<ImageCurrentMutationLockDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ImageCurrentMutationLockDocument>();
        return update.Render(
            new RenderArgs<ImageCurrentMutationLockDocument>(
                serializer,
                BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
