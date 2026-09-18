using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripPreferenceRepository : ITripPreferenceRepository
{
    private readonly IMongoCollection<TripItemPreferenceDocument> collection;

    public TripPreferenceRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<TripItemPreferenceDocument>(
            settings.TripItemPreferencesCollectionName);
    }

    internal TripPreferenceRepository(IMongoCollection<TripItemPreferenceDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
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
        TripItemPreferenceDocument? committed = await this.collection.FindOneAndUpdateAsync(
            filter,
            updates
                .Set(static document => document.Level, materialized.Level)
                .Set(static document => document.Reason, materialized.Reason)
                .Set(static document => document.Version, materialized.Version)
                .Set(static document => document.DocumentState, TripChildDocumentState.Committed)
                .Unset(static document => document.ReservedExpiresAtUtc)
                .Set(static document => document.CreatedAt, materialized.CreatedAt)
                .Set(static document => document.UpdatedAt, materialized.UpdatedAt),
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
        TripItemPreferenceDocument? committed = await this.collection.FindOneAndUpdateAsync(
            commitFilter,
            Builders<TripItemPreferenceDocument>.Update
                .Set(static document => document.Level, materialized.Level)
                .Set(static document => document.Reason, materialized.Reason)
                .Set(static document => document.Version, materialized.Version)
                .Set(static document => document.UpdatedAt, materialized.UpdatedAt)
                .Unset(static document => document.PendingMutation),
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

    public async Task DeleteForUserAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<TripItemPreferenceDocument> filters =
            Builders<TripItemPreferenceDocument>.Filter;
        DeleteResult result = await this.collection.DeleteManyAsync(
            filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
            & filters.Eq(static document => document.UserId, normalizedUserId),
            cancellationToken);
        _ = result.DeletedCount;
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
