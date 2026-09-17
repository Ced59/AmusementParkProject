using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
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
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_plan_owner_scope_operation"
            && index.Options.Unique != true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_plan_member_updated");
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_plan_creation_tombstone"
            && index.Options.ExpireAfter == TimeSpan.Zero);
    }

    [Fact]
    public void BuildCreationOperationFilter_ShouldBindKeyedOwnerScopeAndHashedKey()
    {
        FilterDefinition<TripPlanDocument> filter = TripPlanMongoDefinitions.BuildCreationOperationFilter(
            new[] { "owner-scope-hmac", "previous-owner-scope-hmac" },
            "hash-1");
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));

        Assert.Equal(
            new[] { "owner-scope-hmac", "previous-owner-scope-hmac" },
            rendered["ownerScopeHash"]["$in"].AsBsonArray.Select(static value => value.AsString));
        Assert.False(rendered.Contains("ownerUserId"));
        Assert.Equal("hash-1", rendered["creationOperationKeyHash"].AsString);
    }

    [Fact]
    public void BuildActiveCreationProjection_ShouldRetainEveryReplayDecisionField()
    {
        ProjectionDefinition<TripPlanDocument> projection =
            TripPlanMongoDefinitions.BuildActiveCreationProjection();
        BsonDocument rendered = projection.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));

        Assert.Equal(1, rendered["creationOperationKeyHash"].AsInt32);
        Assert.Equal(1, rendered["creationPayloadHash"].AsInt32);
        Assert.Equal(1, rendered["creationFingerprintKeyVersion"].AsInt32);
        Assert.Equal(1, rendered["creationSnapshot"].AsInt32);
        Assert.Equal(1, rendered["deletionState"].AsInt32);
    }

    [Fact]
    public void BuildChildEpochMutationFilter_ShouldOnlyRequireAnEmptyLeaseSetWhenEpochAdvances()
    {
        FilterDefinition<TripPlanDocument> filter =
            TripPlanMongoDefinitions.BuildChildEpochMutationFilter(2);
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("childMutationEpoch", json, StringComparison.Ordinal);
        Assert.Contains("$$NOW", json, StringComparison.Ordinal);
        Assert.Contains("$exists", json, StringComparison.Ordinal);
        Assert.Contains("$or", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildActiveChildLeaseIdentityFilter_ShouldBindTheGenerationAndMongoServerTime()
    {
        TripChildMutationLease lease = new(
            "operation-1",
            TripMemberId.Parse("member-1"),
            3,
            7,
            new DateTime(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc));

        FilterDefinition<TripPlanDocument> filter =
            TripPlanMongoDefinitions.BuildActiveChildLeaseIdentityFilter(lease);
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("operation-1", json, StringComparison.Ordinal);
        Assert.Contains("member-1", json, StringComparison.Ordinal);
        Assert.Contains("generation", json, StringComparison.Ordinal);
        Assert.Contains("$$NOW", json, StringComparison.Ordinal);
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
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("ownerUserId"));
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("ownerSlot"));
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("createdAt"));
        Assert.False(rendered.ToString().Contains("Voyage privé", StringComparison.Ordinal));
        Assert.False(rendered.ToString().Contains("Europe/Paris", StringComparison.Ordinal));
        Assert.False(rendered.ToString().Contains("user-1", StringComparison.Ordinal));
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

    [Fact]
    public void ResolveIdempotentCreation_WhenConcurrentActiveCreateMatches_ShouldReplayIt()
    {
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc));
        TripPlanDocument activeDocument = trip.ToDocument();
        activeDocument.CreationPayloadHash = "payload-hash";
        activeDocument.CreationSnapshot = activeDocument.CreateCreationSnapshot();

        IdempotentTripPlanCreationResult result = TripPlanRepository.ResolveIdempotentCreation(
            activeDocument,
            "payload-hash");

        Assert.Equal(IdempotentTripPlanCreationStatus.Replayed, result.Status);
        Assert.Equal(trip.Id, result.TripPlan?.Id);
    }

    [Fact]
    public void ResolveIdempotentCreation_WhenDeletedKeyHasDifferentPayload_ShouldConflict()
    {
        TripPlanDocument tombstone = new()
        {
            DeletionState = TripDeletionState.Purged,
            CreationPayloadHash = "original-payload-hash",
        };

        IdempotentTripPlanCreationResult result = TripPlanRepository.ResolveIdempotentCreation(
            tombstone,
            "different-payload-hash");

        Assert.Equal(IdempotentTripPlanCreationStatus.Conflict, result.Status);
        Assert.Null(result.TripPlan);
    }
}
