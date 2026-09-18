using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripAuditRepository : ITripAuditWriter, ITripAuditReader, ITripAuditReconciler
{
    public const int MaximumReconciliationBatchSize = 50;

    private readonly IMongoCollection<TripPlanDocument> plans;
    private readonly IMongoCollection<TripParkCandidateDocument> candidates;
    private readonly IMongoCollection<TripDayPlanDocument> days;
    private readonly IMongoCollection<TripInvitationDocument> invitations;
    private readonly IMongoCollection<TripItemPreferenceDocument> preferences;
    private readonly IMongoCollection<TripItemDecisionDocument> decisions;
    private readonly IMongoCollection<TripActivityEventDocument> activities;
    private readonly ILogger<TripAuditRepository> logger;

    public TripAuditRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        ILogger<TripAuditRepository> logger)
        : this(
            database.GetCollection<TripPlanDocument>(settings.TripPlansCollectionName),
            database.GetCollection<TripParkCandidateDocument>(settings.TripParkCandidatesCollectionName),
            database.GetCollection<TripDayPlanDocument>(settings.TripDayPlansCollectionName),
            database.GetCollection<TripInvitationDocument>(settings.TripInvitationsCollectionName),
            database.GetCollection<TripItemPreferenceDocument>(settings.TripItemPreferencesCollectionName),
            database.GetCollection<TripItemDecisionDocument>(settings.TripItemDecisionsCollectionName),
            database.GetCollection<TripActivityEventDocument>(settings.TripAuditEventsCollectionName),
            logger)
    {
    }

    internal TripAuditRepository(
        IMongoCollection<TripPlanDocument> plans,
        IMongoCollection<TripParkCandidateDocument> candidates,
        IMongoCollection<TripDayPlanDocument> days,
        IMongoCollection<TripInvitationDocument> invitations,
        IMongoCollection<TripItemPreferenceDocument> preferences,
        IMongoCollection<TripItemDecisionDocument> decisions,
        IMongoCollection<TripActivityEventDocument> activities,
        ILogger<TripAuditRepository>? logger = null)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        this.days = days ?? throw new ArgumentNullException(nameof(days));
        this.invitations = invitations ?? throw new ArgumentNullException(nameof(invitations));
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.decisions = decisions ?? throw new ArgumentNullException(nameof(decisions));
        this.activities = activities ?? throw new ArgumentNullException(nameof(activities));
        this.logger = logger ?? NullLogger<TripAuditRepository>.Instance;
    }

    public async Task<bool> AppendAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        try
        {
            TripActivityEventDocument? existing = await this.FindByOperationAsync(
                activity.TripPlanId,
                activity.OperationKey,
                cancellationToken);
            if (existing is not null)
            {
                await this.RemovePendingMarkersAsync(
                    activity.TripPlanId,
                    activity.OperationKey,
                    cancellationToken);
                return true;
            }

            if (!await this.HasPendingMarkerAsync(activity, cancellationToken))
            {
                this.logger.LogWarning(
                    "Trip activity {OperationKey} has no durable source marker and will not be invented.",
                    activity.OperationKey);
                return false;
            }

            return await this.MaterializeAsync(activity, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Unable to materialize trip activity {OperationKey}; its source marker will retry.",
                activity.OperationKey);
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

        IReadOnlyCollection<TripActivityWrite> pending = await this.LoadPendingAsync(
            maximumCount,
            cancellationToken);
        int reconciledCount = 0;
        foreach (TripActivityWrite activity in pending)
        {
            try
            {
                if (await this.MaterializeAsync(activity, cancellationToken))
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
                    "Unable to reconcile trip activity {OperationKey}; its source marker remains pending.",
                    activity.OperationKey);
            }
        }

        return reconciledCount;
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
            new(
                keys.Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.OperationKey),
                new CreateIndexOptions { Name = "uq_trip_audit_operation", Unique = true }),
            new(
                keys.Ascending(static document => document.TripPlanId)
                    .Descending(static document => document.Sequence),
                new CreateIndexOptions { Name = "uq_trip_audit_sequence", Unique = true }),
            new(
                keys.Ascending(static document => document.TripPlanId)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "ix_trip_audit_date" }),
        };
    }

    private async Task<bool> MaterializeAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        TripActivityEventDocument? existing = await this.FindByOperationAsync(
            activity.TripPlanId,
            activity.OperationKey,
            cancellationToken);
        if (existing is not null)
        {
            await this.RemovePendingMarkersAsync(
                activity.TripPlanId,
                activity.OperationKey,
                cancellationToken);
            return true;
        }

        TripPlanDocument? updatedPlan = await this.plans.FindOneAndUpdateAsync(
            Builders<TripPlanDocument>.Filter.Eq(
                static document => document.Id,
                activity.TripPlanId.Value)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.DeletionState,
                    TripDeletionState.None),
            Builders<TripPlanDocument>.Update.Inc(static document => document.AuditSequence, 1),
            new FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (updatedPlan is null)
        {
            await this.RemovePendingMarkersAsync(
                activity.TripPlanId,
                activity.OperationKey,
                cancellationToken);
            return true;
        }

        TripActivityEventDocument document = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            TripPlanId = activity.TripPlanId.Value,
            ActorMemberId = activity.ActorMemberId?.Value,
            ActorRole = activity.ActorRole,
            Kind = activity.Kind,
            OperationKey = activity.OperationKey,
            Sequence = updatedPlan.AuditSequence,
            AffectedCount = activity.AffectedCount,
            CreatedAt = activity.OccurredAtUtc,
            UpdatedAt = activity.OccurredAtUtc,
        };
        try
        {
            await this.activities.InsertOneAsync(document, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            TripActivityEventDocument? replay = await this.FindByOperationAsync(
                activity.TripPlanId,
                activity.OperationKey,
                cancellationToken);
            if (replay is null)
            {
                throw;
            }
        }

        if (!await this.IsTripActiveAsync(activity.TripPlanId, cancellationToken))
        {
            _ = await this.activities.DeleteManyAsync(
                Builders<TripActivityEventDocument>.Filter.Eq(
                    static item => item.TripPlanId,
                    activity.TripPlanId.Value),
                cancellationToken);
        }

        await this.RemovePendingMarkersAsync(
            activity.TripPlanId,
            activity.OperationKey,
            cancellationToken);
        return true;
    }

    private async Task<IReadOnlyCollection<TripActivityWrite>> LoadPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken)
    {
        List<TripActivityPendingDocument> markers = new();
        await AddPendingAsync(this.plans, markers, maximumCount, cancellationToken);
        await AddPendingAsync(this.candidates, markers, maximumCount, cancellationToken);
        await AddPendingAsync(this.days, markers, maximumCount, cancellationToken);
        await AddPendingAsync(this.invitations, markers, maximumCount, cancellationToken);
        await AddPendingAsync(this.preferences, markers, maximumCount, cancellationToken);
        await AddPendingAsync(this.decisions, markers, maximumCount, cancellationToken);

        return markers
            .GroupBy(
                static marker => $"{marker.TripPlanId}\n{marker.OperationKey}",
                StringComparer.Ordinal)
            .OrderBy(static group => group.Min(static marker => marker.OccurredAtUtc))
            .Take(maximumCount)
            .Select(static group => BuildReconciledWrite(group.ToArray()))
            .ToArray();
    }

    private static async Task AddPendingAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        List<TripActivityPendingDocument> destination,
        int limit,
        CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> untyped = collection.Database.GetCollection<BsonDocument>(
            collection.CollectionNamespace.CollectionName);
        BsonDocument filter = new("pendingAuditEvents.0", new BsonDocument("$exists", true));
        List<BsonDocument> projected = await untyped.Find(filter)
            .Project(new BsonDocument("pendingAuditEvents", 1))
            .Limit(limit)
            .ToListAsync(cancellationToken);
        foreach (BsonDocument document in projected)
        {
            if (!document.TryGetValue("pendingAuditEvents", out BsonValue? value)
                || !value.IsBsonArray)
            {
                continue;
            }

            foreach (BsonValue marker in value.AsBsonArray)
            {
                destination.Add(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<TripActivityPendingDocument>(
                    marker.AsBsonDocument));
            }
        }
    }

    private async Task<bool> HasPendingMarkerAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        BsonDocument filter = BuildPendingMarkerFilter(
            activity.TripPlanId,
            activity.OperationKey,
            activity.Kind);
        return await HasPendingAsync(this.plans, filter, cancellationToken)
            || await HasPendingAsync(this.candidates, filter, cancellationToken)
            || await HasPendingAsync(this.days, filter, cancellationToken)
            || await HasPendingAsync(this.invitations, filter, cancellationToken)
            || await HasPendingAsync(this.preferences, filter, cancellationToken)
            || await HasPendingAsync(this.decisions, filter, cancellationToken);
    }

    private static async Task<bool> HasPendingAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        BsonDocument filter,
        CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> untyped = collection.Database.GetCollection<BsonDocument>(
            collection.CollectionNamespace.CollectionName);
        return await untyped.Find(filter).Limit(1).AnyAsync(cancellationToken);
    }

    private async Task RemovePendingMarkersAsync(
        TripPlanId tripPlanId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        BsonDocument filter = BuildPendingMarkerFilter(tripPlanId, operationKey, null);
        BsonDocument update = new("$pull", new BsonDocument(
            "pendingAuditEvents",
            new BsonDocument("operationKey", operationKey)));
        await RemovePendingAsync(this.plans, filter, update, cancellationToken);
        await RemovePendingAsync(this.candidates, filter, update, cancellationToken);
        await RemovePendingAsync(this.days, filter, update, cancellationToken);
        await RemovePendingAsync(this.invitations, filter, update, cancellationToken);
        await RemovePendingAsync(this.preferences, filter, update, cancellationToken);
        await RemovePendingAsync(this.decisions, filter, update, cancellationToken);

        BsonDocument tombstoneFilter = new("$and", new BsonArray
        {
            new BsonDocument("tripPlanId", tripPlanId.Value),
            new BsonDocument("documentState", TripChildDocumentState.Deleted.ToString()),
            new BsonDocument("pendingAuditEvents.0", new BsonDocument("$exists", false)),
        });
        _ = await this.days.Database.GetCollection<BsonDocument>(
                this.days.CollectionNamespace.CollectionName)
            .DeleteManyAsync(tombstoneFilter, cancellationToken);
    }

    private static async Task RemovePendingAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        BsonDocument filter,
        BsonDocument update,
        CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> untyped = collection.Database.GetCollection<BsonDocument>(
            collection.CollectionNamespace.CollectionName);
        _ = await untyped.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    private Task<bool> IsTripActiveAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        return this.plans.Find(
                Builders<TripPlanDocument>.Filter.Eq(static item => item.Id, tripPlanId.Value)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static item => item.DeletionState,
                    TripDeletionState.None))
            .Limit(1)
            .AnyAsync(cancellationToken);
    }

    private async Task<TripActivityEventDocument?> FindByOperationAsync(
        TripPlanId tripPlanId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        return await this.activities.Find(
                Builders<TripActivityEventDocument>.Filter.Eq(
                    static document => document.TripPlanId,
                    tripPlanId.Value)
                & Builders<TripActivityEventDocument>.Filter.Eq(
                    static document => document.OperationKey,
                    operationKey))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static BsonDocument BuildPendingMarkerFilter(
        TripPlanId tripPlanId,
        string operationKey,
        TripActivityKind? kind)
    {
        BsonDocument element = new()
        {
            { "tripPlanId", tripPlanId.Value },
            { "operationKey", operationKey },
        };
        if (kind.HasValue)
        {
            element.Add("kind", kind.Value.ToString());
        }

        return new BsonDocument("pendingAuditEvents", new BsonDocument("$elemMatch", element));
    }

    private static TripActivityWrite BuildReconciledWrite(
        IReadOnlyCollection<TripActivityPendingDocument> markers)
    {
        TripActivityPendingDocument first = markers.OrderBy(static marker => marker.OccurredAtUtc).First();
        TripActivityWrite canonical = first.ToWrite();
        int affectedCount = canonical.Kind == TripActivityKind.PreferencesUpdated
            ? markers.Count
            : canonical.AffectedCount;
        return canonical with { AffectedCount = affectedCount };
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
