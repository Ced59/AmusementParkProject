using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class TripPlanMongoDefinitions
{
    public const int MaximumAccessibleTripsPerRequest = 100;
    public const string LegacyOwnerOperationIndexName = "uq_trip_plan_owner_operation";
    public const string LegacyOwnerScopeOperationIndexName = "ix_trip_plan_owner_scope_operation";
    public const string CreatorScopeOperationIndexName = "uq_trip_plan_creator_scope_operation";

    public static FindOptions<TripPlanDocument, TripPlanDocument> BuildAccessibleListOptions()
    {
        SortDefinitionBuilder<TripPlanDocument> sorts = Builders<TripPlanDocument>.Sort;
        return new FindOptions<TripPlanDocument, TripPlanDocument>
        {
            Limit = MaximumAccessibleTripsPerRequest,
            Sort = sorts.Descending(static document => document.UpdatedAt)
                .Ascending(static document => document.Id),
        };
    }

    public static FilterDefinition<TripPlanDocument> BuildNoActiveChildLeaseFilter()
    {
        return new BsonDocumentFilterDefinition<TripPlanDocument>(new BsonDocument(
            "$expr",
            new BsonDocument("$eq", new BsonArray
            {
                new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
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
                        "cond",
                        new BsonDocument("$gt", new BsonArray
                        {
                            "$$lease.expiresAtUtc",
                            "$$NOW",
                        })
                    },
                })),
                0,
            })));
    }

    public static FilterDefinition<TripPlanDocument> BuildChildEpochMutationFilter(long targetEpoch)
    {
        if (targetEpoch < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targetEpoch));
        }

        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> sameEpoch =
            filters.Eq(static document => document.ChildMutationEpoch, targetEpoch);
        if (targetEpoch == 1)
        {
            return sameEpoch | filters.Exists(static document => document.ChildMutationEpoch, false);
        }

        long previousEpoch = targetEpoch - 1;
        FilterDefinition<TripPlanDocument> previousEpochFilter =
            filters.Eq(static document => document.ChildMutationEpoch, previousEpoch);
        if (previousEpoch == 1)
        {
            previousEpochFilter |= filters.Exists(
                static document => document.ChildMutationEpoch,
                false);
        }

        return sameEpoch | (previousEpochFilter & BuildNoActiveChildLeaseFilter());
    }

    public static FilterDefinition<TripPlanDocument> BuildActiveChildLeaseIdentityFilter(
        TripChildMutationLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        BsonDocument matchingLease = new("$filter", new BsonDocument
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
                "cond",
                new BsonDocument("$and", new BsonArray
                {
                    new BsonDocument("$eq", new BsonArray { "$$lease.operationId", lease.OperationId }),
                    new BsonDocument("$eq", new BsonArray
                    {
                        "$$lease.actorMemberId",
                        lease.ActorMemberId.Value,
                    }),
                    new BsonDocument("$eq", new BsonArray
                    {
                        "$$lease.childMutationEpoch",
                        lease.ChildMutationEpoch,
                    }),
                    new BsonDocument("$eq", new BsonArray
                    {
                        "$$lease.generation",
                        lease.Generation,
                    }),
                    new BsonDocument("$gt", new BsonArray { "$$lease.expiresAtUtc", "$$NOW" }),
                })
            },
        });
        BsonDocument expression = new("$expr", new BsonDocument("$gt", new BsonArray
        {
            new BsonDocument("$size", matchingLease),
            0,
        }));
        return new BsonDocumentFilterDefinition<TripPlanDocument>(expression);
    }

    public static FilterDefinition<TripPlanDocument> BuildCreationOperationFilter(
        IReadOnlyCollection<string> ownerScopeHashes,
        string operationKeyHash)
    {
        ArgumentNullException.ThrowIfNull(ownerScopeHashes);
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        return filters.In(static document => document.OwnerScopeHash, ownerScopeHashes)
            & filters.Eq(static document => document.CreationOperationKeyHash, operationKeyHash);
    }

    public static FilterDefinition<TripPlanDocument> BuildNoAdmissionInFlightFilter()
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        return filters.Exists(static document => document.MemberAdmissionFence, false)
            | filters.Eq(static document => document.MemberAdmissionFence, null);
    }

    public static UpdateDefinition<TripPlanDocument> BuildDomainMutation(TripPlan trip)
    {
        TripPlanDocument document = trip.ToDocument();
        UpdateDefinitionBuilder<TripPlanDocument> updates = Builders<TripPlanDocument>.Update;
        return updates.Set(static item => item.Title, document.Title)
            .Set(static item => item.DateProposal, document.DateProposal)
            .Set(static item => item.DestinationTimeZoneId, document.DestinationTimeZoneId)
            .Set(static item => item.Status, document.Status)
            .Set(static item => item.AccessScope, document.AccessScope)
            .Set(static item => item.Members, document.Members)
            .Set(static item => item.AdmissionClosureState, document.AdmissionClosureState)
            .Set(static item => item.DeletionState, document.DeletionState)
            .Set(static item => item.ChildMutationEpoch, document.ChildMutationEpoch)
            .Set(static item => item.UpdatedAt, document.UpdatedAt)
            .Set(static item => item.Version, document.Version);
    }

    public static UpdateDefinition<TripPlanDocument> BuildOwnershipTransferMutation(
        TripPlan trip,
        int ownerSlot)
    {
        ArgumentNullException.ThrowIfNull(trip);
        if (ownerSlot is < 0 or >= TripPlan.MaximumPlansPerOwner)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerSlot));
        }

        return BuildDomainMutation(trip)
            .Set(static document => document.OwnerUserId, trip.OwnerUserId)
            .Set(static document => document.OwnerSlot, ownerSlot);
    }

    public static ProjectionDefinition<TripPlanDocument> BuildActiveCreationProjection()
    {
        return Builders<TripPlanDocument>.Projection
            .Include(static document => document.OwnerSlot)
            .Include(static document => document.CreationOperationKeyHash)
            .Include(static document => document.CreationPayloadHash)
            .Include(static document => document.CreationFingerprintKeyVersion)
            .Include(static document => document.CreationSnapshot)
            .Include(static document => document.DeletionState)
            .Include(static document => document.Id)
            .Include(static document => document.OwnerUserId);
    }

    public static UpdateDefinition<TripPlanDocument> BuildDeletionTombstone(TripPlan trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        if (trip.DeletionState != TripDeletionState.Pending
            || trip.AdmissionClosureState != TripAdmissionClosureState.Closing)
        {
            throw new ArgumentException("The trip must have started deletion before it can be purged.", nameof(trip));
        }

        UpdateDefinitionBuilder<TripPlanDocument> updates = Builders<TripPlanDocument>.Update;
        return updates.Combine(
            updates.Set(static document => document.Title, string.Empty),
            updates.Unset(static document => document.OwnerUserId),
            updates.Unset(static document => document.OwnerSlot),
            updates.Unset(static document => document.CreatedAt),
            updates.Set(static document => document.DateProposal, new TripDateProposalDocument
            {
                Kind = TripDateProposalKind.None,
            }),
            updates.Unset(static document => document.DestinationTimeZoneId),
            updates.Set(static document => document.Status, TripPlanStatus.Cancelled),
            updates.Set(static document => document.AccessScope, TripPlanAccessScope.MembersOnly),
            updates.Set(static document => document.Members, new List<TripMemberDocument>()),
            updates.Unset(static document => document.MemberAdmissionFence),
            updates.Set(static document => document.AdmissionClosureState, TripAdmissionClosureState.Closed),
            updates.Set(static document => document.DeletionState, TripDeletionState.Purged),
            updates.Set(static document => document.ChildMutationEpoch, trip.ChildMutationEpoch),
            updates.Set(
                static document => document.ActiveChildMutationLeases,
                new List<TripChildMutationLeaseDocument>()),
            updates.Set(
                static document => document.ParkCandidateOrderIds,
                new List<string>()),
            updates.Set(static document => document.ParkCandidateOrderVersion, 0),
            updates.Unset(static document => document.CreationSnapshot),
            updates.Set(
                static document => document.CreationOperationExpiresAtUtc,
                trip.UpdatedAtUtc.Add(TripPlan.CreationReplayRetention)),
            updates.Set(static document => document.UpdatedAt, trip.UpdatedAtUtc),
            updates.Set(static document => document.Version, trip.Version));
    }

    public static FilterDefinition<TripPlanDocument> BuildDeletionFinalizationFilter(TripPlan trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> exactTripVersion = filters.Eq(
                static document => document.Id,
                trip.Id.Value)
            & filters.Eq(static document => document.Version, trip.Version);
        FilterDefinition<TripPlanDocument> ownedPending = filters.Eq(
                static document => document.OwnerUserId,
                trip.OwnerUserId)
            & filters.Eq(static document => document.DeletionState, TripDeletionState.Pending);
        FilterDefinition<TripPlanDocument> alreadyPurged = filters.Eq(
            static document => document.DeletionState,
            TripDeletionState.Purged);
        return exactTripVersion & (ownedPending | alreadyPurged);
    }

    public static IReadOnlyCollection<CreateIndexModel<TripPlanDocument>> BuildIndexes()
    {
        return new List<CreateIndexModel<TripPlanDocument>>
        {
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending(static document => document.OwnerUserId)
                    .Ascending(static document => document.OwnerSlot),
                new CreateIndexOptions<TripPlanDocument>
                {
                    Unique = true,
                    Name = "uq_trip_plan_owner_slot",
                    PartialFilterExpression = Builders<TripPlanDocument>.Filter.Eq(
                        static document => document.DeletionState,
                        TripDeletionState.None),
                }),
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending(static document => document.OwnerScopeHash)
                    .Ascending(static document => document.CreationOperationKeyHash),
                new CreateIndexOptions<TripPlanDocument>
                {
                    Unique = true,
                    Name = CreatorScopeOperationIndexName,
                    PartialFilterExpression = Builders<TripPlanDocument>.Filter.Eq(
                        static document => document.DeletionState,
                        TripDeletionState.None),
                }),
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending("members.userId")
                    .Ascending("members.state")
                    .Ascending(static document => document.DeletionState)
                    .Descending(static document => document.UpdatedAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "ix_trip_plan_member_updated" }),
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending(static document => document.OwnerUserId)
                    .Ascending(static document => document.Status)
                    .Ascending(static document => document.DeletionState)
                    .Descending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = "ix_trip_plan_owner_status_updated" }),
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending(static document => document.DeletionState)
                    .Ascending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = "ix_trip_plan_deletion_recovery" }),
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending("memberAdmissionFence.leaseExpiresAtUtc")
                    .Ascending("memberAdmissionFence.state"),
                new CreateIndexOptions<TripPlanDocument>
                {
                    Name = "ix_trip_plan_admission_fence",
                    PartialFilterExpression = Builders<TripPlanDocument>.Filter.Type(
                        static document => document.MemberAdmissionFence,
                        BsonType.Document),
                }),
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending(static document => document.CreationOperationExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "ttl_trip_plan_creation_tombstone",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };
    }
}
