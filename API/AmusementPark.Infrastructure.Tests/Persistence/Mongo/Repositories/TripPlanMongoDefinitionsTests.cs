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
    public void BuildAccessibleListOptions_ShouldUseADedicatedMembershipSafetyLimit()
    {
        FindOptions<TripPlanDocument, TripPlanDocument> options =
            TripPlanMongoDefinitions.BuildAccessibleListOptions();

        Assert.Equal(TripPlanMongoDefinitions.MaximumAccessibleTripsPerRequest, options.Limit);
        Assert.True(options.Limit > TripPlan.MaximumPlansPerOwner);
        Assert.NotNull(options.Sort);
    }

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
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_plan_admission_fence"
            && index.Options.PartialFilterExpression is not null);
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
    public void BuildDomainMutation_ShouldNeverOverwriteTheAdmissionFence()
    {
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc));

        UpdateDefinition<TripPlanDocument> update = TripPlanMongoDefinitions.BuildDomainMutation(trip);
        BsonDocument rendered = update.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry)).AsBsonDocument;

        Assert.False(rendered["$set"].AsBsonDocument.Contains("memberAdmissionFence"));
        Assert.False(rendered.Contains("$unset"));
    }

    [Fact]
    public void BuildOwnershipTransferMutation_ShouldPreserveTheImmutableCreationScope()
    {
        DateTime createdAtUtc = new(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        TripMember owner = TripMember.Restore(
            TripMemberId.Parse("member-1"),
            "user-1",
            null,
            TripMembershipState.Active,
            createdAtUtc);
        TripMember target = TripMember.Restore(
            TripMemberId.Parse("member-2"),
            "user-2",
            TripDelegatedRole.Participant,
            TripMembershipState.Active,
            createdAtUtc.AddMinutes(1));
        TripPlan trip = TripPlan.Restore(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            TripPlanStatus.Draft,
            TripPlanAccessScope.MembersOnly,
            new[] { owner, target },
            TripAdmissionClosureState.Open,
            TripDeletionState.None,
            1,
            createdAtUtc,
            createdAtUtc.AddMinutes(1),
            1);
        trip.TransferOwnership(
            "user-1",
            target.Id,
            TripDelegatedRole.Editor,
            new DateTime(2026, 9, 17, 8, 2, 0, DateTimeKind.Utc));

        UpdateDefinition<TripPlanDocument> update =
            TripPlanMongoDefinitions.BuildOwnershipTransferMutation(trip, 7);
        BsonDocument rendered = update.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry)).AsBsonDocument;

        Assert.Equal("user-2", rendered["$set"]["ownerUserId"].AsString);
        Assert.Equal(7, rendered["$set"]["ownerSlot"].AsInt32);
        Assert.False(rendered["$set"].AsBsonDocument.Contains("ownerScopeHash"));
        Assert.False(rendered["$set"].AsBsonDocument.Contains("creationOperationKeyHash"));
        Assert.False(rendered["$set"].AsBsonDocument.Contains("creationPayloadHash"));
    }

    [Fact]
    public void BuildNoAdmissionInFlightFilter_ShouldAcceptOnlyMissingOrLegacyNullFences()
    {
        FilterDefinition<TripPlanDocument> filter =
            TripPlanMongoDefinitions.BuildNoAdmissionInFlightFilter();
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("memberAdmissionFence", json, StringComparison.Ordinal);
        Assert.Contains("$exists", json, StringComparison.Ordinal);
        Assert.Contains("null", json, StringComparison.OrdinalIgnoreCase);
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
    public void BuildExpiredAdmissionFenceFilter_ShouldBindIdentityAndMongoServerTime()
    {
        TripMemberAdmissionFence fence = TripMemberAdmissionFence.Prepare(
            TripInvitationId.Parse("invitation-1"),
            "operation-1",
            "user-2",
            3,
            new DateTime(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc));

        FilterDefinition<TripPlanDocument> filter =
            TripAdmissionRepository.BuildExpiredFenceCancellationFilter(
                TripPlanId.Parse("trip-1"),
                fence);
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("trip-1", json, StringComparison.Ordinal);
        Assert.Contains("invitation-1", json, StringComparison.Ordinal);
        Assert.Contains("operation-1", json, StringComparison.Ordinal);
        Assert.Contains("user-2", json, StringComparison.Ordinal);
        Assert.Contains("generation", json, StringComparison.Ordinal);
        Assert.Contains("$$NOW", json, StringComparison.Ordinal);
        Assert.Contains("$gte", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveCancellationReplay_WhenFenceAndProvisionalMemberAreAlreadyGone_ShouldResumeCleanup()
    {
        TripMemberAdmissionFence fence = TripMemberAdmissionFence.Prepare(
            TripInvitationId.Parse("invitation-1"),
            "operation-1",
            "user-2",
            3,
            new DateTime(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        TripPlanDocument cleanedPlan = new()
        {
            Id = "trip-1",
            Members = new List<TripMemberDocument>(),
        };

        TripAdmissionWriteOutcome outcome =
            TripAdmissionRepository.ResolveCancellationReplay(cleanedPlan, fence);

        Assert.Equal(TripAdmissionWriteOutcome.Success, outcome);
    }

    [Fact]
    public void ResolveCancellationReplay_WhenMemberWasEstablished_ShouldPreserveAcceptance()
    {
        TripMemberAdmissionFence fence = TripMemberAdmissionFence.Prepare(
            TripInvitationId.Parse("invitation-1"),
            "operation-1",
            "user-2",
            3,
            new DateTime(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        TripPlanDocument establishedPlan = new()
        {
            Id = "trip-1",
            Members = new List<TripMemberDocument>
            {
                new()
                {
                    MemberId = "member-2",
                    UserId = "user-2",
                    State = TripMembershipState.Active,
                    AdmissionOperationId = "operation-1",
                },
            },
        };

        TripAdmissionWriteOutcome outcome =
            TripAdmissionRepository.ResolveCancellationReplay(establishedPlan, fence);

        Assert.Equal(TripAdmissionWriteOutcome.AlreadyCompleted, outcome);
    }

    [Fact]
    public void ResolveCancellationReplay_WhenAnotherInvitationEstablishedTheMember_ShouldResumeCleanup()
    {
        TripMemberAdmissionFence fence = TripMemberAdmissionFence.Prepare(
            TripInvitationId.Parse("invitation-1"),
            "operation-1",
            "user-2",
            3,
            new DateTime(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        TripPlanDocument planJoinedThroughAnotherInvitation = new()
        {
            Id = "trip-1",
            Members = new List<TripMemberDocument>
            {
                new()
                {
                    MemberId = "member-2",
                    UserId = "user-2",
                    State = TripMembershipState.Active,
                    AdmissionOperationId = "operation-2",
                },
            },
        };

        TripAdmissionWriteOutcome outcome = TripAdmissionRepository.ResolveCancellationReplay(
            planJoinedThroughAnotherInvitation,
            fence);

        Assert.Equal(TripAdmissionWriteOutcome.Success, outcome);
    }

    [Fact]
    public void BuildEstablishedMemberReplayFilter_ShouldRequireTheExactAdmissionOperation()
    {
        TripMemberAdmissionFence fence = TripMemberAdmissionFence.Prepare(
            TripInvitationId.Parse("invitation-1"),
            "operation-1",
            "user-2",
            3,
            new DateTime(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc));

        FilterDefinition<TripPlanDocument> filter =
            TripAdmissionRepository.BuildEstablishedMemberReplayFilter(
                TripPlanId.Parse("trip-1"),
                fence);
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));
        BsonDocument memberMatch = rendered["members"]["$elemMatch"].AsBsonDocument;

        Assert.Equal("trip-1", rendered["_id"].AsString);
        Assert.Equal("user-2", memberMatch["userId"].AsString);
        Assert.Equal(TripMembershipState.Active.ToString(), memberMatch["state"].AsString);
        Assert.Equal("operation-1", memberMatch["admissionOperationId"].AsString);
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
        Assert.Empty(rendered["$set"]["parkCandidateOrderIds"].AsBsonArray);
        Assert.Equal(0, rendered["$set"]["parkCandidateOrderVersion"].AsInt64);
        Assert.Equal(
            createdAtUtc.AddMinutes(1).Add(TripPlan.CreationReplayRetention),
            rendered["$set"]["creationOperationExpiresAtUtc"].ToUniversalTime());
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("destinationTimeZoneId"));
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("creationSnapshot"));
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("memberAdmissionFence"));
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("ownerUserId"));
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("ownerSlot"));
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("createdAt"));
        Assert.False(rendered.ToString().Contains("Voyage privé", StringComparison.Ordinal));
        Assert.False(rendered.ToString().Contains("Europe/Paris", StringComparison.Ordinal));
        Assert.False(rendered.ToString().Contains("user-1", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildDeletionFinalizationFilter_ShouldAcceptTheOwnedPendingOrMatchingPurgedVersion()
    {
        DateTime createdAtUtc = new(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            createdAtUtc);
        trip.BeginDeletion(createdAtUtc.AddMinutes(1));

        FilterDefinition<TripPlanDocument> filter =
            TripPlanMongoDefinitions.BuildDeletionFinalizationFilter(trip);
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("trip-1", json, StringComparison.Ordinal);
        Assert.Contains("user-1", json, StringComparison.Ordinal);
        Assert.Contains(TripDeletionState.Pending.ToString(), json, StringComparison.Ordinal);
        Assert.Contains(TripDeletionState.Purged.ToString(), json, StringComparison.Ordinal);
        Assert.Contains("version", json, StringComparison.Ordinal);
        Assert.Contains("$or", json, StringComparison.Ordinal);
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
