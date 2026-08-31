using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class DurableBackgroundJobMongoDefinitions
{
    private static readonly DurableBackgroundJobStatus[] ActiveStatuses =
    {
        DurableBackgroundJobStatus.Pending,
        DurableBackgroundJobStatus.Leased,
        DurableBackgroundJobStatus.RetryScheduled,
    };

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildActiveNaturalKeyFilter(
        string kind,
        string naturalKey)
    {
        return Builders<DurableBackgroundJobDocument>.Filter.Eq(item => item.Kind, kind)
            & Builders<DurableBackgroundJobDocument>.Filter.Eq(item => item.NaturalKey, naturalKey)
            & Builders<DurableBackgroundJobDocument>.Filter.In(item => item.Status, ActiveStatuses);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildCoalesceUpdate(
        long requestedRevision,
        int payloadVersion,
        string payloadJson,
        int priority,
        DateTime notBeforeUtc,
        DateTime nowUtc,
        string? correlationId)
    {
        BsonDocument currentRevision = new BsonDocument(
            "$ifNull",
            new BsonArray { "$requestedRevision", new BsonInt64(-1) });
        BsonDocument hasNewerRevision = new BsonDocument(
            "$gt",
            new BsonArray { new BsonInt64(requestedRevision), currentRevision });
        BsonDocument hasHigherPriority = new BsonDocument(
            "$gt",
            new BsonArray { new BsonInt32(priority), "$priority" });
        BsonDocument set = new BsonDocument
        {
            {
                "requestedRevision",
                new BsonDocument("$cond", new BsonArray { hasNewerRevision, requestedRevision, "$requestedRevision" })
            },
            {
                "payloadVersion",
                new BsonDocument("$cond", new BsonArray { hasNewerRevision, payloadVersion, "$payloadVersion" })
            },
            {
                "payload",
                new BsonDocument("$cond", new BsonArray
                {
                    hasNewerRevision,
                    new BsonDocument("$literal", payloadJson),
                    "$payload",
                })
            },
            {
                "priority",
                new BsonDocument("$cond", new BsonArray { hasHigherPriority, priority, "$priority" })
            },
            {
                "notBeforeUtc",
                new BsonDocument("$cond", new BsonArray
                {
                    hasNewerRevision,
                    notBeforeUtc,
                    "$notBeforeUtc",
                })
            },
            {
                "attemptCount",
                new BsonDocument("$cond", new BsonArray { hasNewerRevision, 0, "$attemptCount" })
            },
            { "updatedAt", nowUtc },
        };
        if (correlationId is not null)
        {
            set.Add("correlationId", correlationId);
        }

        PipelineDefinition<DurableBackgroundJobDocument, DurableBackgroundJobDocument> pipeline =
            PipelineDefinition<DurableBackgroundJobDocument, DurableBackgroundJobDocument>.Create(
                new[] { new BsonDocument("$set", set) });
        return Builders<DurableBackgroundJobDocument>.Update.Pipeline(pipeline);
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildScheduledRunnableFilter(
        IReadOnlyCollection<string> kinds,
        DateTime nowUtc)
    {
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters = Builders<DurableBackgroundJobDocument>.Filter;
        return filters.In(item => item.Kind, kinds)
            & filters.In(item => item.Status, new[]
            {
                DurableBackgroundJobStatus.Pending,
                DurableBackgroundJobStatus.RetryScheduled,
            })
            & filters.Lte(item => item.NotBeforeUtc, nowUtc);
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildExpiredLeaseRunnableFilter(
        IReadOnlyCollection<string> kinds,
        DateTime nowUtc)
    {
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters = Builders<DurableBackgroundJobDocument>.Filter;
        return filters.In(item => item.Kind, kinds)
            & filters.Eq(item => item.Status, DurableBackgroundJobStatus.Leased)
            & filters.Lte(item => item.LeaseExpiresAtUtc, nowUtc)
            & filters.Lte(item => item.NotBeforeUtc, nowUtc);
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildActiveKindScanFilter(string? afterKind)
    {
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters = Builders<DurableBackgroundJobDocument>.Filter;
        FilterDefinition<DurableBackgroundJobDocument> filter = filters.In(item => item.Status, ActiveStatuses);
        if (afterKind is not null)
        {
            filter &= filters.Gt(item => item.Kind, afterKind);
        }

        return filter;
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildScheduledUnknownKindRunnableFilter(
        string kind,
        DateTime maximumUpdatedAtUtc,
        DateTime nowUtc)
    {
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters = Builders<DurableBackgroundJobDocument>.Filter;
        return filters.Eq(item => item.Kind, kind)
            & filters.In(item => item.Status, new[]
            {
                DurableBackgroundJobStatus.Pending,
                DurableBackgroundJobStatus.RetryScheduled,
            })
            & filters.Lte(item => item.NotBeforeUtc, nowUtc)
            & filters.Lte(item => item.UpdatedAt, maximumUpdatedAtUtc);
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildExpiredUnknownKindLeaseRunnableFilter(
        string kind,
        DateTime maximumUpdatedAtUtc,
        DateTime nowUtc)
    {
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters = Builders<DurableBackgroundJobDocument>.Filter;
        return filters.Eq(item => item.Kind, kind)
            & filters.Eq(item => item.Status, DurableBackgroundJobStatus.Leased)
            & filters.Lte(item => item.LeaseExpiresAtUtc, nowUtc)
            & filters.Lte(item => item.NotBeforeUtc, nowUtc)
            & filters.Lte(item => item.UpdatedAt, maximumUpdatedAtUtc);
    }

    internal static SortDefinition<DurableBackgroundJobDocument> BuildScheduledRunnableSort()
    {
        return Builders<DurableBackgroundJobDocument>.Sort
            .Descending(item => item.Priority)
            .Ascending(item => item.NotBeforeUtc)
            .Ascending(item => item.CreatedAt);
    }

    internal static SortDefinition<DurableBackgroundJobDocument> BuildExpiredLeaseRunnableSort()
    {
        return Builders<DurableBackgroundJobDocument>.Sort
            .Descending(item => item.Priority)
            .Ascending(item => item.LeaseExpiresAtUtc)
            .Ascending(item => item.CreatedAt);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildLeaseUpdate(
        string leaseOwner,
        string leaseToken,
        DateTime leaseExpiresAtUtc,
        DateTime nowUtc)
    {
        return Builders<DurableBackgroundJobDocument>.Update
            .Set(item => item.Status, DurableBackgroundJobStatus.Leased)
            .Set(item => item.LeaseOwner, leaseOwner)
            .Set(item => item.LeaseToken, leaseToken)
            .Set(item => item.LeaseExpiresAtUtc, leaseExpiresAtUtc)
            .Set(item => item.UpdatedAt, nowUtc)
            .Inc(item => item.AttemptCount, 1)
            .Unset(item => item.CompletedAtUtc);
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildLeaseOwnershipFilter(
        DurableBackgroundJobLease lease,
        DateTime nowUtc)
    {
        string jobId = DurableBackgroundJobRepository.NormalizeRequired(lease.JobId, nameof(lease.JobId));
        string leaseOwner = DurableBackgroundJobRepository.NormalizeRequired(lease.LeaseOwner, nameof(lease.LeaseOwner));
        string leaseToken = DurableBackgroundJobRepository.NormalizeRequired(lease.LeaseToken, nameof(lease.LeaseToken));
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters = Builders<DurableBackgroundJobDocument>.Filter;
        return filters.Eq(item => item.Id, jobId)
            & filters.Eq(item => item.Status, DurableBackgroundJobStatus.Leased)
            & filters.Eq(item => item.LeaseOwner, leaseOwner)
            & filters.Eq(item => item.LeaseToken, leaseToken)
            & filters.Gt(item => item.LeaseExpiresAtUtc, nowUtc);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildRenewLeaseUpdate(
        DateTime leaseExpiresAtUtc,
        DateTime nowUtc)
    {
        return Builders<DurableBackgroundJobDocument>.Update
            .Set(item => item.LeaseExpiresAtUtc, leaseExpiresAtUtc)
            .Set(item => item.UpdatedAt, nowUtc);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildCompletionUpdate(
        long? processedRevision,
        DateTime nowUtc)
    {
        BsonValue processedRevisionValue = processedRevision.HasValue
            ? new BsonInt64(processedRevision.Value)
            : "$processedRevision";
        BsonDocument requiresReplay = BuildRequiresReplay(processedRevision);
        BsonDocument set = new BsonDocument
        {
            { "processedRevision", processedRevisionValue },
            {
                "status",
                new BsonDocument("$cond", new BsonArray
                {
                    requiresReplay,
                    DurableBackgroundJobStatus.Pending.ToString(),
                    DurableBackgroundJobStatus.Succeeded.ToString(),
                })
            },
            { "notBeforeUtc", "$notBeforeUtc" },
            {
                "completedAtUtc",
                new BsonDocument("$cond", new BsonArray { requiresReplay, "$$REMOVE", nowUtc })
            },
            { "leaseOwner", "$$REMOVE" },
            { "leaseToken", "$$REMOVE" },
            { "leaseExpiresAtUtc", "$$REMOVE" },
            { "lastErrorCode", "$$REMOVE" },
            { "updatedAt", nowUtc },
        };
        PipelineDefinition<DurableBackgroundJobDocument, DurableBackgroundJobDocument> pipeline =
            PipelineDefinition<DurableBackgroundJobDocument, DurableBackgroundJobDocument>.Create(
                new[] { new BsonDocument("$set", set) });
        return Builders<DurableBackgroundJobDocument>.Update.Pipeline(pipeline);
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildExpiredLeaseFilter(DateTime nowUtc)
    {
        return Builders<DurableBackgroundJobDocument>.Filter.Eq(
                item => item.Status,
                DurableBackgroundJobStatus.Leased)
            & Builders<DurableBackgroundJobDocument>.Filter.Lte(item => item.LeaseExpiresAtUtc, nowUtc);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildScheduleRetryUpdate(
        long? attemptedRevision,
        DateTime notBeforeUtc,
        string errorCode,
        DateTime nowUtc)
    {
        BsonDocument requiresReplay = BuildRequiresReplay(attemptedRevision);
        BsonDocument set = new BsonDocument
        {
            {
                "status",
                new BsonDocument("$cond", new BsonArray
                {
                    requiresReplay,
                    DurableBackgroundJobStatus.Pending.ToString(),
                    DurableBackgroundJobStatus.RetryScheduled.ToString(),
                })
            },
            {
                "notBeforeUtc",
                new BsonDocument("$cond", new BsonArray { requiresReplay, "$notBeforeUtc", notBeforeUtc })
            },
            {
                "lastErrorCode",
                new BsonDocument("$cond", new BsonArray { requiresReplay, "$$REMOVE", errorCode })
            },
            { "leaseOwner", "$$REMOVE" },
            { "leaseToken", "$$REMOVE" },
            { "leaseExpiresAtUtc", "$$REMOVE" },
            { "completedAtUtc", "$$REMOVE" },
            { "updatedAt", nowUtc },
        };
        PipelineDefinition<DurableBackgroundJobDocument, DurableBackgroundJobDocument> pipeline =
            PipelineDefinition<DurableBackgroundJobDocument, DurableBackgroundJobDocument>.Create(
                new[] { new BsonDocument("$set", set) });
        return Builders<DurableBackgroundJobDocument>.Update.Pipeline(pipeline);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildDeadLetterUpdate(
        long? attemptedRevision,
        string errorCode,
        DateTime nowUtc)
    {
        BsonDocument requiresReplay = BuildRequiresReplay(attemptedRevision);
        BsonDocument set = new BsonDocument
        {
            {
                "status",
                new BsonDocument("$cond", new BsonArray
                {
                    requiresReplay,
                    DurableBackgroundJobStatus.Pending.ToString(),
                    DurableBackgroundJobStatus.DeadLetter.ToString(),
                })
            },
            {
                "lastErrorCode",
                new BsonDocument("$cond", new BsonArray { requiresReplay, "$$REMOVE", errorCode })
            },
            {
                "completedAtUtc",
                new BsonDocument("$cond", new BsonArray { requiresReplay, "$$REMOVE", nowUtc })
            },
            { "leaseOwner", "$$REMOVE" },
            { "leaseToken", "$$REMOVE" },
            { "leaseExpiresAtUtc", "$$REMOVE" },
            { "updatedAt", nowUtc },
        };
        PipelineDefinition<DurableBackgroundJobDocument, DurableBackgroundJobDocument> pipeline =
            PipelineDefinition<DurableBackgroundJobDocument, DurableBackgroundJobDocument>.Create(
                new[] { new BsonDocument("$set", set) });
        return Builders<DurableBackgroundJobDocument>.Update.Pipeline(pipeline);
    }

    private static BsonDocument BuildRequiresReplay(long? attemptedRevision)
    {
        BsonValue attemptedRevisionValue = attemptedRevision.HasValue
            ? new BsonInt64(attemptedRevision.Value)
            : BsonNull.Value;
        return new BsonDocument(
            "$gt",
            new BsonArray { "$requestedRevision", attemptedRevisionValue });
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildCancelFilter(string jobId)
    {
        return Builders<DurableBackgroundJobDocument>.Filter.Eq(item => item.Id, jobId)
            & Builders<DurableBackgroundJobDocument>.Filter.In(item => item.Status, ActiveStatuses);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildCancelUpdate(DateTime nowUtc)
    {
        return Builders<DurableBackgroundJobDocument>.Update
            .Set(item => item.Status, DurableBackgroundJobStatus.Cancelled)
            .Set(item => item.CompletedAtUtc, nowUtc)
            .Set(item => item.UpdatedAt, nowUtc)
            .Unset(item => item.LeaseOwner)
            .Unset(item => item.LeaseToken)
            .Unset(item => item.LeaseExpiresAtUtc);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildReleaseExpiredLeaseUpdate(DateTime nowUtc)
    {
        return Builders<DurableBackgroundJobDocument>.Update
            .Set(item => item.Status, DurableBackgroundJobStatus.RetryScheduled)
            .Set(item => item.UpdatedAt, nowUtc)
            .Unset(item => item.LeaseOwner)
            .Unset(item => item.LeaseToken)
            .Unset(item => item.LeaseExpiresAtUtc);
    }
}
