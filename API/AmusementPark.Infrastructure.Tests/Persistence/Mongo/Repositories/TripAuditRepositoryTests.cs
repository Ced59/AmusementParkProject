using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripAuditRepositoryTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectIdempotencyAndSequenceOrdering()
    {
        IReadOnlyCollection<CreateIndexModel<TripActivityEventDocument>> indexes =
            TripAuditRepository.BuildIndexes();

        CreateIndexModel<TripActivityEventDocument> operation = Assert.Single(
            indexes,
            index => index.Options.Name == "uq_trip_audit_operation");
        CreateIndexModel<TripActivityEventDocument> sequence = Assert.Single(
            indexes,
            index => index.Options.Name == "uq_trip_audit_sequence");
        CreateIndexModel<TripActivityEventDocument> date = Assert.Single(
            indexes,
            index => index.Options.Name == "ix_trip_audit_date");
        Assert.Equal(3, indexes.Count);
        Assert.True(operation.Options.Unique);
        Assert.True(sequence.Options.Unique);
        Assert.Equal(
            new BsonDocument { { "tripPlanId", 1 }, { "operationKey", 1 } },
            Render(operation.Keys));
        Assert.Equal(
            new BsonDocument { { "tripPlanId", 1 }, { "sequence", -1 } },
            Render(sequence.Keys));
        Assert.Equal(
            new BsonDocument { { "tripPlanId", 1 }, { "createdAt", -1 } },
            Render(date.Keys));
    }

    [Fact]
    public void PendingDocument_ShouldRoundTripWithoutAnAccountIdentifier()
    {
        TripActivityPendingDocument document = new()
        {
            TripPlanId = Guid.NewGuid().ToString(),
            ActorMemberId = Guid.NewGuid().ToString(),
            Kind = AmusementPark.Core.Domain.Trips.TripActivityKind.TripRenamed,
            OperationKey = "root:TripRenamed:2",
            AffectedCount = 1,
            OccurredAtUtc = DateTime.UtcNow,
        };

        BsonDocument serialized = document.ToBsonDocument();
        TripActivityPendingDocument roundTrip =
            BsonSerializer.Deserialize<TripActivityPendingDocument>(serialized);

        Assert.Equal(document.TripPlanId, roundTrip.TripPlanId);
        Assert.Equal(document.OperationKey, roundTrip.OperationKey);
        Assert.DoesNotContain("actorUserId", serialized.Names);
        Assert.DoesNotContain("userId", serialized.Names);
    }

    [Fact]
    public void Document_ShouldNeverPersistAnAccountIdentifier()
    {
        TripActivityEventDocument document = new TripActivityEventDocument
        {
            Id = "activity-1",
            TripPlanId = "trip-1",
            ActorMemberId = "member-1",
            Kind = AmusementPark.Core.Domain.Trips.TripActivityKind.TripRenamed,
            OperationKey = "root:TripRenamed:2",
            Sequence = 1,
            AffectedCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        BsonDocument serialized = document.ToBsonDocument();

        Assert.DoesNotContain("actorUserId", serialized.Names);
        Assert.DoesNotContain("userId", serialized.Names);
    }

    [Fact]
    public void TripPlanDocument_ShouldEmbedTheDurablePendingMarker()
    {
        TripPlanDocument document = new()
        {
            Id = "trip-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PendingAuditEvents = new List<TripActivityPendingDocument>
            {
                new()
                {
                    TripPlanId = "trip-1",
                    ActorMemberId = "member-1",
                    Kind = AmusementPark.Core.Domain.Trips.TripActivityKind.TripRenamed,
                    OperationKey = "root:TripRenamed:2",
                    AffectedCount = 1,
                    OccurredAtUtc = DateTime.UtcNow,
                },
            },
        };

        BsonDocument serialized = document.ToBsonDocument();

        Assert.Single(serialized["pendingAuditEvents"].AsBsonArray);
    }

    [Fact]
    public void BuildReconciledWrite_ShouldCountTheCompletePreferenceOperation()
    {
        string tripPlanId = Guid.NewGuid().ToString();
        TripActivityPendingDocument[] markers = Enumerable.Range(0, 250)
            .Select(index => new TripActivityPendingDocument
            {
                MarkerId = $"marker-{index}",
                TripPlanId = tripPlanId,
                ActorMemberId = "member-1",
                ActorRole = TripEffectiveRole.Participant,
                Kind = TripActivityKind.PreferencesUpdated,
                OperationKey = "child:PreferencesUpdated:operation:1",
                AffectedCount = 1,
                OccurredAtUtc = DateTime.UtcNow.AddTicks(index),
            })
            .ToArray();

        TripActivityWrite reconciled = TripAuditRepository.BuildReconciledWrite(markers);

        Assert.Equal(250, reconciled.AffectedCount);
    }

    [Fact]
    public void BuildReconciledWrite_ShouldDeduplicateARelocatedPreferenceMarker()
    {
        TripActivityPendingDocument marker = new()
        {
            MarkerId = "marker-1",
            TripPlanId = Guid.NewGuid().ToString(),
            ActorMemberId = "member-1",
            ActorRole = TripEffectiveRole.Participant,
            Kind = TripActivityKind.PreferencesUpdated,
            OperationKey = "child:PreferencesUpdated:operation:1",
            AffectedCount = 1,
            OccurredAtUtc = DateTime.UtcNow,
        };

        TripActivityWrite reconciled = TripAuditRepository.BuildReconciledWrite(
            new[] { marker, marker });

        Assert.Equal(1, reconciled.AffectedCount);
    }

    [Fact]
    public void BuildPendingOperationPipeline_ShouldFilterWithoutTruncatingTheSelectedGroup()
    {
        BsonDocument[] pipeline = TripAuditRepository.BuildPendingOperationPipeline(
            new[] { "operation-1", "operation-2" });

        Assert.Equal(2, pipeline.Length);
        Assert.Equal(
            new BsonArray { "operation-1", "operation-2" },
            pipeline[0]["$match"]["pendingAuditEvents.operationKey"]["$in"].AsBsonArray);
        Assert.False(pipeline[1].ToString().Contains("$slice", StringComparison.Ordinal));
        Assert.Contains("$filter", pipeline[1].ToString(), StringComparison.Ordinal);
    }

    private static BsonDocument Render(IndexKeysDefinition<TripActivityEventDocument> keys)
    {
        return keys.Render(new RenderArgs<TripActivityEventDocument>(
            BsonSerializer.LookupSerializer<TripActivityEventDocument>(),
            BsonSerializer.SerializerRegistry));
    }

}
