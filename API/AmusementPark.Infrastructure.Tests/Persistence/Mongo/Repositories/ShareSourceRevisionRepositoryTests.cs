using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ShareSourceRevisionRepositoryTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 5, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task BeginMutationAsync_ShouldPersistAnExpiringLeaseWithoutAdvancingRevision()
    {
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        UpdateDefinition<ShareSourceRevisionDocument>? capturedUpdate = null;
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<ShareSourceRevisionDocument> _,
                UpdateDefinition<ShareSourceRevisionDocument> update,
                FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument> _,
                CancellationToken _) => capturedUpdate = update)
            .ReturnsAsync(new ShareSourceRevisionDocument
            {
                ScopeKey = "personal-ranking:owner-1",
                Revision = 0,
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            });
        using ShareSourceRevisionRepository repository = CreateRepository(collection.Object);

        ShareSourceMutationLease mutationLease = await repository.BeginMutationAsync(
            " personal-ranking:owner-1 ",
            CancellationToken.None);

        Assert.Equal("personal-ranking:owner-1", mutationLease.ScopeKey);
        Assert.NotNull(capturedUpdate);
        BsonDocument updateDocument = Render(capturedUpdate);
        Assert.False(updateDocument.Contains("$inc"));
        BsonDocument pushedLease = updateDocument["$push"].AsBsonDocument["mutationLeases"].AsBsonDocument;
        Assert.Equal(mutationLease.Token, pushedLease["token"].AsString);
        Assert.Equal(
            NowUtc.Add(ShareSourceRevisionRepository.MutationLeaseDuration),
            pushedLease["expiresAtUtc"].ToUniversalTime());
        collection.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenSourceChanged_ShouldAtomicallyRemoveLeaseAndAdvanceRevision()
    {
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        FilterDefinition<ShareSourceRevisionDocument>? capturedFilter = null;
        UpdateDefinition<ShareSourceRevisionDocument>? capturedUpdate = null;
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<ShareSourceRevisionDocument> filter,
                UpdateDefinition<ShareSourceRevisionDocument> update,
                FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument> _,
                CancellationToken _) =>
            {
                capturedFilter = filter;
                capturedUpdate = update;
            })
            .ReturnsAsync(new ShareSourceRevisionDocument
            {
                ScopeKey = "personal-ranking:owner-1",
                Revision = 4,
                CreatedAt = NowUtc.AddDays(-1),
                UpdatedAt = NowUtc,
            });
        ShareSourceRevisionRepository repository = CreateRepository(collection.Object);
        ShareSourceMutationLease mutationLease = new ShareSourceMutationLease(
            "personal-ranking:owner-1",
            3.ToString("x32"));

        ShareSourceRevision result = await repository.CompleteMutationAsync(
            mutationLease,
            sourceChanged: true,
            CancellationToken.None);

        Assert.Equal(4, result.Revision);
        Assert.True(result.IsStable);
        BsonDocument filter = Render(capturedFilter!);
        Assert.Equal("personal-ranking:owner-1", filter["_id"].AsString);
        BsonDocument update = Render(capturedUpdate!);
        Assert.Equal(1, update["$inc"].AsBsonDocument["revision"].AsInt64);
        Assert.Equal(
            mutationLease.Token,
            update["$pull"].AsBsonDocument["mutationLeases"].AsBsonDocument["token"].AsString);
        collection.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenRecoveredLeaseIsMissing_ShouldAdvanceRevisionAgain()
    {
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        collection.SetupSequence(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .ReturnsAsync((ShareSourceRevisionDocument)null!)
            .ReturnsAsync((ShareSourceRevisionDocument)null!)
            .ReturnsAsync(new ShareSourceRevisionDocument
            {
                ScopeKey = "personal-ranking:owner-1",
                Revision = 8,
                CreatedAt = NowUtc.AddDays(-1),
                UpdatedAt = NowUtc,
            });
        ShareSourceRevisionRepository repository = CreateRepository(collection.Object);
        ShareSourceMutationLease mutationLease = new ShareSourceMutationLease(
            "personal-ranking:owner-1",
            4.ToString("x32"));

        ShareSourceRevision result = await repository.CompleteMutationAsync(
            mutationLease,
            sourceChanged: true,
            CancellationToken.None);

        Assert.Equal(8, result.Revision);
        collection.VerifyAll();
    }

    [Fact]
    public void BuildHeartbeatUpdate_ShouldExtendTheMatchingLeaseWithSafetyMargin()
    {
        UpdateDefinition<ShareSourceRevisionDocument> heartbeat =
            ShareSourceRevisionRepository.BuildHeartbeatUpdate(NowUtc);

        BsonDocument update = Render(heartbeat);

        BsonDocument set = update["$set"].AsBsonDocument;
        Assert.Equal(
            NowUtc.Add(ShareSourceRevisionRepository.MutationLeaseDuration),
            set["mutationLeases.$.expiresAtUtc"].ToUniversalTime());
        Assert.Equal(NowUtc, set["updatedAt"].ToUniversalTime());
        Assert.True(
            ShareSourceRevisionRepository.MutationHeartbeatInterval
            < ShareSourceRevisionRepository.MutationLeaseDuration);
        Assert.True(
            ShareSourceRevisionRepository.WriterLeaseCancellationDelay
            < ShareSourceRevisionRepository.MutationLeaseDuration);
    }

    [Fact]
    public async Task BeginMutationAsync_ShouldRenewLeaseWhileWriterRemainsActive()
    {
        TaskCompletionSource heartbeatObserved = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int updateCallCount = 0;
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                updateCallCount++;
                if (updateCallCount == 2)
                {
                    heartbeatObserved.TrySetResult();
                }
            })
            .ReturnsAsync(() => new UpdateResult.Acknowledged(1, 1, null));
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevisionDocument
            {
                ScopeKey = "personal-ranking:owner-1",
                Revision = 0,
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            });
        using ShareSourceRevisionRepository repository = new ShareSourceRevisionRepository(
            collection.Object,
            TimeProvider.System,
            NullLogger<ShareSourceRevisionRepository>.Instance,
            TimeSpan.FromMilliseconds(10));

        await repository.BeginMutationAsync(
            "personal-ranking:owner-1",
            CancellationToken.None);
        await heartbeatObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(updateCallCount >= 2);
    }

    [Fact]
    public async Task BeginMutationAsync_WhenHeartbeatFailsOnce_ShouldRetryWhileWriterRemainsActive()
    {
        TaskCompletionSource retryObserved = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int updateCallCount = 0;
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                FilterDefinition<ShareSourceRevisionDocument> _,
                UpdateDefinition<ShareSourceRevisionDocument> _,
                UpdateOptions _,
                CancellationToken _) =>
            {
                int call = Interlocked.Increment(ref updateCallCount);
                if (call == 2)
                {
                    return Task.FromException<UpdateResult>(
                        new TimeoutException("Transient MongoDB timeout."));
                }

                if (call == 3)
                {
                    retryObserved.TrySetResult();
                }

                return Task.FromResult<UpdateResult>(
                    new UpdateResult.Acknowledged(1, 1, null));
            });
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevisionDocument
            {
                ScopeKey = "personal-ranking:owner-1",
                Revision = 0,
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            });
        using ShareSourceRevisionRepository repository = new ShareSourceRevisionRepository(
            collection.Object,
            TimeProvider.System,
            NullLogger<ShareSourceRevisionRepository>.Instance,
            TimeSpan.FromMilliseconds(10));

        await repository.BeginMutationAsync(
            "personal-ranking:owner-1",
            CancellationToken.None);
        await retryObserved.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(updateCallCount >= 3);
        Assert.Equal(
            TimeSpan.FromMilliseconds(10),
            ShareSourceRevisionRepository.GetHeartbeatRetryInterval(
                TimeSpan.FromMilliseconds(10)));
        Assert.Equal(
            TimeSpan.FromSeconds(5),
            ShareSourceRevisionRepository.GetHeartbeatRetryInterval(
                ShareSourceRevisionRepository.MutationHeartbeatInterval));
    }

    [Fact]
    public async Task BeginMutationAsync_WhenHeartbeatNoLongerOwnsLease_ShouldCancelWriterLease()
    {
        TaskCompletionSource heartbeatObserved = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback(() => heartbeatObserved.TrySetResult())
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevisionDocument
            {
                ScopeKey = "personal-ranking:owner-1",
                Revision = 0,
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            });
        using ShareSourceRevisionRepository repository = new ShareSourceRevisionRepository(
            collection.Object,
            TimeProvider.System,
            NullLogger<ShareSourceRevisionRepository>.Instance,
            TimeSpan.FromMilliseconds(10));

        ShareSourceMutationLease mutationLease = await repository.BeginMutationAsync(
            "personal-ranking:owner-1",
            CancellationToken.None);
        Task leaseLossCancellation = Task.Delay(
            Timeout.InfiniteTimeSpan,
            mutationLease.LeaseCancellationToken);

        await heartbeatObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => leaseLossCancellation.WaitAsync(TimeSpan.FromSeconds(1)));
        collection.VerifyAll();
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldRecoverExpiredLeasesConservativelyBeforeReading()
    {
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        UpdateDefinition<ShareSourceRevisionDocument>? recoveryUpdate = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<ShareSourceRevisionDocument> _,
                UpdateDefinition<ShareSourceRevisionDocument> update,
                UpdateOptions _,
                CancellationToken _) => recoveryUpdate = update)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevisionDocument
            {
                ScopeKey = "personal-ranking:owner-1",
                Revision = 6,
                CreatedAt = NowUtc.AddDays(-1),
                UpdatedAt = NowUtc,
            });
        ShareSourceRevisionRepository repository = CreateRepository(collection.Object);

        ShareSourceRevision result = await repository.GetOrCreateAsync(
            "personal-ranking:owner-1",
            CancellationToken.None);

        Assert.Equal(6, result.Revision);
        BsonDocument update = Render(recoveryUpdate!);
        Assert.Equal(1, update["$inc"].AsBsonDocument["revision"].AsInt64);
        Assert.True(update.Contains("$pull"));
        collection.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenUnchangedLeaseExpired_ShouldPersistProjectedGeneration()
    {
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        List<FilterDefinition<ShareSourceRevisionDocument>> filters =
            new List<FilterDefinition<ShareSourceRevisionDocument>>();
        List<UpdateDefinition<ShareSourceRevisionDocument>> updates =
            new List<UpdateDefinition<ShareSourceRevisionDocument>>();
        int callCount = 0;
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<ShareSourceRevisionDocument> filter,
                UpdateDefinition<ShareSourceRevisionDocument> update,
                FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument> _,
                CancellationToken _) =>
            {
                filters.Add(filter);
                updates.Add(update);
            })
            .Returns((
                FilterDefinition<ShareSourceRevisionDocument> _,
                UpdateDefinition<ShareSourceRevisionDocument> _,
                FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument> _,
                CancellationToken _) =>
            {
                callCount++;
                ShareSourceRevisionDocument? document = callCount == 1
                    ? null
                    : new ShareSourceRevisionDocument
                    {
                        ScopeKey = "personal-ranking:owner-1",
                        Revision = 5,
                        CreatedAt = NowUtc.AddDays(-1),
                        UpdatedAt = NowUtc,
                    };
                return Task.FromResult(document!);
            });
        ShareSourceRevisionRepository repository = CreateRepository(collection.Object);
        ShareSourceMutationLease mutationLease = new ShareSourceMutationLease(
            "personal-ranking:owner-1",
            5.ToString("x32"));

        ShareSourceRevision result = await repository.CompleteMutationAsync(
            mutationLease,
            sourceChanged: false,
            CancellationToken.None);

        Assert.Equal(5, result.Revision);
        Assert.True(result.IsStable);
        Assert.Equal(2, filters.Count);
        string activeFilter = Render(filters[0]).ToJson();
        string expiredFilter = Render(filters[1]).ToJson();
        Assert.Contains("$gt", activeFilter);
        Assert.Contains("$$NOW", activeFilter);
        Assert.Contains("$lte", expiredFilter);
        Assert.Contains("$$NOW", expiredFilter);
        BsonDocument recoveryUpdate = Render(updates[1]);
        Assert.Equal(1, recoveryUpdate["$inc"].AsBsonDocument["revision"].AsInt64);
        Assert.Equal(
            mutationLease.Token,
            recoveryUpdate["$pull"].AsBsonDocument["mutationLeases"].AsBsonDocument["token"].AsString);
        collection.Verify(value => value.FindOneAndUpdateAsync(
            It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
            It.IsAny<UpdateDefinition<ShareSourceRevisionDocument>>(),
            It.IsAny<FindOneAndUpdateOptions<ShareSourceRevisionDocument, ShareSourceRevisionDocument>>(),
            CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task GetSnapshotAsync_ShouldBatchReadWithoutWritesAndProjectExpiredLeases()
    {
        ShareSourceRevisionDocument ownerDocument = new ShareSourceRevisionDocument
        {
            ScopeKey = "personal-ranking:owner-1",
            Revision = 4,
            MutationLeases = new List<ShareSourceMutationLeaseDocument>
            {
                new ShareSourceMutationLeaseDocument
                {
                    Token = "expired",
                    ExpiresAtUtc = NowUtc.AddSeconds(-1),
                },
            },
            CreatedAt = NowUtc.AddDays(-1),
            UpdatedAt = NowUtc.AddMinutes(-5),
        };
        ShareSourceRevisionDocument catalogDocument = new ShareSourceRevisionDocument
        {
            ScopeKey = "personal-ranking:public-catalog",
            Revision = 2,
            MutationLeases = new List<ShareSourceMutationLeaseDocument>
            {
                new ShareSourceMutationLeaseDocument
                {
                    Token = "active",
                    ExpiresAtUtc = NowUtc.AddMinutes(1),
                },
            },
            CreatedAt = NowUtc.AddDays(-1),
            UpdatedAt = NowUtc,
        };
        Mock<IAsyncCursor<ShareSourceRevisionDocument>> cursor =
            new Mock<IAsyncCursor<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        cursor.SetupGet(value => value.Current).Returns(new[] { ownerDocument, catalogDocument });
        cursor.SetupSequence(value => value.MoveNextAsync(CancellationToken.None))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        cursor.Setup(value => value.Dispose());
        Mock<IMongoCollection<ShareSourceRevisionDocument>> collection =
            new Mock<IMongoCollection<ShareSourceRevisionDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<ShareSourceRevisionDocument>>(),
                It.IsAny<FindOptions<
                    ShareSourceRevisionDocument,
                    ShareSourceRevisionDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
        using ShareSourceRevisionRepository repository = CreateRepository(collection.Object);

        IReadOnlyDictionary<string, ShareSourceRevision> snapshot =
            await repository.GetSnapshotAsync(
                new[]
                {
                    "personal-ranking:owner-1",
                    "personal-ranking:public-catalog",
                    "personal-ranking:missing",
                },
                CancellationToken.None);

        Assert.Equal(5, snapshot["personal-ranking:owner-1"].Revision);
        Assert.True(snapshot["personal-ranking:owner-1"].IsStable);
        Assert.Equal(2, snapshot["personal-ranking:public-catalog"].Revision);
        Assert.False(snapshot["personal-ranking:public-catalog"].IsStable);
        Assert.Equal(0, snapshot["personal-ranking:missing"].Revision);
        Assert.True(snapshot["personal-ranking:missing"].IsStable);
        collection.VerifyAll();
        cursor.VerifyAll();
    }

    [Fact]
    public void SourceRevisionDocument_ShouldContainNoPublicOrPrivateProfilePayload()
    {
        ShareSourceRevisionDocument document = new ShareSourceRevisionDocument
        {
            ScopeKey = "personal-ranking:owner-1",
            Revision = 2,
            CreatedAt = NowUtc,
            UpdatedAt = NowUtc,
        };

        BsonDocument bson = document.ToBsonDocument();

        Assert.False(bson.Contains("email"));
        Assert.False(bson.Contains("displayName"));
        Assert.False(bson.Contains("ratings"));
        Assert.False(bson.Contains("privateComment"));
    }

    private static ShareSourceRevisionRepository CreateRepository(
        IMongoCollection<ShareSourceRevisionDocument> collection)
    {
        return new ShareSourceRevisionRepository(
            collection,
            new ShareSourceRevisionFixedTimeProvider(NowUtc));
    }

    private static BsonDocument Render(
        FilterDefinition<ShareSourceRevisionDocument> filter)
    {
        IBsonSerializer<ShareSourceRevisionDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ShareSourceRevisionDocument>();
        return filter.Render(
            new RenderArgs<ShareSourceRevisionDocument>(serializer, BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(
        UpdateDefinition<ShareSourceRevisionDocument> update)
    {
        IBsonSerializer<ShareSourceRevisionDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ShareSourceRevisionDocument>();
        return update.Render(
            new RenderArgs<ShareSourceRevisionDocument>(serializer, BsonSerializer.SerializerRegistry))
            .AsBsonDocument;
    }
}
