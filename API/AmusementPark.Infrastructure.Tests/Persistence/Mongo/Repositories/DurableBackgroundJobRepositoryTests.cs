using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;
using DurableBackgroundJobRepository = AmusementPark.Infrastructure.Persistence.Mongo.Repositories.DurableBackgroundJobMongoDefinitions;
using DurableBackgroundJobStore = AmusementPark.Infrastructure.Persistence.Mongo.Repositories.DurableBackgroundJobRepository;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class DurableBackgroundJobRepositoryTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 8, 31, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TryLeaseNextAsync_ShouldCheckExpiredLeasesBeforeScheduledJobs()
    {
        Mock<IMongoCollection<DurableBackgroundJobDocument>> collection =
            new Mock<IMongoCollection<DurableBackgroundJobDocument>>(MockBehavior.Strict);
        List<FilterDefinition<DurableBackgroundJobDocument>> filters =
            new List<FilterDefinition<DurableBackgroundJobDocument>>();
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<DurableBackgroundJobDocument>>(),
                It.IsAny<UpdateDefinition<DurableBackgroundJobDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<DurableBackgroundJobDocument, DurableBackgroundJobDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<DurableBackgroundJobDocument> filter,
                UpdateDefinition<DurableBackgroundJobDocument> _,
                FindOneAndUpdateOptions<DurableBackgroundJobDocument, DurableBackgroundJobDocument> _,
                CancellationToken _) => filters.Add(filter))
            .ReturnsAsync((DurableBackgroundJobDocument)null!);
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<DurableBackgroundJobDocument>("background_jobs", null))
            .Returns(collection.Object);
        DurableBackgroundJobStore repository = new DurableBackgroundJobStore(
            database.Object,
            new MongoDbSettings { DurableBackgroundJobsCollectionName = "background_jobs" });

        DurableBackgroundJob? leased = await repository.TryLeaseNextAsync(
            new LeaseBackgroundJobRequest(new[] { "rank.snapshot" }, "worker-1", TimeSpan.FromMinutes(2)),
            CancellationToken.None);

        Assert.Null(leased);
        Assert.Equal(2, filters.Count);
        Assert.Equal(DurableBackgroundJobStatus.Leased.ToString(), Render(filters[0])["status"].AsString);
        BsonArray scheduledStatuses = Render(filters[1])["status"].AsBsonDocument["$in"].AsBsonArray;
        Assert.Contains(
            DurableBackgroundJobStatus.Pending.ToString(),
            scheduledStatuses.Select(static item => item.AsString));
        collection.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public void BuildScheduledRunnableFilter_ShouldSelectDueJobsForRequestedKinds()
    {
        FilterDefinition<DurableBackgroundJobDocument> filter =
            DurableBackgroundJobRepository.BuildScheduledRunnableFilter(
                new[] { "rank.snapshot", "seo.refresh" },
                NowUtc);

        BsonDocument rendered = Render(filter);

        BsonArray kinds = rendered["kind"].AsBsonDocument["$in"].AsBsonArray;
        Assert.Equal(new[] { "rank.snapshot", "seo.refresh" }, kinds.Select(static item => item.AsString));
        Assert.Equal(
            new[] { DurableBackgroundJobStatus.Pending.ToString(), DurableBackgroundJobStatus.RetryScheduled.ToString() },
            rendered["status"].AsBsonDocument["$in"].AsBsonArray.Select(static item => item.AsString));
        Assert.Equal(NowUtc, rendered["notBeforeUtc"].AsBsonDocument["$lte"].ToUniversalTime());
    }

    [Fact]
    public void BuildExpiredLeaseRunnableFilter_ShouldSelectExpiredLeasesForRequestedKinds()
    {
        FilterDefinition<DurableBackgroundJobDocument> filter =
            DurableBackgroundJobRepository.BuildExpiredLeaseRunnableFilter(
                new[] { "rank.snapshot", "seo.refresh" },
                NowUtc);

        BsonDocument rendered = Render(filter);

        Assert.Equal(DurableBackgroundJobStatus.Leased.ToString(), rendered["status"].AsString);
        Assert.Equal(NowUtc, rendered["leaseExpiresAtUtc"].AsBsonDocument["$lte"].ToUniversalTime());
        Assert.Equal(NowUtc, rendered["notBeforeUtc"].AsBsonDocument["$lte"].ToUniversalTime());
    }

    [Fact]
    public void BuildScheduledRunnableSort_ShouldPrioritizePriorityThenScheduleThenCreation()
    {
        BsonDocument rendered = Render(DurableBackgroundJobRepository.BuildScheduledRunnableSort());

        Assert.Equal(-1, rendered["priority"].AsInt32);
        Assert.Equal(1, rendered["notBeforeUtc"].AsInt32);
        Assert.Equal(1, rendered["createdAt"].AsInt32);
    }

    [Fact]
    public void BuildExpiredLeaseRunnableSort_ShouldPrioritizePriorityThenExpiryThenCreation()
    {
        BsonDocument rendered = Render(DurableBackgroundJobRepository.BuildExpiredLeaseRunnableSort());

        Assert.Equal(-1, rendered["priority"].AsInt32);
        Assert.Equal(1, rendered["leaseExpiresAtUtc"].AsInt32);
        Assert.Equal(1, rendered["createdAt"].AsInt32);
    }

    [Fact]
    public void BuildLeaseUpdate_ShouldFenceAndCountTheAttempt()
    {
        DateTime expiresAtUtc = NowUtc.AddMinutes(2);
        BsonDocument rendered = Render(DurableBackgroundJobRepository.BuildLeaseUpdate(
            "worker-1",
            "token-1",
            expiresAtUtc,
            NowUtc)).AsBsonDocument;

        BsonDocument set = rendered["$set"].AsBsonDocument;
        Assert.Equal(DurableBackgroundJobStatus.Leased.ToString(), set["status"].AsString);
        Assert.Equal("worker-1", set["leaseOwner"].AsString);
        Assert.Equal("token-1", set["leaseToken"].AsString);
        Assert.Equal(expiresAtUtc, set["leaseExpiresAtUtc"].ToUniversalTime());
        Assert.Equal(1, rendered["$inc"].AsBsonDocument["attemptCount"].AsInt32);
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("completedAtUtc"));
    }

    [Fact]
    public void BuildLeaseOwnershipFilter_ShouldRequireTokenOwnerAndUnexpiredLease()
    {
        DurableBackgroundJobLease lease = new DurableBackgroundJobLease(" job-1 ", " worker-1 ", " token-1 ");

        BsonDocument rendered = Render(DurableBackgroundJobRepository.BuildLeaseOwnershipFilter(lease, NowUtc));

        Assert.Equal("job-1", rendered["_id"].AsString);
        Assert.Equal(DurableBackgroundJobStatus.Leased.ToString(), rendered["status"].AsString);
        Assert.Equal("worker-1", rendered["leaseOwner"].AsString);
        Assert.Equal("token-1", rendered["leaseToken"].AsString);
        Assert.Equal(NowUtc, rendered["leaseExpiresAtUtc"].AsBsonDocument["$gt"].ToUniversalTime());
    }

    [Fact]
    public void BuildCoalesceUpdate_ShouldOnlyAdvancePayloadAndScheduleForANewerRevision()
    {
        BsonValue rendered = Render(DurableBackgroundJobRepository.BuildCoalesceUpdate(
            17,
            3,
            "{\"scope\":\"world\",\"revision\":17}",
            40,
            NowUtc.AddMinutes(3),
            NowUtc,
            "correlation-1"));

        BsonDocument set = Assert.Single(rendered.AsBsonArray).AsBsonDocument["$set"].AsBsonDocument;
        BsonArray revisionCondition = set["requestedRevision"].AsBsonDocument["$cond"].AsBsonArray;
        BsonDocument newerRevision = revisionCondition[0].AsBsonDocument;
        Assert.Equal(17, revisionCondition[1].AsInt64);
        Assert.Equal("$requestedRevision", revisionCondition[2].AsString);
        Assert.Equal(17, newerRevision["$gt"].AsBsonArray[0].AsInt64);

        BsonArray payloadVersionCondition = set["payloadVersion"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal(newerRevision, payloadVersionCondition[0].AsBsonDocument);
        Assert.Equal(3, payloadVersionCondition[1].AsInt32);
        Assert.Equal("$payloadVersion", payloadVersionCondition[2].AsString);
        BsonArray payloadCondition = set["payload"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal(newerRevision, payloadCondition[0].AsBsonDocument);
        Assert.Equal(
            "{\"scope\":\"world\",\"revision\":17}",
            payloadCondition[1].AsBsonDocument["$literal"].AsString);
        Assert.Equal("$payload", payloadCondition[2].AsString);

        BsonArray scheduleCondition = set["notBeforeUtc"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal(newerRevision, scheduleCondition[0].AsBsonDocument);
        Assert.Equal("$notBeforeUtc", scheduleCondition[2].AsString);
        Assert.Equal(
            NowUtc.AddMinutes(3),
            scheduleCondition[1].ToUniversalTime());

        BsonArray attemptCountCondition = set["attemptCount"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal(newerRevision, attemptCountCondition[0].AsBsonDocument);
        Assert.Equal(0, attemptCountCondition[1].AsInt32);
        Assert.Equal("$attemptCount", attemptCountCondition[2].AsString);

        BsonArray priorityCondition = set["priority"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal(40, priorityCondition[1].AsInt32);
        Assert.Equal("$priority", priorityCondition[2].AsString);
        Assert.Equal("correlation-1", set["correlationId"].AsString);
    }

    [Fact]
    public void BuildCompletionUpdate_ShouldAtomicallyReplayANewerRevisionAndClearTheLease()
    {
        BsonValue rendered = Render(DurableBackgroundJobRepository.BuildCompletionUpdate(16, NowUtc));

        BsonDocument set = Assert.Single(rendered.AsBsonArray).AsBsonDocument["$set"].AsBsonDocument;
        Assert.Equal(16, set["processedRevision"].AsInt64);
        BsonArray statusCondition = set["status"].AsBsonDocument["$cond"].AsBsonArray;
        BsonArray revisionComparison = statusCondition[0].AsBsonDocument["$gt"].AsBsonArray;
        Assert.Equal("$requestedRevision", revisionComparison[0].AsString);
        Assert.Equal(16, revisionComparison[1].AsInt64);
        Assert.Equal(DurableBackgroundJobStatus.Pending.ToString(), statusCondition[1].AsString);
        Assert.Equal(DurableBackgroundJobStatus.Succeeded.ToString(), statusCondition[2].AsString);
        Assert.Equal("$notBeforeUtc", set["notBeforeUtc"].AsString);
        Assert.Equal("$$REMOVE", set["leaseOwner"].AsString);
        Assert.Equal("$$REMOVE", set["leaseToken"].AsString);
        Assert.Equal("$$REMOVE", set["leaseExpiresAtUtc"].AsString);
        Assert.Equal("$$REMOVE", set["lastErrorCode"].AsString);
    }

    [Fact]
    public void BuildExpiredLeaseFilter_ShouldOnlySelectExpiredLeases()
    {
        BsonDocument rendered = Render(DurableBackgroundJobRepository.BuildExpiredLeaseFilter(NowUtc));

        Assert.Equal(DurableBackgroundJobStatus.Leased.ToString(), rendered["status"].AsString);
        Assert.Equal(NowUtc, rendered["leaseExpiresAtUtc"].AsBsonDocument["$lte"].ToUniversalTime());
    }

    [Fact]
    public void BuildScheduledUnknownKindRunnableFilter_ShouldExcludeKnownKindsAndEnforceTheGracePeriod()
    {
        DateTime maximumUpdatedAtUtc = NowUtc.AddHours(-1);

        BsonDocument rendered = Render(
            DurableBackgroundJobRepository.BuildScheduledUnknownKindRunnableFilter(
                new[] { "rank.compute", "projection.refresh" },
                maximumUpdatedAtUtc,
                NowUtc));

        Assert.Equal(
            new[] { "rank.compute", "projection.refresh" },
            rendered["kind"].AsBsonDocument["$nin"].AsBsonArray.Select(static item => item.AsString));
        Assert.Equal(maximumUpdatedAtUtc, rendered["updatedAt"].AsBsonDocument["$lte"].ToUniversalTime());
        Assert.Equal(NowUtc, rendered["notBeforeUtc"].AsBsonDocument["$lte"].ToUniversalTime());
        Assert.Equal(
            new[]
            {
                DurableBackgroundJobStatus.Pending.ToString(),
                DurableBackgroundJobStatus.RetryScheduled.ToString(),
            },
            rendered["status"].AsBsonDocument["$in"].AsBsonArray.Select(static item => item.AsString));
    }

    [Fact]
    public void BuildExpiredUnknownKindLeaseRunnableFilter_ShouldRequireAnExpiredAgedLease()
    {
        DateTime maximumUpdatedAtUtc = NowUtc.AddHours(-1);

        BsonDocument rendered = Render(
            DurableBackgroundJobRepository.BuildExpiredUnknownKindLeaseRunnableFilter(
                Array.Empty<string>(),
                maximumUpdatedAtUtc,
                NowUtc));

        Assert.Empty(rendered["kind"].AsBsonDocument["$nin"].AsBsonArray);
        Assert.Equal(DurableBackgroundJobStatus.Leased.ToString(), rendered["status"].AsString);
        Assert.Equal(NowUtc, rendered["leaseExpiresAtUtc"].AsBsonDocument["$lte"].ToUniversalTime());
        Assert.Equal(maximumUpdatedAtUtc, rendered["updatedAt"].AsBsonDocument["$lte"].ToUniversalTime());
    }

    [Fact]
    public void BuildRenewLeaseUpdate_ShouldOnlyExtendTheOwnedLeaseMetadata()
    {
        DateTime expiresAtUtc = NowUtc.AddMinutes(2);

        BsonDocument rendered = Render(
            DurableBackgroundJobRepository.BuildRenewLeaseUpdate(expiresAtUtc, NowUtc)).AsBsonDocument;

        Assert.Single(rendered);
        Assert.Equal(expiresAtUtc, rendered["$set"].AsBsonDocument["leaseExpiresAtUtc"].ToUniversalTime());
        Assert.Equal(NowUtc, rendered["$set"].AsBsonDocument["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public void WasSingleJobMatched_ShouldAcceptAnOwnedLeaseWithoutPhysicalModification()
    {
        UpdateResult result = new UpdateResult.Acknowledged(1, 0, null);

        bool matched = DurableBackgroundJobStore.WasSingleJobMatched(result);

        Assert.True(matched);
    }

    [Fact]
    public void WasSingleJobMatched_ShouldRejectAMissingLease()
    {
        UpdateResult result = new UpdateResult.Acknowledged(0, 0, null);

        bool matched = DurableBackgroundJobStore.WasSingleJobMatched(result);

        Assert.False(matched);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public void CanRetryCoalesceInsert_ShouldBoundTerminalRaceRetries(int failedAttempt, bool expected)
    {
        bool canRetry = DurableBackgroundJobStore.CanRetryCoalesceInsert(failedAttempt);

        Assert.Equal(expected, canRetry);
    }

    [Fact]
    public void BuildScheduleRetryUpdate_ShouldReplayANewerRevisionOrRetryTheAttemptedRevision()
    {
        DateTime notBeforeUtc = NowUtc.AddMinutes(5);

        BsonValue rendered = Render(
            DurableBackgroundJobRepository.BuildScheduleRetryUpdate(16, notBeforeUtc, "temporary", NowUtc));

        BsonDocument set = Assert.Single(rendered.AsBsonArray).AsBsonDocument["$set"].AsBsonDocument;
        BsonArray statusCondition = set["status"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal(DurableBackgroundJobStatus.Pending.ToString(), statusCondition[1].AsString);
        Assert.Equal(DurableBackgroundJobStatus.RetryScheduled.ToString(), statusCondition[2].AsString);
        BsonArray scheduleCondition = set["notBeforeUtc"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal("$notBeforeUtc", scheduleCondition[1].AsString);
        Assert.Equal(notBeforeUtc, scheduleCondition[2].ToUniversalTime());
        BsonArray errorCondition = set["lastErrorCode"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal("$$REMOVE", errorCondition[1].AsString);
        Assert.Equal("temporary", errorCondition[2].AsString);
        AssertLeaseMetadataIsRemoved(set);
        Assert.Equal("$$REMOVE", set["completedAtUtc"].AsString);
    }

    [Fact]
    public void BuildDeadLetterUpdate_ShouldReplayANewerRevisionOrTerminateTheAttemptedRevision()
    {
        BsonValue rendered = Render(
            DurableBackgroundJobRepository.BuildDeadLetterUpdate(16, "permanent", NowUtc));

        BsonDocument set = Assert.Single(rendered.AsBsonArray).AsBsonDocument["$set"].AsBsonDocument;
        BsonArray statusCondition = set["status"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal(DurableBackgroundJobStatus.Pending.ToString(), statusCondition[1].AsString);
        Assert.Equal(DurableBackgroundJobStatus.DeadLetter.ToString(), statusCondition[2].AsString);
        BsonArray errorCondition = set["lastErrorCode"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal("$$REMOVE", errorCondition[1].AsString);
        Assert.Equal("permanent", errorCondition[2].AsString);
        BsonArray completionCondition = set["completedAtUtc"].AsBsonDocument["$cond"].AsBsonArray;
        Assert.Equal("$$REMOVE", completionCondition[1].AsString);
        Assert.Equal(NowUtc, completionCondition[2].ToUniversalTime());
        AssertLeaseMetadataIsRemoved(set);
    }

    [Fact]
    public void BuildCancelDefinitions_ShouldOnlyTerminateAnActiveJob()
    {
        BsonDocument filter = Render(DurableBackgroundJobRepository.BuildCancelFilter("job-1"));
        BsonDocument update = Render(DurableBackgroundJobRepository.BuildCancelUpdate(NowUtc)).AsBsonDocument;

        Assert.Equal("job-1", filter["_id"].AsString);
        Assert.Equal(
            new[]
            {
                DurableBackgroundJobStatus.Pending.ToString(),
                DurableBackgroundJobStatus.Leased.ToString(),
                DurableBackgroundJobStatus.RetryScheduled.ToString(),
            },
            filter["status"].AsBsonDocument["$in"].AsBsonArray.Select(static item => item.AsString));
        Assert.Equal(DurableBackgroundJobStatus.Cancelled.ToString(), update["$set"].AsBsonDocument["status"].AsString);
        Assert.Equal(NowUtc, update["$set"].AsBsonDocument["completedAtUtc"].ToUniversalTime());
        AssertLeaseMetadataIsUnset(update);
    }

    [Fact]
    public void BuildReleaseExpiredLeaseUpdate_ShouldPreserveTheRequestedSchedule()
    {
        BsonDocument rendered = Render(
            DurableBackgroundJobRepository.BuildReleaseExpiredLeaseUpdate(NowUtc)).AsBsonDocument;

        Assert.Equal(DurableBackgroundJobStatus.RetryScheduled.ToString(), rendered["$set"].AsBsonDocument["status"].AsString);
        Assert.False(rendered["$set"].AsBsonDocument.Contains("notBeforeUtc"));
        AssertLeaseMetadataIsUnset(rendered);
    }

    private static void AssertLeaseMetadataIsUnset(BsonDocument rendered)
    {
        BsonDocument unset = rendered["$unset"].AsBsonDocument;
        Assert.True(unset.Contains("leaseOwner"));
        Assert.True(unset.Contains("leaseToken"));
        Assert.True(unset.Contains("leaseExpiresAtUtc"));
    }

    private static void AssertLeaseMetadataIsRemoved(BsonDocument set)
    {
        Assert.Equal("$$REMOVE", set["leaseOwner"].AsString);
        Assert.Equal("$$REMOVE", set["leaseToken"].AsString);
        Assert.Equal("$$REMOVE", set["leaseExpiresAtUtc"].AsString);
    }

    private static BsonDocument Render(FilterDefinition<DurableBackgroundJobDocument> filter)
    {
        IBsonSerializer<DurableBackgroundJobDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<DurableBackgroundJobDocument>();
        RenderArgs<DurableBackgroundJobDocument> arguments =
            new RenderArgs<DurableBackgroundJobDocument>(serializer, BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(SortDefinition<DurableBackgroundJobDocument> sort)
    {
        IBsonSerializer<DurableBackgroundJobDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<DurableBackgroundJobDocument>();
        RenderArgs<DurableBackgroundJobDocument> arguments =
            new RenderArgs<DurableBackgroundJobDocument>(serializer, BsonSerializer.SerializerRegistry);
        return sort.Render(arguments);
    }

    private static BsonValue Render(UpdateDefinition<DurableBackgroundJobDocument> update)
    {
        IBsonSerializer<DurableBackgroundJobDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<DurableBackgroundJobDocument>();
        RenderArgs<DurableBackgroundJobDocument> arguments =
            new RenderArgs<DurableBackgroundJobDocument>(serializer, BsonSerializer.SerializerRegistry);
        return update.Render(arguments);
    }
}
