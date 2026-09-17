using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripPlanMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectOwnerCapacityAndIdempotency()
    {
        IReadOnlyCollection<CreateIndexModel<TripPlanDocument>> indexes = TripPlanMongoDefinitions.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_plan_owner_slot"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_plan_owner_operation"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_plan_member_updated");
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_plan_creation_tombstone"
            && index.Options.ExpireAfter == TimeSpan.Zero);
    }

    [Fact]
    public void BuildCreationOperationFilter_ShouldBindOwnerAndHashedKey()
    {
        FilterDefinition<TripPlanDocument> filter = TripPlanMongoDefinitions.BuildCreationOperationFilter(
            "user-1",
            "hash-1");
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));

        Assert.Equal("user-1", rendered["ownerUserId"].AsString);
        Assert.Equal("hash-1", rendered["creationOperationKeyHash"].AsString);
    }

    [Fact]
    public void BuildDeletionTombstone_ShouldScrubPrivateDataAndKeepOnlyTheReplayFence()
    {
        DateTime createdAtUtc = new(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage privé",
            TripDateProposal.Fixed(new DateOnly(2027, 6, 10)),
            "Europe/Paris",
            createdAtUtc);
        trip.BeginDeletion(createdAtUtc.AddMinutes(1));

        UpdateDefinition<TripPlanDocument> update = TripPlanMongoDefinitions.BuildDeletionTombstone(trip);
        BsonDocument rendered = update.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry)).AsBsonDocument;

        Assert.Equal(string.Empty, rendered["$set"]["title"].AsString);
        Assert.Equal(TripPlanStatus.Cancelled.ToString(), rendered["$set"]["status"].AsString);
        Assert.Equal(TripDeletionState.Purged.ToString(), rendered["$set"]["deletionState"].AsString);
        Assert.Empty(rendered["$set"]["members"].AsBsonArray);
        Assert.Equal(
            createdAtUtc.AddMinutes(1).Add(TripPlan.CreationReplayRetention),
            rendered["$set"]["creationOperationExpiresAtUtc"].ToUniversalTime());
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("destinationTimeZoneId"));
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("creationSnapshot"));
        Assert.False(rendered.ToString().Contains("Voyage privé", StringComparison.Ordinal));
        Assert.False(rendered.ToString().Contains("Europe/Paris", StringComparison.Ordinal));
    }

    [Fact]
    public void ResolveIdempotentCreation_WhenTripWasDeleted_ShouldNeverRecreateIt()
    {
        TripPlanDocument tombstone = new()
        {
            DeletionState = TripDeletionState.Purged,
            CreationPayloadHash = "payload-hash",
        };

        IdempotentTripPlanCreationResult result = TripPlanRepository.ResolveIdempotentCreation(
            tombstone,
            "payload-hash");

        Assert.Equal(IdempotentTripPlanCreationStatus.Deleted, result.Status);
        Assert.Null(result.TripPlan);
    }
}
