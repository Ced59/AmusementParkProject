using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripItemDecisionRepository : ITripItemDecisionRepository
{
    private readonly IMongoCollection<TripItemDecisionDocument> collection;

    public TripItemDecisionRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<TripItemDecisionDocument>(
            settings.TripItemDecisionsCollectionName);
    }

    internal static IReadOnlyCollection<CreateIndexModel<TripItemDecisionDocument>> BuildIndexes()
    {
        FilterDefinitionBuilder<TripItemDecisionDocument> filters =
            Builders<TripItemDecisionDocument>.Filter;
        return new List<CreateIndexModel<TripItemDecisionDocument>>
        {
            new(
                Builders<TripItemDecisionDocument>.IndexKeys
                    .Ascending(static document => document.TripPlanId)
                    .Ascending(static document => document.ParkItemId),
                new CreateIndexOptions<TripItemDecisionDocument>
                {
                    Unique = true,
                    Name = "uq_trip_item_decision_plan_item",
                }),
            new(
                Builders<TripItemDecisionDocument>.IndexKeys
                    .Ascending(static document => document.ReservedExpiresAtUtc),
                new CreateIndexOptions<TripItemDecisionDocument>
                {
                    Name = "ttl_trip_item_decision_reserved",
                    ExpireAfter = TimeSpan.Zero,
                    PartialFilterExpression = filters.Eq(
                        static document => document.DocumentState,
                        TripChildDocumentState.Reserved),
                }),
        };
    }

    public async Task<IReadOnlyCollection<TripItemDecision>> ListAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripItemDecisionDocument> filters =
            Builders<TripItemDecisionDocument>.Filter;
        List<TripItemDecisionDocument> documents = await this.collection.Find(
                filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
                & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed))
            .SortBy(static document => document.ParkItemId)
            .Limit(TripItemDecision.MaximumDecisionsPerTrip)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<TripItemDecision?> GetAsync(
        TripPlanId tripPlanId,
        string parkItemId,
        CancellationToken cancellationToken)
    {
        TripItemDecisionDocument? document = await this.collection.Find(
                BuildCommittedIdentityFilter(tripPlanId, parkItemId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<TripItemDecisionWriteResult> CreateAsync(
        TripItemDecision decision,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(lease);
        TripItemDecisionDocument shell = new()
        {
            Id = decision.Id.Value,
            TripPlanId = decision.TripPlanId.Value,
            ParkItemId = decision.ParkItemId,
            DocumentState = TripChildDocumentState.Reserved,
            OperationId = lease.OperationId,
            ChildMutationEpoch = lease.ChildMutationEpoch,
            LeaseGeneration = lease.Generation,
            LeaseExpiresAtUtc = lease.ExpiresAtUtc,
            ReservedExpiresAtUtc = lease.ExpiresAtUtc.AddMinutes(5),
            CreatedAt = decision.CreatedAtUtc,
            UpdatedAt = decision.UpdatedAtUtc,
        };
        try
        {
            await this.collection.InsertOneAsync(shell, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            TripItemDecision? existing = await this.GetAsync(
                decision.TripPlanId,
                decision.ParkItemId,
                cancellationToken);
            return new TripItemDecisionWriteResult(
                TripChildWriteOutcome.Conflict,
                existing,
                existing?.Version);
        }

        TripItemDecisionDocument materialized = decision.ToDocument();
        FilterDefinitionBuilder<TripItemDecisionDocument> filters =
            Builders<TripItemDecisionDocument>.Filter;
        FilterDefinition<TripItemDecisionDocument> filter = filters.Eq(
                static document => document.Id,
                decision.Id.Value)
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Reserved)
            & filters.Eq(static document => document.OperationId, lease.OperationId)
            & filters.Eq(static document => document.ChildMutationEpoch, lease.ChildMutationEpoch)
            & filters.Eq(static document => document.LeaseGeneration, lease.Generation)
            & TripChildMutationMongoDefinitions.BuildCreationLeaseGuard<TripItemDecisionDocument>();
        UpdateDefinitionBuilder<TripItemDecisionDocument> updates =
            Builders<TripItemDecisionDocument>.Update;
        TripItemDecisionDocument? committed = await this.collection.FindOneAndUpdateAsync(
            filter,
            updates
                .Set(static document => document.Status, materialized.Status)
                .Set(static document => document.Reason, materialized.Reason)
                .Set(static document => document.DecidedByUserId, materialized.DecidedByUserId)
                .Set(static document => document.Version, materialized.Version)
                .Set(static document => document.DocumentState, TripChildDocumentState.Committed)
                .Unset(static document => document.ReservedExpiresAtUtc)
                .Set(static document => document.CreatedAt, materialized.CreatedAt)
                .Set(static document => document.UpdatedAt, materialized.UpdatedAt),
            new FindOneAndUpdateOptions<TripItemDecisionDocument, TripItemDecisionDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        return committed is null
            ? new TripItemDecisionWriteResult(TripChildWriteOutcome.LeaseExpired)
            : new TripItemDecisionWriteResult(
                TripChildWriteOutcome.Success,
                committed.ToDomain(),
                committed.Version);
    }

    public async Task<TripItemDecisionWriteResult> ReplaceAsync(
        TripItemDecision decision,
        long expectedVersion,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(lease);
        FilterDefinitionBuilder<TripItemDecisionDocument> filters =
            Builders<TripItemDecisionDocument>.Filter;
        FilterDefinition<TripItemDecisionDocument> identity = BuildCommittedIdentityFilter(
                decision.TripPlanId,
                decision.ParkItemId)
            & filters.Eq(static document => document.Version, expectedVersion);
        TripItemDecisionDocument? reserved = await this.collection.FindOneAndUpdateAsync(
            identity & TripChildMutationMongoDefinitions.BuildPendingAvailableGuard<TripItemDecisionDocument>(),
            Builders<TripItemDecisionDocument>.Update.Set(
                static document => document.PendingMutation,
                TripChildMutationMongoDefinitions.ToPendingDocument(lease)),
            new FindOneAndUpdateOptions<TripItemDecisionDocument, TripItemDecisionDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        if (reserved is null)
        {
            TripItemDecision? existing = await this.GetAsync(
                decision.TripPlanId,
                decision.ParkItemId,
                cancellationToken);
            return new TripItemDecisionWriteResult(
                existing is null ? TripChildWriteOutcome.NotFound : TripChildWriteOutcome.Conflict,
                existing,
                existing?.Version);
        }

        TripItemDecisionDocument materialized = decision.ToDocument();
        FilterDefinition<TripItemDecisionDocument> commitFilter = identity
            & filters.Eq("pendingMutation.operationId", lease.OperationId)
            & filters.Eq("pendingMutation.childMutationEpoch", lease.ChildMutationEpoch)
            & filters.Eq("pendingMutation.leaseGeneration", lease.Generation)
            & TripChildMutationMongoDefinitions.BuildPendingLeaseGuard<TripItemDecisionDocument>();
        TripItemDecisionDocument? committed = await this.collection.FindOneAndUpdateAsync(
            commitFilter,
            Builders<TripItemDecisionDocument>.Update
                .Set(static document => document.Status, materialized.Status)
                .Set(static document => document.Reason, materialized.Reason)
                .Set(static document => document.DecidedByUserId, materialized.DecidedByUserId)
                .Set(static document => document.Version, materialized.Version)
                .Set(static document => document.UpdatedAt, materialized.UpdatedAt)
                .Unset(static document => document.PendingMutation),
            new FindOneAndUpdateOptions<TripItemDecisionDocument, TripItemDecisionDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        return committed is null
            ? new TripItemDecisionWriteResult(TripChildWriteOutcome.LeaseExpired)
            : new TripItemDecisionWriteResult(
                TripChildWriteOutcome.Success,
                committed.ToDomain(),
                committed.Version);
    }

    private static FilterDefinition<TripItemDecisionDocument> BuildCommittedIdentityFilter(
        TripPlanId tripPlanId,
        string parkItemId)
    {
        FilterDefinitionBuilder<TripItemDecisionDocument> filters =
            Builders<TripItemDecisionDocument>.Filter;
        return filters.Eq(static document => document.TripPlanId, tripPlanId.Value)
            & filters.Eq(static document => document.ParkItemId, parkItemId)
            & filters.Eq(static document => document.DocumentState, TripChildDocumentState.Committed);
    }
}
