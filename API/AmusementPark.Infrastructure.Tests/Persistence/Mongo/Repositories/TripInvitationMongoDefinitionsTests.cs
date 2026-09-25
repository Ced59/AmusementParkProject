using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripInvitationMongoDefinitionsTests
{
    [Fact]
    public void Indexes_ShouldProtectTokensOperationsCapacityAndExpiry()
    {
        IReadOnlyCollection<CreateIndexModel<TripInvitationDocument>> indexes =
            TripInvitationRepository.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_invitation_token_hash"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_invitation_operation"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_invitation_active_slot"
            && index.Options.Unique == true
            && index.Options.PartialFilterExpression is not null);
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_invitation_retention"
            && index.Options.ExpireAfter == TimeSpan.Zero);
        Assert.Contains(indexes, index => index.Options.Name == "ttl_trip_invitation_prepared"
            && index.Options.ExpireAfter == TimeSpan.Zero);
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_invitation_status_expires");
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_invitation_acceptance_recovery");
    }

    [Fact]
    public void ExpirationCleanup_ShouldUseMongoServerTimeBeforeReleasingAnActiveSlot()
    {
        FilterDefinition<TripInvitationDocument> filter =
            TripInvitationRepository.BuildElapsedExpirationFilter();
        BsonDocument rendered = filter.Render(new RenderArgs<TripInvitationDocument>(
            BsonSerializer.LookupSerializer<TripInvitationDocument>(),
            BsonSerializer.SerializerRegistry));

        Assert.Equal("$$NOW", rendered["$expr"]["$gte"][0].AsString);
        Assert.Equal("$expiresAtUtc", rendered["$expr"]["$gte"][1].AsString);
    }

    [Fact]
    public void PublicLookup_ShouldUseMongoServerTimeInsteadOfTheWebNodeClock()
    {
        FilterDefinition<TripInvitationDocument> filter = TripInvitationRepository.BuildPublicActiveFilter();
        BsonDocument rendered = filter.Render(new RenderArgs<TripInvitationDocument>(
            BsonSerializer.LookupSerializer<TripInvitationDocument>(),
            BsonSerializer.SerializerRegistry));

        Assert.Equal("$$NOW", rendered["$expr"]["$lt"][0].AsString);
        Assert.Equal("$expiresAtUtc", rendered["$expr"]["$lt"][1].AsString);
    }

    [Fact]
    public void PendingAcceptanceFilter_ShouldExcludeAcceptedAdmissionsAlreadyCompleted()
    {
        FilterDefinition<TripInvitationDocument> filter =
            TripAdmissionRepository.BuildPendingAcceptanceFilter();
        BsonDocument rendered = filter.Render(new RenderArgs<TripInvitationDocument>(
            BsonSerializer.LookupSerializer<TripInvitationDocument>(),
            BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains(TripInvitationStatus.Accepting.ToString(), json, StringComparison.Ordinal);
        Assert.Contains(TripInvitationStatus.Accepted.ToString(), json, StringComparison.Ordinal);
        Assert.Contains("admissionCompletedAtUtc", json, StringComparison.Ordinal);
        Assert.Contains("null", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppendPendingAudit_ShouldSuspendTtlUntilTheMarkerIsMaterialized()
    {
        DateTime occurredAtUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        TripActivityWrite activity = new(
            TripPlanId.New(),
            null,
            null,
            TripActivityKind.InvitationDeclined,
            "invitation:decline:operation-1",
            1,
            occurredAtUtc);
        UpdateDefinition<TripInvitationDocument> update =
            TripInvitationRepository.AppendPendingAudit(
                Builders<TripInvitationDocument>.Update.Set(
                    static document => document.Status,
                    TripInvitationStatus.Declined),
                activity);

        BsonDocument rendered = update.Render(new RenderArgs<TripInvitationDocument>(
            BsonSerializer.LookupSerializer<TripInvitationDocument>(),
            BsonSerializer.SerializerRegistry)).AsBsonDocument;

        Assert.True(rendered["$unset"].AsBsonDocument.Contains("retentionExpiresAtUtc"));
        Assert.True(rendered["$push"].AsBsonDocument.Contains("pendingAuditEvents"));
    }

    [Fact]
    public void RetentionRestorePipeline_ShouldKeepTheReplayWindowOrUseServerTime()
    {
        BsonDocument pipeline = Assert.Single(
            TripAuditRepository.BuildInvitationRetentionRestorePipeline());
        string json = pipeline.ToJson();

        Assert.Contains("retentionExpiresAtUtc", json, StringComparison.Ordinal);
        Assert.Contains("$expiresAtUtc", json, StringComparison.Ordinal);
        Assert.Contains("$max", json, StringComparison.Ordinal);
        Assert.Contains("$$NOW", json, StringComparison.Ordinal);
    }
}
