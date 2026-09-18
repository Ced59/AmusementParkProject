using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripAuditRepository : ITripAuditWriter, ITripAuditReader, ITripAuditReconciler
{
    public const int MaximumReconciliationBatchSize = 50;
    public static readonly TimeSpan MaterializedOutboxRetention = TimeSpan.FromDays(7);

    private readonly IMongoCollection<TripPlanDocument> plans;
    private readonly IMongoCollection<TripActivityEventDocument> activities;
    private readonly IMongoCollection<TripActivityOutboxDocument> outbox;
    private readonly ILogger<TripAuditRepository> logger;

    public TripAuditRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        ILogger<TripAuditRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.plans = database.GetCollection<TripPlanDocument>(settings.TripPlansCollectionName);
        this.activities = database.GetCollection<TripActivityEventDocument>(
            settings.TripAuditEventsCollectionName);
        this.outbox = database.GetCollection<TripActivityOutboxDocument>(
            settings.TripAuditOutboxCollectionName);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal TripAuditRepository(
        IMongoCollection<TripPlanDocument> plans,
        IMongoCollection<TripActivityEventDocument> activities,
        IMongoCollection<TripActivityOutboxDocument> outbox,
        ILogger<TripAuditRepository>? logger = null)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.activities = activities ?? throw new ArgumentNullException(nameof(activities));
        this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        this.logger = logger ?? NullLogger<TripAuditRepository>.Instance;
    }

    public async Task<bool> AppendAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        TripActivityOutboxDocument pending = await this.EnqueueAsync(
            activity,
            cancellationToken);
        try
        {
            return await this.MaterializeAsync(pending, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Unable to materialize trip activity {OperationKey}; the durable outbox will retry.",
                pending.OperationKey);
            return false;
        }
    }

    public async Task<int> ReconcilePendingAsync(
        int maximumCount,
        CancellationToken cancellationToken)
    {
        if (maximumCount is < 1 or > MaximumReconciliationBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        List<TripActivityOutboxDocument> pending = await this.outbox.Find(
                Builders<TripActivityOutboxDocument>.Filter.Eq(
                    static document => document.MaterializedAtUtc,
                    null))
            .SortBy(static document => document.CreatedAt)
            .ThenBy(static document => document.Id)
            .Limit(maximumCount)
            .ToListAsync(cancellationToken);
        int reconciledCount = 0;
        foreach (TripActivityOutboxDocument item in pending)
        {
            try
            {
                if (await this.MaterializeAsync(item, cancellationToken))
                {
                    reconciledCount++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to reconcile trip activity {OperationKey}; it remains pending.",
                    item.OperationKey);
            }
        }

        return reconciledCount;
    }

    private async Task<TripActivityOutboxDocument> EnqueueAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        TripActivityOutboxDocument document = new TripActivityOutboxDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            TripPlanId = activity.TripPlanId.Value,
            ActorMemberId = activity.ActorMemberId?.Value,
            ActorRole = activity.ActorRole,
            Kind = activity.Kind,
            OperationKey = activity.OperationKey,
            AffectedCount = activity.AffectedCount,
            OccurredAtUtc = activity.OccurredAtUtc,
            CreatedAt = activity.OccurredAtUtc,
            UpdatedAt = activity.OccurredAtUtc,
        };
        try
        {
            await this.outbox.InsertOneAsync(document, cancellationToken: cancellationToken);
            return document;
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            TripActivityOutboxDocument? existing = await this.FindOutboxByOperationAsync(
                activity.TripPlanId,
                activity.OperationKey,
                cancellationToken);
            if (existing is null || !HasSameActivity(existing, activity))
            {
                throw;
            }

            return existing;
        }
    }

    private async Task<bool> MaterializeAsync(
        TripActivityOutboxDocument pending,
        CancellationToken cancellationToken)
    {
        if (pending.MaterializedAtUtc.HasValue)
        {
            return true;
        }

        TripPlanId tripPlanId = TripPlanId.Parse(pending.TripPlanId);
        TripActivityEventDocument? existing = await this.FindByOperationAsync(
            tripPlanId,
            pending.OperationKey,
            cancellationToken);
        if (existing is not null)
        {
            await this.MarkMaterializedAsync(pending.Id, cancellationToken);
            return true;
        }

        TripPlanDocument? updatedPlan = await this.plans.FindOneAndUpdateAsync(
            Builders<TripPlanDocument>.Filter.Eq(
                static document => document.Id,
                pending.TripPlanId)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.DeletionState,
                    TripDeletionState.None),
            Builders<TripPlanDocument>.Update.Inc(
                static document => document.AuditSequence,
                1),
            new FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (updatedPlan is null)
        {
            await this.MarkMaterializedAsync(pending.Id, cancellationToken);
            return true;
        }

        TripActivityEventDocument document = new TripActivityEventDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            TripPlanId = pending.TripPlanId,
            ActorMemberId = pending.ActorMemberId,
            ActorRole = pending.ActorRole,
            Kind = pending.Kind,
            OperationKey = pending.OperationKey,
            Sequence = updatedPlan.AuditSequence,
            AffectedCount = pending.AffectedCount,
            CreatedAt = pending.OccurredAtUtc,
            UpdatedAt = pending.OccurredAtUtc,
        };
        try
        {
            await this.activities.InsertOneAsync(document, cancellationToken: cancellationToken);
            await this.RemoveIfTripDeletionStartedAsync(document, cancellationToken);
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            TripActivityEventDocument? replay = await this.FindByOperationAsync(
                tripPlanId,
                pending.OperationKey,
                cancellationToken);
            if (replay is null)
            {
                throw;
            }
        }

        await this.MarkMaterializedAsync(pending.Id, cancellationToken);
        return true;
    }

    private Task MarkMaterializedAsync(
        string outboxId,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = DateTime.UtcNow;
        return this.outbox.UpdateOneAsync(
            Builders<TripActivityOutboxDocument>.Filter.Eq(
                static document => document.Id,
                outboxId)
            & Builders<TripActivityOutboxDocument>.Filter.Eq(
                static document => document.MaterializedAtUtc,
                null),
            Builders<TripActivityOutboxDocument>.Update
                .Set(static document => document.MaterializedAtUtc, nowUtc)
                .Set(static document => document.UpdatedAt, nowUtc),
            cancellationToken: cancellationToken);
    }

    private async Task RemoveIfTripDeletionStartedAsync(
        TripActivityEventDocument document,
        CancellationToken cancellationToken)
    {
        long activeTripCount = await this.plans.CountDocumentsAsync(
            Builders<TripPlanDocument>.Filter.Eq(
                static plan => plan.Id,
                document.TripPlanId)
            & Builders<TripPlanDocument>.Filter.Eq(
                static plan => plan.DeletionState,
                TripDeletionState.None),
            new CountOptions { Limit = 1 },
            cancellationToken);
        if (activeTripCount == 0)
        {
            _ = await this.activities.DeleteOneAsync(
                Builders<TripActivityEventDocument>.Filter.Eq(
                    static activity => activity.Id,
                    document.Id),
                cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<TripActivityEvent>> ListAsync(
        TripPlanId tripPlanId,
        long? beforeSequence,
        int limit,
        CancellationToken cancellationToken)
    {
        if (beforeSequence is < 1 || limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        FilterDefinition<TripActivityEventDocument> filter =
            Builders<TripActivityEventDocument>.Filter.Eq(
                static document => document.TripPlanId,
                tripPlanId.Value);
        if (beforeSequence.HasValue)
        {
            filter &= Builders<TripActivityEventDocument>.Filter.Lt(
                static document => document.Sequence,
                beforeSequence.Value);
        }

        List<TripActivityEventDocument> documents = await this.activities.Find(filter)
            .SortByDescending(static document => document.Sequence)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToArray();
    }

    public static IReadOnlyCollection<CreateIndexModel<TripActivityEventDocument>> BuildIndexes()
    {
        IndexKeysDefinitionBuilder<TripActivityEventDocument> keys =
            Builders<TripActivityEventDocument>.IndexKeys;
        return new List<CreateIndexModel<TripActivityEventDocument>>
        {
            new CreateIndexModel<TripActivityEventDocument>(
                keys.Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.OperationKey),
                new CreateIndexOptions
                {
                    Name = "uq_trip_audit_operation",
                    Unique = true,
                }),
            new CreateIndexModel<TripActivityEventDocument>(
                keys.Ascending(static document => document.TripPlanId)
                    .Descending(static document => document.Sequence),
                new CreateIndexOptions
                {
                    Name = "uq_trip_audit_sequence",
                    Unique = true,
                }),
            new CreateIndexModel<TripActivityEventDocument>(
                keys.Ascending(static document => document.TripPlanId)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "ix_trip_audit_date" }),
        };
    }

    public static IReadOnlyCollection<CreateIndexModel<TripActivityOutboxDocument>> BuildOutboxIndexes()
    {
        IndexKeysDefinitionBuilder<TripActivityOutboxDocument> keys =
            Builders<TripActivityOutboxDocument>.IndexKeys;
        return new List<CreateIndexModel<TripActivityOutboxDocument>>
        {
            new CreateIndexModel<TripActivityOutboxDocument>(
                keys.Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.OperationKey),
                new CreateIndexOptions
                {
                    Name = "uq_trip_audit_outbox_operation",
                    Unique = true,
                }),
            new CreateIndexModel<TripActivityOutboxDocument>(
                keys.Ascending(static document => document.MaterializedAtUtc)
                    .Ascending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "ix_trip_audit_outbox_pending" }),
            new CreateIndexModel<TripActivityOutboxDocument>(
                keys.Ascending(static document => document.MaterializedAtUtc),
                new CreateIndexOptions
                {
                    Name = "ttl_trip_audit_outbox_materialized",
                    ExpireAfter = MaterializedOutboxRetention,
                }),
        };
    }

    private async Task<TripActivityEventDocument?> FindByOperationAsync(
        TripPlanId tripPlanId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        TripActivityEventDocument? document = await this.activities.Find(
                Builders<TripActivityEventDocument>.Filter.Eq(
                    static document => document.TripPlanId,
                    tripPlanId.Value)
                & Builders<TripActivityEventDocument>.Filter.Eq(
                    static document => document.OperationKey,
                    operationKey))
            .FirstOrDefaultAsync(cancellationToken);
        return document;
    }

    private async Task<TripActivityOutboxDocument?> FindOutboxByOperationAsync(
        TripPlanId tripPlanId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        return await this.outbox.Find(
                Builders<TripActivityOutboxDocument>.Filter.Eq(
                    static document => document.TripPlanId,
                    tripPlanId.Value)
                & Builders<TripActivityOutboxDocument>.Filter.Eq(
                    static document => document.OperationKey,
                    operationKey))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool HasSameActivity(
        TripActivityOutboxDocument existing,
        TripActivityWrite requested)
    {
        return string.Equals(existing.TripPlanId, requested.TripPlanId.Value, StringComparison.Ordinal)
            && string.Equals(existing.ActorMemberId, requested.ActorMemberId?.Value, StringComparison.Ordinal)
            && existing.ActorRole == requested.ActorRole
            && existing.Kind == requested.Kind
            && string.Equals(existing.OperationKey, requested.OperationKey, StringComparison.Ordinal)
            && existing.AffectedCount == requested.AffectedCount;
    }

    private static TripActivityEvent ToDomain(TripActivityEventDocument document)
    {
        return new TripActivityEvent(
            document.Id,
            TripPlanId.Parse(document.TripPlanId),
            string.IsNullOrWhiteSpace(document.ActorMemberId)
                ? null
                : TripMemberId.Parse(document.ActorMemberId),
            document.ActorRole,
            document.Kind,
            document.OperationKey,
            document.Sequence,
            document.AffectedCount,
            DateTime.SpecifyKind(document.CreatedAt, DateTimeKind.Utc));
    }
}
