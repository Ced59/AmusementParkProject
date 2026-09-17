using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class TripPlanMongoDefinitions
{
    public static FilterDefinition<TripPlanDocument> BuildCreationOperationFilter(
        IReadOnlyCollection<string> ownerScopeHashes,
        string operationKeyHash)
    {
        ArgumentNullException.ThrowIfNull(ownerScopeHashes);
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        return filters.In(static document => document.OwnerScopeHash, ownerScopeHashes)
            & filters.Eq(static document => document.CreationOperationKeyHash, operationKeyHash);
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
            .Set(static item => item.UpdatedAt, document.UpdatedAt)
            .Set(static item => item.Version, document.Version);
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
            updates.Set(static document => document.AdmissionClosureState, TripAdmissionClosureState.Closed),
            updates.Set(static document => document.DeletionState, TripDeletionState.Purged),
            updates.Unset(static document => document.CreationSnapshot),
            updates.Set(
                static document => document.CreationOperationExpiresAtUtc,
                trip.UpdatedAtUtc.Add(TripPlan.CreationReplayRetention)),
            updates.Set(static document => document.UpdatedAt, trip.UpdatedAtUtc),
            updates.Set(static document => document.Version, trip.Version));
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
                    .Ascending(static document => document.OwnerUserId)
                    .Ascending(static document => document.CreationOperationKeyHash),
                new CreateIndexOptions<TripPlanDocument>
                {
                    Unique = true,
                    Name = "uq_trip_plan_owner_operation",
                    PartialFilterExpression = Builders<TripPlanDocument>.Filter.Eq(
                        static document => document.DeletionState,
                        TripDeletionState.None),
                }),
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending(static document => document.OwnerScopeHash)
                    .Ascending(static document => document.CreationOperationKeyHash),
                new CreateIndexOptions { Name = "ix_trip_plan_owner_scope_operation" }),
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
                    .Ascending(static document => document.CreationOperationExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "ttl_trip_plan_creation_tombstone",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };
    }
}
