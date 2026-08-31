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
        int priority,
        DateTime notBeforeUtc,
        DateTime nowUtc,
        string? correlationId)
    {
        UpdateDefinitionBuilder<DurableBackgroundJobDocument> updates = Builders<DurableBackgroundJobDocument>.Update;
        List<UpdateDefinition<DurableBackgroundJobDocument>> definitions = new List<UpdateDefinition<DurableBackgroundJobDocument>>
        {
            updates.Max(item => item.RequestedRevision, requestedRevision),
            updates.Max(item => item.Priority, priority),
            updates.Min(item => item.NotBeforeUtc, notBeforeUtc),
            updates.Set(item => item.UpdatedAt, nowUtc),
        };
        if (correlationId is not null)
        {
            definitions.Add(updates.Set(item => item.CorrelationId, correlationId));
        }

        return updates.Combine(definitions);
    }

    internal static FilterDefinition<DurableBackgroundJobDocument> BuildRunnableFilter(
        IReadOnlyCollection<string> kinds,
        DateTime nowUtc)
    {
        FilterDefinitionBuilder<DurableBackgroundJobDocument> filters = Builders<DurableBackgroundJobDocument>.Filter;
        FilterDefinition<DurableBackgroundJobDocument> scheduled =
            filters.In(item => item.Status, new[]
            {
                DurableBackgroundJobStatus.Pending,
                DurableBackgroundJobStatus.RetryScheduled,
            })
            & filters.Lte(item => item.NotBeforeUtc, nowUtc);
        FilterDefinition<DurableBackgroundJobDocument> expiredLease =
            filters.Eq(item => item.Status, DurableBackgroundJobStatus.Leased)
            & filters.Lte(item => item.LeaseExpiresAtUtc, nowUtc);
        return filters.In(item => item.Kind, kinds) & filters.Or(scheduled, expiredLease);
    }

    internal static SortDefinition<DurableBackgroundJobDocument> BuildRunnableSort()
    {
        return Builders<DurableBackgroundJobDocument>.Sort
            .Descending(item => item.Priority)
            .Ascending(item => item.NotBeforeUtc)
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
        BsonDocument requiresReplay = new BsonDocument(
            "$gt",
            new BsonArray { "$requestedRevision", processedRevision.HasValue ? new BsonInt64(processedRevision.Value) : BsonNull.Value });
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
            {
                "notBeforeUtc",
                new BsonDocument("$cond", new BsonArray { requiresReplay, nowUtc, "$notBeforeUtc" })
            },
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
        DateTime notBeforeUtc,
        string errorCode,
        DateTime nowUtc)
    {
        return Builders<DurableBackgroundJobDocument>.Update
            .Set(item => item.Status, DurableBackgroundJobStatus.RetryScheduled)
            .Set(item => item.NotBeforeUtc, notBeforeUtc)
            .Set(item => item.LastErrorCode, errorCode)
            .Set(item => item.UpdatedAt, nowUtc)
            .Unset(item => item.LeaseOwner)
            .Unset(item => item.LeaseToken)
            .Unset(item => item.LeaseExpiresAtUtc)
            .Unset(item => item.CompletedAtUtc);
    }

    internal static UpdateDefinition<DurableBackgroundJobDocument> BuildDeadLetterUpdate(
        string errorCode,
        DateTime nowUtc)
    {
        return Builders<DurableBackgroundJobDocument>.Update
            .Set(item => item.Status, DurableBackgroundJobStatus.DeadLetter)
            .Set(item => item.LastErrorCode, errorCode)
            .Set(item => item.CompletedAtUtc, nowUtc)
            .Set(item => item.UpdatedAt, nowUtc)
            .Unset(item => item.LeaseOwner)
            .Unset(item => item.LeaseToken)
            .Unset(item => item.LeaseExpiresAtUtc);
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
            .Set(item => item.NotBeforeUtc, nowUtc)
            .Set(item => item.UpdatedAt, nowUtc)
            .Unset(item => item.LeaseOwner)
            .Unset(item => item.LeaseToken)
            .Unset(item => item.LeaseExpiresAtUtc);
    }
}
