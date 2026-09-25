using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripPreferenceRepository : ITripPreferenceRepository
{
    private readonly IMongoCollection<TripItemPreferenceDocument> collection;
    private readonly IMongoCollection<TripPlanDocument> tripPlans;

    public TripPreferenceRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<TripItemPreferenceDocument>(
            settings.TripItemPreferencesCollectionName);
        this.tripPlans = database.GetCollection<TripPlanDocument>(
            settings.TripPlansCollectionName);
    }

    internal TripPreferenceRepository(
        IMongoCollection<TripItemPreferenceDocument> collection,
        IMongoCollection<TripPlanDocument> tripPlans)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.tripPlans = tripPlans ?? throw new ArgumentNullException(nameof(tripPlans));
    }

    internal static IReadOnlyCollection<CreateIndexModel<TripItemPreferenceDocument>> BuildIndexes()
    {
        FilterDefinitionBuilder<TripItemPreferenceDocument> filters =
            Builders<TripItemPreferenceDocument>.Filter;
        return new List<CreateIndexModel<TripItemPreferenceDocument>>
        {
            new(
                Builders<TripItemPreferenceDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ParkItemId),
                new CreateIndexOptions<TripItemPreferenceDocument>
                {
                    Unique = true,
                    Name = "uq_trip_preference_plan_user_item",
                }),
            new(
                Builders<TripItemPreferenceDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.DocumentState)
                    .Ascending(static document => document.ParkItemId),
                new CreateIndexOptions { Name = "ix_trip_preference_plan_item" }),
            new(
                Builders<TripItemPreferenceDocument>.IndexKeys
                    .Ascending(static document => document.ReservedExpiresAtUtc),
                new CreateIndexOptions<TripItemPreferenceDocument>
                {
                    Name = "ttl_trip_preference_reserved",
                    ExpireAfter = TimeSpan.Zero,
                    PartialFilterExpression = filters.Eq(
                        static document => document.DocumentState,
                        TripChildDocumentState.Reserved),
                }),
            TripActivityPendingMongoDefinitions.BuildPendingIndex<TripItemPreferenceDocument>(
                "ix_trip_preference_pending_audit"),
        };
    }

    public async Task<IReadOnlyCollection<TripPreferenceCount>> SummarizeAsync(
        TripPlanId tripPlanId,
        IReadOnlyCollection<string> activeUserIds,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activeUserIds);
        ArgumentNullException.ThrowIfNull(parkItemIds);
        if (activeUserIds.Count == 0 || parkItemIds.Count == 0)
        {
            return Array.Empty<TripPreferenceCount>();
        }

        PipelineDefinition<TripItemPreferenceDocument, BsonDocument> pipeline = BuildSummaryStages(
            tripPlanId,
            activeUserIds,
            parkItemIds);
        List<BsonDocument> rows = await this.collection.Aggregate(pipeline)
            .ToListAsync(cancellationToken);
        List<TripPreferenceCount> counts = new(rows.Count);
        foreach (BsonDocument row in rows)
        {
            BsonDocument key = row["_id"].AsBsonDocument;
            if (Enum.TryParse(key["level"].AsString, true, out TripItemPreferenceLevel level)
                && Enum.IsDefined(level))
            {
                counts.Add(new TripPreferenceCount(
                    key["parkItemId"].AsString,
                    level,
                    checked((int)row["count"].ToInt64()),
                    row["latestUpdatedAtUtc"].ToUniversalTime()));
            }
        }

        return counts;
    }

    internal static BsonDocument[] BuildSummaryStages(
        TripPlanId tripPlanId,
        IReadOnlyCollection<string> activeUserIds,
        IReadOnlyCollection<string> parkItemIds)
    {
        ArgumentNullException.ThrowIfNull(activeUserIds);
        ArgumentNullException.ThrowIfNull(parkItemIds);
        BsonArray users = new BsonArray(activeUserIds.Select(static value => new BsonString(value)));
        BsonArray items = new BsonArray(parkItemIds.Select(static value => new BsonString(value)));
        return new BsonDocument[]
        {
            new("$match", new BsonDocument
            {
                { "tripPlanId", tripPlanId.Value },
                { "userId", new BsonDocument("$in", users) },
                { "parkItemId", new BsonDocument("$in", items) },
                { "documentState", TripChildDocumentState.Committed.ToString() },
                { "level", new BsonDocument("$ne", TripItemPreferenceLevel.Unknown.ToString()) },
            }),
            new("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "parkItemId", "$parkItemId" },
                        { "level", "$level" },
                    }
                },
                { "count", new BsonDocument("$sum", 1) },
                { "latestUpdatedAtUtc", new BsonDocument("$max", "$updatedAt") },
            }),
        };
    }

    public async Task<IReadOnlyCollection<TripItemPreference>> ListForUserAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<TripItemPreferenceDocument> filters =
            Builders<TripItemPreferenceDocument>.Filter;
        List<TripItemPreferenceDocument> documents = await this.collection.Find(
                filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
                & filters.Eq(static document => document.UserId, normalizedUserId)
                & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed))
            .SortBy(static document => document.ParkItemId)
            .Limit(TripItemPreference.MaximumPreferencesPerMember)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<TripItemPreference?> GetAsync(
        TripPlanId tripPlanId,
        string userId,
        string parkItemId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<TripItemPreferenceDocument> identity = BuildCommittedIdentityFilter(
            tripPlanId,
            IdentifierRules.NormalizeRequired(userId, nameof(userId)),
            IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId)));
        TripItemPreferenceDocument? document = await this.collection.Find(identity)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<TripItemPreferenceWriteResult> CreateAsync(
        TripItemPreference preference,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preference);
        ArgumentNullException.ThrowIfNull(lease);
        TripItemPreferenceDocument shell = new()
        {
            Id = preference.Id.Value,
            TripPlanId = preference.TripPlanId.Value,
            MemberId = preference.MemberId.Value,
            UserId = preference.UserId,
            ParkItemId = preference.ParkItemId,
            DocumentState = TripChildDocumentState.Reserved,
            OperationId = lease.OperationId,
            ChildMutationEpoch = lease.ChildMutationEpoch,
            LeaseGeneration = lease.Generation,
            LeaseExpiresAtUtc = lease.ExpiresAtUtc,
            ReservedExpiresAtUtc = lease.ExpiresAtUtc.AddMinutes(5),
            CreatedAt = preference.CreatedAtUtc,
            UpdatedAt = preference.UpdatedAtUtc,
        };
        try
        {
            await this.collection.InsertOneAsync(shell, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            TripItemPreference? existing = await this.GetAsync(
                preference.TripPlanId,
                preference.UserId,
                preference.ParkItemId,
                cancellationToken);
            return new TripItemPreferenceWriteResult(
                TripChildWriteOutcome.Conflict,
                existing,
                existing?.Version);
        }

        TripItemPreferenceDocument materialized = preference.ToDocument();
        FilterDefinitionBuilder<TripItemPreferenceDocument> filters =
            Builders<TripItemPreferenceDocument>.Filter;
        FilterDefinition<TripItemPreferenceDocument> filter = filters.Eq(
                static document => document.Id,
                preference.Id.Value)
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Reserved)
            & filters.Eq(static document => document.OperationId, lease.OperationId)
            & filters.Eq(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
            & filters.Eq(static document => document.LeaseGeneration, lease.Generation)
            & TripChildMutationMongoDefinitions.BuildCreationLeaseGuard<TripItemPreferenceDocument>();
        UpdateDefinitionBuilder<TripItemPreferenceDocument> updates =
            Builders<TripItemPreferenceDocument>.Update;
        UpdateDefinition<TripItemPreferenceDocument> commitUpdate =
            TripActivityPendingMongoDefinitions.Append(
                updates
                    .Set(static document => document.Level, materialized.Level)
                    .Set(static document => document.Reason, materialized.Reason)
                    .Set(static document => document.Version, materialized.Version)
                    .Set(static document => document.DocumentState, TripChildDocumentState.Committed)
                    .Unset(static document => document.ReservedExpiresAtUtc)
                    .Set(static document => document.CreatedAt, materialized.CreatedAt)
                    .Set(static document => document.UpdatedAt, materialized.UpdatedAt),
                pendingActivity);
        TripItemPreferenceDocument? committed = await this.collection.FindOneAndUpdateAsync(
            filter,
            commitUpdate,
            new FindOneAndUpdateOptions<TripItemPreferenceDocument, TripItemPreferenceDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        return committed is null
            ? new TripItemPreferenceWriteResult(TripChildWriteOutcome.LeaseExpired)
            : new TripItemPreferenceWriteResult(
                TripChildWriteOutcome.Success,
                committed.ToDomain(),
                committed.Version);
    }

    public async Task<TripItemPreferenceWriteResult> ReplaceAsync(
        TripItemPreference preference,
        long expectedVersion,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preference);
        ArgumentNullException.ThrowIfNull(lease);
        FilterDefinitionBuilder<TripItemPreferenceDocument> filters =
            Builders<TripItemPreferenceDocument>.Filter;
        FilterDefinition<TripItemPreferenceDocument> identity = BuildCommittedIdentityFilter(
                preference.TripPlanId,
                preference.UserId,
                preference.ParkItemId)
            & filters.Eq(static document => document.Version, expectedVersion);
        TripItemPreferenceDocument? reserved = await this.collection.FindOneAndUpdateAsync(
            identity & TripChildMutationMongoDefinitions
                .BuildPendingAvailableGuard<TripItemPreferenceDocument>(),
            Builders<TripItemPreferenceDocument>.Update.Set(
                static document => document.PendingMutation,
                TripChildMutationMongoDefinitions.ToPendingDocument(lease)),
            new FindOneAndUpdateOptions<TripItemPreferenceDocument, TripItemPreferenceDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (reserved is null)
        {
            TripItemPreference? existing = await this.GetAsync(
                preference.TripPlanId,
                preference.UserId,
                preference.ParkItemId,
                cancellationToken);
            return new TripItemPreferenceWriteResult(
                existing is null ? TripChildWriteOutcome.NotFound : TripChildWriteOutcome.Conflict,
                existing,
                existing?.Version);
        }

        TripItemPreferenceDocument materialized = preference.ToDocument();
        FilterDefinition<TripItemPreferenceDocument> commitFilter = identity
            & filters.Eq("pendingMutation.operationId", lease.OperationId)
            & filters.Eq("pendingMutation.childMutationEpoch", lease.ChildMutationEpoch)
            & filters.Eq("pendingMutation.leaseGeneration", lease.Generation)
            & TripChildMutationMongoDefinitions.BuildPendingLeaseGuard<TripItemPreferenceDocument>();
        UpdateDefinition<TripItemPreferenceDocument> commitUpdate =
            TripActivityPendingMongoDefinitions.Append(
                Builders<TripItemPreferenceDocument>.Update
                    .Set(static document => document.Level, materialized.Level)
                    .Set(static document => document.Reason, materialized.Reason)
                    .Set(static document => document.Version, materialized.Version)
                    .Set(static document => document.UpdatedAt, materialized.UpdatedAt)
                    .Unset(static document => document.PendingMutation),
                pendingActivity);
        TripItemPreferenceDocument? committed = await this.collection.FindOneAndUpdateAsync(
            commitFilter,
            commitUpdate,
            new FindOneAndUpdateOptions<TripItemPreferenceDocument, TripItemPreferenceDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        return committed is null
            ? new TripItemPreferenceWriteResult(TripChildWriteOutcome.LeaseExpired)
            : new TripItemPreferenceWriteResult(
                TripChildWriteOutcome.Success,
                committed.ToDomain(),
                committed.Version);
    }

    public async Task CompleteDepartureCleanupAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<TripItemPreferenceDocument> filters =
            Builders<TripItemPreferenceDocument>.Filter;
        FilterDefinition<TripItemPreferenceDocument> departingPreferences =
            filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
            & filters.Eq(static document => document.UserId, normalizedUserId);
        FilterDefinition<TripItemPreferenceDocument> pendingAuditFilter =
            new BsonDocumentFilterDefinition<TripItemPreferenceDocument>(
                new BsonDocument(
                    "pendingAuditEvents.0",
                    new BsonDocument("$exists", true)));
        List<List<TripActivityPendingDocument>> pendingSets = await this.collection
            .Find(departingPreferences & pendingAuditFilter)
            .Project(static document => document.PendingAuditEvents)
            .ToListAsync(cancellationToken);
        List<TripActivityPendingDocument> pendingActivities = pendingSets
            .SelectMany(static pending => pending)
            .ToList();
        if (pendingActivities.Count > 0)
        {
            UpdateResult relocation = await this.tripPlans.UpdateOneAsync(
                Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.Id,
                    tripPlanId.Value)
                & Builders<TripPlanDocument>.Filter.Eq(
                    static document => document.DeletionState,
                    TripDeletionState.None),
                BuildDepartureAuditRelocation(pendingActivities),
                cancellationToken: cancellationToken);
            if (relocation.MatchedCount != 1)
            {
                return;
            }
        }

        DeleteResult result = await this.collection.DeleteManyAsync(
            departingPreferences & BuildDepartureSafeDeletionFilter(
                pendingActivities.Select(static activity => activity.MarkerId).ToArray()),
            cancellationToken);
        _ = result.DeletedCount;
        long remaining = await this.collection.CountDocumentsAsync(
            departingPreferences,
            new CountOptions { Limit = 1 },
            cancellationToken);
        if (remaining > 0)
        {
            return;
        }

        UpdateResult planResult = await this.tripPlans.UpdateOneAsync(
            Builders<TripPlanDocument>.Filter.Eq(
                static document => document.Id,
                tripPlanId.Value)
            & Builders<TripPlanDocument>.Filter.AnyEq(
                static document => document.DepartedPreferenceCleanupUserIds,
                normalizedUserId),
            Builders<TripPlanDocument>.Update.Pull(
                static document => document.DepartedPreferenceCleanupUserIds,
                normalizedUserId),
            cancellationToken: cancellationToken);
        _ = planResult.ModifiedCount;
    }

    internal static UpdateDefinition<TripPlanDocument> BuildDepartureAuditRelocation(
        IReadOnlyCollection<TripActivityPendingDocument> pendingActivities)
    {
        ArgumentNullException.ThrowIfNull(pendingActivities);
        if (pendingActivities.Count == 0)
        {
            throw new ArgumentException(
                "At least one pending activity is required.",
                nameof(pendingActivities));
        }

        return Builders<TripPlanDocument>.Update.AddToSetEach(
            static document => document.PendingAuditEvents,
            pendingActivities);
    }

    internal static FilterDefinition<TripItemPreferenceDocument> BuildDepartureSafeDeletionFilter(
        IReadOnlyCollection<string> relocatedMarkerIds)
    {
        ArgumentNullException.ThrowIfNull(relocatedMarkerIds);
        BsonArray markerIds = new();
        foreach (string markerId in relocatedMarkerIds.Distinct(StringComparer.Ordinal))
        {
            markerIds.Add(markerId);
        }

        return markerIds.Count == 0
            ? new BsonDocumentFilterDefinition<TripItemPreferenceDocument>(
                new BsonDocument(
                    "pendingAuditEvents.0",
                    new BsonDocument("$exists", false)))
            : new BsonDocumentFilterDefinition<TripItemPreferenceDocument>(
                new BsonDocument(
                    "pendingAuditEvents",
                    new BsonDocument("$not", new BsonDocument(
                        "$elemMatch",
                        new BsonDocument(
                            "markerId",
                            new BsonDocument("$nin", markerIds))))));
    }

    public async Task<int> ReconcileDepartureCleanupAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        FilterDefinition<TripPlanDocument> pendingFilter =
            new BsonDocumentFilterDefinition<TripPlanDocument>(
                new BsonDocument(
                    "departedPreferenceCleanupUserIds.0",
                    new BsonDocument("$exists", true)));
        List<TripPlanDocument> pendingPlans = await this.tripPlans.Find(pendingFilter)
            .SortBy(static document => document.UpdatedAt)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        int completed = 0;
        foreach (TripPlanDocument plan in pendingPlans)
        {
            foreach (string userId in plan.DepartedPreferenceCleanupUserIds
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await this.CompleteDepartureCleanupAsync(
                    TripPlanId.Parse(plan.Id),
                    userId,
                    cancellationToken);
                completed++;
                if (completed >= limit)
                {
                    return completed;
                }
            }
        }

        return completed;
    }

    private static FilterDefinition<TripItemPreferenceDocument> BuildCommittedIdentityFilter(
        TripPlanId tripPlanId,
        string userId,
        string parkItemId)
    {
        FilterDefinitionBuilder<TripItemPreferenceDocument> filters =
            Builders<TripItemPreferenceDocument>.Filter;
        return filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
            & filters.Eq(static document => document.UserId, userId)
            & filters.Eq(static document => document.ParkItemId, parkItemId)
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed);
    }
}
