using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;
using DurableBackgroundJobRepository = AmusementPark.Infrastructure.Persistence.Mongo.Repositories.DurableBackgroundJobMongoDefinitions;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class DurableBackgroundJobRepositoryTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 8, 31, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildRunnableFilter_ShouldSelectDueOrExpiredJobsForRequestedKinds()
    {
        FilterDefinition<DurableBackgroundJobDocument> filter =
            DurableBackgroundJobRepository.BuildRunnableFilter(new[] { "rank.snapshot", "seo.refresh" }, NowUtc);

        BsonDocument rendered = Render(filter);

        BsonArray kinds = rendered["kind"].AsBsonDocument["$in"].AsBsonArray;
        Assert.Equal(new[] { "rank.snapshot", "seo.refresh" }, kinds.Select(static item => item.AsString));
        BsonArray alternatives = rendered["$or"].AsBsonArray;
        Assert.Equal(2, alternatives.Count);
        Assert.Equal(
            new[] { DurableBackgroundJobStatus.Pending.ToString(), DurableBackgroundJobStatus.RetryScheduled.ToString() },
            alternatives[0].AsBsonDocument["status"].AsBsonDocument["$in"].AsBsonArray.Select(static item => item.AsString));
        Assert.Equal(NowUtc, alternatives[0].AsBsonDocument["notBeforeUtc"].AsBsonDocument["$lte"].ToUniversalTime());
        Assert.Equal(DurableBackgroundJobStatus.Leased.ToString(), alternatives[1].AsBsonDocument["status"].AsString);
        Assert.Equal(NowUtc, alternatives[1].AsBsonDocument["leaseExpiresAtUtc"].AsBsonDocument["$lte"].ToUniversalTime());
    }

    [Fact]
    public void BuildRunnableSort_ShouldPrioritizePriorityThenScheduleThenCreation()
    {
        BsonDocument rendered = Render(DurableBackgroundJobRepository.BuildRunnableSort());

        Assert.Equal(-1, rendered["priority"].AsInt32);
        Assert.Equal(1, rendered["notBeforeUtc"].AsInt32);
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
    public void BuildCoalesceUpdate_ShouldAdvanceSignalsWithoutReplacingPayload()
    {
        BsonDocument rendered = Render(DurableBackgroundJobRepository.BuildCoalesceUpdate(
            17,
            40,
            NowUtc.AddMinutes(3),
            NowUtc,
            "correlation-1")).AsBsonDocument;

        Assert.Equal(17, rendered["$max"].AsBsonDocument["requestedRevision"].AsInt64);
        Assert.Equal(40, rendered["$max"].AsBsonDocument["priority"].AsInt32);
        Assert.Equal(
            NowUtc.AddMinutes(3),
            rendered["$min"].AsBsonDocument["notBeforeUtc"].ToUniversalTime());
        Assert.Equal("correlation-1", rendered["$set"].AsBsonDocument["correlationId"].AsString);
        Assert.DoesNotContain("payload", rendered.ToJson(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payloadVersion", rendered.ToJson(), StringComparison.OrdinalIgnoreCase);
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
    public void BuildScheduleRetryUpdate_ShouldRequeueAndReleaseTheLease()
    {
        DateTime notBeforeUtc = NowUtc.AddMinutes(5);

        BsonDocument rendered = Render(
            DurableBackgroundJobRepository.BuildScheduleRetryUpdate(notBeforeUtc, "temporary", NowUtc)).AsBsonDocument;

        Assert.Equal(DurableBackgroundJobStatus.RetryScheduled.ToString(), rendered["$set"].AsBsonDocument["status"].AsString);
        Assert.Equal(notBeforeUtc, rendered["$set"].AsBsonDocument["notBeforeUtc"].ToUniversalTime());
        Assert.Equal("temporary", rendered["$set"].AsBsonDocument["lastErrorCode"].AsString);
        AssertLeaseMetadataIsUnset(rendered);
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("completedAtUtc"));
    }

    [Fact]
    public void BuildDeadLetterUpdate_ShouldTerminateAndReleaseTheLease()
    {
        BsonDocument rendered = Render(
            DurableBackgroundJobRepository.BuildDeadLetterUpdate("permanent", NowUtc)).AsBsonDocument;

        Assert.Equal(DurableBackgroundJobStatus.DeadLetter.ToString(), rendered["$set"].AsBsonDocument["status"].AsString);
        Assert.Equal("permanent", rendered["$set"].AsBsonDocument["lastErrorCode"].AsString);
        Assert.Equal(NowUtc, rendered["$set"].AsBsonDocument["completedAtUtc"].ToUniversalTime());
        AssertLeaseMetadataIsUnset(rendered);
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
    public void BuildReleaseExpiredLeaseUpdate_ShouldMakeTheJobImmediatelyRunnable()
    {
        BsonDocument rendered = Render(
            DurableBackgroundJobRepository.BuildReleaseExpiredLeaseUpdate(NowUtc)).AsBsonDocument;

        Assert.Equal(DurableBackgroundJobStatus.RetryScheduled.ToString(), rendered["$set"].AsBsonDocument["status"].AsString);
        Assert.Equal(NowUtc, rendered["$set"].AsBsonDocument["notBeforeUtc"].ToUniversalTime());
        AssertLeaseMetadataIsUnset(rendered);
    }

    private static void AssertLeaseMetadataIsUnset(BsonDocument rendered)
    {
        BsonDocument unset = rendered["$unset"].AsBsonDocument;
        Assert.True(unset.Contains("leaseOwner"));
        Assert.True(unset.Contains("leaseToken"));
        Assert.True(unset.Contains("leaseExpiresAtUtc"));
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
