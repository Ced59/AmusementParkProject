using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LivePollingStateRepositoryTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TryAcquireAsync_ShouldReplaceAnIdlePilotTargetAndResetItsState()
    {
        List<FilterDefinition<LivePollingStateDocument>> filters =
            new List<FilterDefinition<LivePollingStateDocument>>();
        List<UpdateDefinition<LivePollingStateDocument>> updates =
            new List<UpdateDefinition<LivePollingStateDocument>>();
        int invocation = 0;
        LivePollingStateDocument replacement = new LivePollingStateDocument
        {
            Id = "state-1",
            SourceId = "queue-times",
            ExternalEntityId = "new-park",
            LeaseOwner = "worker-1",
            LeaseToken = "token-1",
            LeaseExpiresAtUtc = NowUtc.AddMinutes(2),
            NextAttemptAtUtc = NowUtc.AddMinutes(5),
            CreatedAt = NowUtc.AddDays(-1),
            UpdatedAt = NowUtc,
        };
        Mock<IMongoCollection<LivePollingStateDocument>> collection =
            new Mock<IMongoCollection<LivePollingStateDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<LivePollingStateDocument>>(),
                It.IsAny<UpdateDefinition<LivePollingStateDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<LivePollingStateDocument, LivePollingStateDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<LivePollingStateDocument> filter,
                UpdateDefinition<LivePollingStateDocument> update,
                FindOneAndUpdateOptions<LivePollingStateDocument, LivePollingStateDocument> _,
                CancellationToken _) =>
            {
                filters.Add(filter);
                updates.Add(update);
            })
            .ReturnsAsync(() => ++invocation == 1 ? null! : replacement);
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<LivePollingStateDocument>(
                "live-polling-states",
                null))
            .Returns(collection.Object);
        LivePollingStateRepository repository = new LivePollingStateRepository(
            database.Object,
            new MongoDbSettings { LivePollingStatesCollectionName = "live-polling-states" });

        LivePollingLease? lease = await repository.TryAcquireAsync(
            new LivePollingLeaseRequest(
                LiveDataSourceId.Parse("queue-times"),
                "new-park",
                "worker-1",
                NowUtc,
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(5)),
            CancellationToken.None);

        Assert.NotNull(lease);
        Assert.Equal("new-park", lease.ExternalEntityId);
        Assert.Equal(2, filters.Count);
        BsonDocument replacementFilter = Render(filters[1]);
        Assert.Contains("queue-times", replacementFilter.ToJson(), StringComparison.Ordinal);
        Assert.Contains("new-park", replacementFilter.ToJson(), StringComparison.Ordinal);
        Assert.Contains("$ne", replacementFilter.ToJson(), StringComparison.Ordinal);

        BsonDocument replacementUpdate = Render(updates[1]);
        BsonDocument set = replacementUpdate["$set"].AsBsonDocument;
        BsonDocument unset = replacementUpdate["$unset"].AsBsonDocument;
        Assert.Equal("new-park", set["externalEntityId"].AsString);
        Assert.Equal(0, set["consecutiveFailures"].AsInt32);
        Assert.Equal(NowUtc.AddMinutes(5), set["nextAttemptAtUtc"].ToUniversalTime());
        Assert.True(unset.Contains("entityTag"));
        Assert.True(unset.Contains("lastPolledAtUtc"));
        Assert.True(unset.Contains("lastSuccessfulPollAtUtc"));
        Assert.True(unset.Contains("circuitOpenUntilUtc"));
        Assert.True(unset.Contains("lastDisposition"));
        collection.VerifyAll();
        database.VerifyAll();
    }

    private static BsonDocument Render(FilterDefinition<LivePollingStateDocument> filter)
    {
        return filter.Render(new RenderArgs<LivePollingStateDocument>(
            BsonSerializer.LookupSerializer<LivePollingStateDocument>(),
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(UpdateDefinition<LivePollingStateDocument> update)
    {
        return update.Render(new RenderArgs<LivePollingStateDocument>(
            BsonSerializer.LookupSerializer<LivePollingStateDocument>(),
            BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
