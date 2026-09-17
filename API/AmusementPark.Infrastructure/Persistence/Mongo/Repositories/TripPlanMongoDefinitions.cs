using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class TripPlanMongoDefinitions
{
    public static FilterDefinition<TripPlanDocument> BuildCreationOperationFilter(
        string ownerUserId,
        string operationKeyHash)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        return filters.Eq(static document => document.OwnerUserId, ownerUserId)
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

    public static IReadOnlyCollection<CreateIndexModel<TripPlanDocument>> BuildIndexes()
    {
        return new List<CreateIndexModel<TripPlanDocument>>
        {
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending(static document => document.OwnerUserId)
                    .Ascending(static document => document.OwnerSlot),
                new CreateIndexOptions { Unique = true, Name = "uq_trip_plan_owner_slot" }),
            new(
                Builders<TripPlanDocument>.IndexKeys
                    .Ascending(static document => document.OwnerUserId)
                    .Ascending(static document => document.CreationOperationKeyHash),
                new CreateIndexOptions { Unique = true, Name = "uq_trip_plan_owner_operation" }),
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
        };
    }
}
