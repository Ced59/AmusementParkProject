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
                await this.EnrichExistingActorAsync(existing, activity, cancellationToken);
                await this.RemovePendingMarkersAsync(
                    activity.TripPlanId,
                    activity.OperationKey,
                    cancellationToken);
                return true;
            }

            IReadOnlyCollection<TripActivityPendingDocument> markers =
                await this.LoadPendingOperationAsync(
                    activity.TripPlanId,
                    activity.OperationKey,
                    cancellationToken);
            if (markers.Count == 0)
            {
                this.logger.LogWarning(
                    "Trip activity {OperationKey} has no durable source marker and will not be invented.",
                    activity.OperationKey);
                return false;
            }

            TripActivityWrite canonical = EnrichCanonicalWrite(
                BuildReconciledWrite(markers),
                activity);
            return await this.MaterializeAsync(canonical, cancellationToken);
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

    private async Task EnrichExistingActorAsync(
        TripActivityEventDocument existing,
        TripActivityWrite requested,
        CancellationToken cancellationToken)
    {
        if (!CanEnrichExistingActor(existing, requested))
        {
            return;
        }

        FilterDefinitionBuilder<TripActivityEventDocument> filters =
            Builders<TripActivityEventDocument>.Filter;
        _ = await this.activities.UpdateOneAsync(
            filters.Eq(static document => document.Id, existing.Id)
                & filters.Eq(static document => document.TripPlanId, requested.TripPlanId.Value)
                & filters.Eq(static document => document.OperationKey, requested.OperationKey)
                & filters.Eq(static document => document.Kind, TripActivityKind.InvitationAccepted)
                & filters.Eq(static document => document.ActorMemberId, null)
                & filters.Eq(static document => document.ActorRole, null),
            Builders<TripActivityEventDocument>.Update
                .Set(static document => document.ActorMemberId, requested.ActorMemberId!.Value.Value)
                .Set(static document => document.ActorRole, requested.ActorRole!.Value),
            cancellationToken: cancellationToken);
    }

    internal static bool CanEnrichExistingActor(
        TripActivityEventDocument existing,
        TripActivityWrite requested)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(requested);
        return existing.Kind == TripActivityKind.InvitationAccepted
            && requested.Kind == TripActivityKind.InvitationAccepted
            && string.Equals(existing.TripPlanId, requested.TripPlanId.Value, StringComparison.Ordinal)
            && string.Equals(existing.OperationKey, requested.OperationKey, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(existing.ActorMemberId)
            && !existing.ActorRole.HasValue
            && requested.ActorMemberId.HasValue
            && requested.ActorRole.HasValue;
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

        TripActivityEventDocument? cursor = null;
        if (beforeSequence.HasValue)
        {
            cursor = await this.activities.Find(
                    Builders<TripActivityEventDocument>.Filter.Eq(
                        static document => document.TripPlanId,
                        tripPlanId.Value)
                    & Builders<TripActivityEventDocument>.Filter.Eq(
                        static document => document.Sequence,
                        beforeSequence.Value))
                .FirstOrDefaultAsync(cancellationToken);
            if (cursor is null)
            {
                return Array.Empty<TripActivityEvent>();
            }
        }

        List<TripActivityEventDocument> documents = await this.activities
            .Find(BuildPageFilter(tripPlanId, cursor))
            .Sort(BuildPageSort())
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
                    .Descending(static document => document.CreatedAt)
                    .Descending(static document => document.Sequence),
                new CreateIndexOptions { Name = "ix_trip_audit_date" }),
        };
    }

    internal static FilterDefinition<TripActivityEventDocument> BuildPageFilter(
        TripPlanId tripPlanId,
        TripActivityEventDocument? cursor)
    {
        FilterDefinitionBuilder<TripActivityEventDocument> filters =
            Builders<TripActivityEventDocument>.Filter;
        FilterDefinition<TripActivityEventDocument> trip = filters.Eq(
            static document => document.TripPlanId,
            tripPlanId.Value);
        if (cursor is null)
        {
            return trip;
        }

        return trip
            & (filters.Lt(static document => document.CreatedAt, cursor.CreatedAt)
                | (filters.Eq(static document => document.CreatedAt, cursor.CreatedAt)
                    & filters.Lt(static document => document.Sequence, cursor.Sequence)));
    }

    internal static SortDefinition<TripActivityEventDocument> BuildPageSort()
    {
        return Builders<TripActivityEventDocument>.Sort
            .Descending(static document => document.CreatedAt)
            .Descending(static document => document.Sequence);
    }

    private async Task<bool> MaterializeAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        if (await this.IsActivityInFlightAsync(activity, cancellationToken))
        {
            return false;
        }

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

    private Task<bool> IsActivityInFlightAsync(
        TripActivityWrite activity,
        CancellationToken cancellationToken)
    {
        FilterDefinition<TripPlanDocument>? filter = BuildInFlightActivityFilter(activity);
        return filter is null
            ? Task.FromResult(false)
            : this.plans.Find(filter).Limit(1).AnyAsync(cancellationToken);
    }

    internal static FilterDefinition<TripPlanDocument>? BuildInFlightActivityFilter(
        TripActivityWrite activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (string.IsNullOrWhiteSpace(activity.ChildLeaseOperationId)
            || !activity.ChildLeaseEpoch.HasValue
            || !activity.ChildLeaseGeneration.HasValue)
        {
            return null;
        }

        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        return filters.Eq(static document => document.Id, activity.TripPlanId.Value)
            & new BsonDocumentFilterDefinition<TripPlanDocument>(
                new BsonDocument("$expr", new BsonDocument(
                    "$anyElementTrue",
                    new BsonDocument("$map", new BsonDocument
                    {
                        {
                            "input",
                            new BsonDocument("$ifNull", new BsonArray
                            {
                                "$activeChildMutationLeases",
                                new BsonArray(),
                            })
                        },
                        { "as", "lease" },
                        {
                            "in",
                            new BsonDocument("$and", new BsonArray
                            {
                                new BsonDocument("$eq", new BsonArray
                                {
                                    "$$lease.operationId",
                                    activity.ChildLeaseOperationId,
                                }),
                                new BsonDocument("$eq", new BsonArray
                                {
                                    "$$lease.childMutationEpoch",
                                    activity.ChildLeaseEpoch.Value,
                                }),
                                new BsonDocument("$eq", new BsonArray
                                {
                                    "$$lease.generation",
                                    activity.ChildLeaseGeneration.Value,
                                }),
                                new BsonDocument("$gt", new BsonArray
                                {
                                    "$$lease.expiresAtUtc",
                                    "$$NOW",
                                }),
                            })
                        },
                    }))));
    }

    private async Task<IReadOnlyCollection<TripActivityWrite>> LoadPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken)
    {
        List<TripActivityPendingDocument> candidates = new();
        await AddPendingCandidatesAsync(this.plans, candidates, maximumCount, cancellationToken);
        await AddPendingCandidatesAsync(this.candidates, candidates, maximumCount, cancellationToken);
        await AddPendingCandidatesAsync(this.days, candidates, maximumCount, cancellationToken);
        await AddPendingCandidatesAsync(this.invitations, candidates, maximumCount, cancellationToken);
        await AddPendingCandidatesAsync(this.preferences, candidates, maximumCount, cancellationToken);
        await AddPendingCandidatesAsync(this.decisions, candidates, maximumCount, cancellationToken);

        string[] selectedGroupKeys = candidates
            .GroupBy(
                static marker => BuildPendingGroupKey(marker),
                StringComparer.Ordinal)
            .OrderBy(static group => group.Min(static marker => marker.OccurredAtUtc))
            .Take(maximumCount)
            .Select(static group => group.Key)
            .ToArray();
        if (selectedGroupKeys.Length == 0)
        {
            return Array.Empty<TripActivityWrite>();
        }

        HashSet<string> selectedGroups = new(selectedGroupKeys, StringComparer.Ordinal);
        string[] operationKeys = candidates
            .Where(marker => selectedGroups.Contains(BuildPendingGroupKey(marker)))
            .Select(static marker => marker.OperationKey)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        List<TripActivityPendingDocument> completeMarkers = new();
        await AddPendingOperationsAsync(this.plans, completeMarkers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.candidates, completeMarkers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.days, completeMarkers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.invitations, completeMarkers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.preferences, completeMarkers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.decisions, completeMarkers, operationKeys, selectedGroups, cancellationToken);

        return completeMarkers
            .GroupBy(
                static marker => BuildPendingGroupKey(marker),
                StringComparer.Ordinal)
            .OrderBy(static group => group.Min(static marker => marker.OccurredAtUtc))
            .Select(static group => BuildReconciledWrite(group.ToArray()))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<TripActivityPendingDocument>> LoadPendingOperationAsync(
        TripPlanId tripPlanId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        HashSet<string> selectedGroups = new(StringComparer.Ordinal)
        {
            BuildPendingGroupKey(tripPlanId.Value, operationKey),
        };
        List<TripActivityPendingDocument> markers = new();
        string[] operationKeys = { operationKey };
        await AddPendingOperationsAsync(this.plans, markers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.candidates, markers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.days, markers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.invitations, markers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.preferences, markers, operationKeys, selectedGroups, cancellationToken);
        await AddPendingOperationsAsync(this.decisions, markers, operationKeys, selectedGroups, cancellationToken);
        return markers;
    }

    private static async Task AddPendingCandidatesAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        List<TripActivityPendingDocument> destination,
        int limit,
        CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> untyped = collection.Database.GetCollection<BsonDocument>(
            collection.CollectionNamespace.CollectionName);
        BsonDocument filter = new("pendingAuditEvents.0", new BsonDocument("$exists", true));
        List<BsonDocument> projected = await untyped.Find(filter)
            .Project(new BsonDocument(
                "pendingAuditEvents",
                new BsonDocument("$slice", limit)))
            .Limit(limit)
            .ToListAsync(cancellationToken);
        AddDeserializedMarkers(projected, destination, null);
    }

    private static async Task AddPendingOperationsAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        List<TripActivityPendingDocument> destination,
        IReadOnlyCollection<string> operationKeys,
        HashSet<string> selectedGroups,
        CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> untyped = collection.Database.GetCollection<BsonDocument>(
            collection.CollectionNamespace.CollectionName);
        List<BsonDocument> projected = await untyped.Aggregate<BsonDocument>(
                BuildPendingOperationPipeline(operationKeys))
            .ToListAsync(cancellationToken);
        AddDeserializedMarkers(projected, destination, selectedGroups);
    }

    private static void AddDeserializedMarkers(
        IEnumerable<BsonDocument> documents,
        List<TripActivityPendingDocument> destination,
        HashSet<string>? selectedGroups)
    {
        foreach (BsonDocument document in documents)
        {
            if (!document.TryGetValue("pendingAuditEvents", out BsonValue? value)
                || !value.IsBsonArray)
            {
                continue;
            }

            foreach (BsonValue marker in value.AsBsonArray)
            {
                TripActivityPendingDocument pending =
                    MongoDB.Bson.Serialization.BsonSerializer.Deserialize<TripActivityPendingDocument>(
                        marker.AsBsonDocument);
                if (selectedGroups is null
                    || selectedGroups.Contains(BuildPendingGroupKey(pending)))
                {
                    destination.Add(pending);
                }
            }
        }
    }

    internal static BsonDocument[] BuildPendingOperationPipeline(
        IReadOnlyCollection<string> operationKeys)
    {
        ArgumentNullException.ThrowIfNull(operationKeys);
        if (operationKeys.Count == 0)
        {
            throw new ArgumentException("At least one operation key is required.", nameof(operationKeys));
        }

        BsonArray keys = new();
        foreach (string operationKey in operationKeys)
        {
            keys.Add(operationKey);
        }

        return new BsonDocument[]
        {
            new("$match", new BsonDocument(
                "pendingAuditEvents.operationKey",
                new BsonDocument("$in", keys))),
            new("$project", new BsonDocument(
                "pendingAuditEvents",
                new BsonDocument("$filter", new BsonDocument
                {
                    { "input", "$pendingAuditEvents" },
                    { "as", "pending" },
                    {
                        "cond",
                        new BsonDocument("$in", new BsonArray
                        {
                            "$$pending.operationKey",
                            keys,
                        })
                    },
                }))),
        };
    }

    private async Task RemovePendingMarkersAsync(
        TripPlanId tripPlanId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        BsonDocument filter = BuildPendingMarkerFilter(tripPlanId, operationKey);
        BsonDocument update = new("$pull", new BsonDocument(
            "pendingAuditEvents",
            new BsonDocument("operationKey", operationKey)));
        await RemovePendingAsync(this.plans, filter, update, cancellationToken);
        await RemovePendingAsync(this.candidates, filter, update, cancellationToken);
        await RemovePendingAsync(this.days, filter, update, cancellationToken);
        await RemovePendingAsync(this.invitations, filter, update, cancellationToken);
        await RemovePendingAsync(this.preferences, filter, update, cancellationToken);
        await RemovePendingAsync(this.decisions, filter, update, cancellationToken);

        FilterDefinitionBuilder<TripParkCandidateDocument> candidateFilters =
            Builders<TripParkCandidateDocument>.Filter;
        FilterDefinition<TripParkCandidateDocument> candidateTombstone =
            candidateFilters.Eq(static document => document.TripPlanId, tripPlanId.Value)
            & candidateFilters.Eq(
                static document => document.DocumentState,
                TripChildDocumentState.Deleted)
            & candidateFilters.Eq(
                static document => document.TombstoneExpiresAtUtc,
                null)
            & new BsonDocumentFilterDefinition<TripParkCandidateDocument>(
                new BsonDocument(
                    "pendingAuditEvents.0",
                    new BsonDocument("$exists", false)));
        _ = await this.candidates.UpdateManyAsync(
            candidateTombstone,
            Builders<TripParkCandidateDocument>.Update.Set(
                static document => document.TombstoneExpiresAtUtc,
                DateTime.UtcNow.Add(TripParkCandidate.CreationReplayRetention)),
            cancellationToken: cancellationToken);

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
        string operationKey)
    {
        BsonDocument element = new()
        {
            { "tripPlanId", tripPlanId.Value },
            { "operationKey", operationKey },
        };

        return new BsonDocument("pendingAuditEvents", new BsonDocument("$elemMatch", element));
    }

    internal static TripActivityWrite BuildReconciledWrite(
        IReadOnlyCollection<TripActivityPendingDocument> markers)
    {
        TripActivityPendingDocument first = markers.OrderBy(static marker => marker.OccurredAtUtc).First();
        TripActivityWrite canonical = first.ToWrite();
        int affectedCount = canonical.Kind == TripActivityKind.PreferencesUpdated
            ? markers
                .Where(static marker => !string.IsNullOrWhiteSpace(marker.MarkerId))
                .Select(static marker => marker.MarkerId)
                .Distinct(StringComparer.Ordinal)
                .Count()
                + markers.Count(static marker => string.IsNullOrWhiteSpace(marker.MarkerId))
            : canonical.AffectedCount;
        return canonical with { AffectedCount = affectedCount };
    }

    internal static TripActivityWrite EnrichCanonicalWrite(
        TripActivityWrite canonical,
        TripActivityWrite requested)
    {
        ArgumentNullException.ThrowIfNull(canonical);
        ArgumentNullException.ThrowIfNull(requested);
        if (canonical.TripPlanId != requested.TripPlanId
            || !string.Equals(canonical.OperationKey, requested.OperationKey, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The requested activity must target the canonical operation.",
                nameof(requested));
        }

        return canonical.ActorMemberId.HasValue
            || !requested.ActorMemberId.HasValue
            || !requested.ActorRole.HasValue
            ? canonical
            : canonical with
            {
                ActorMemberId = requested.ActorMemberId,
                ActorRole = requested.ActorRole,
            };
    }

    private static string BuildPendingGroupKey(TripActivityPendingDocument marker)
    {
        return BuildPendingGroupKey(marker.TripPlanId, marker.OperationKey);
    }

    private static string BuildPendingGroupKey(string tripPlanId, string operationKey)
    {
        return $"{tripPlanId}\n{operationKey}";
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
