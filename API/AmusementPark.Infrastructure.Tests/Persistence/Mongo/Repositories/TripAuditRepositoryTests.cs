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
    public void BuildOutboxIndexes_ShouldProtectDurabilityAndBoundPendingScans()
    {
        IReadOnlyCollection<CreateIndexModel<TripActivityOutboxDocument>> indexes =
            TripAuditRepository.BuildOutboxIndexes();

        CreateIndexModel<TripActivityOutboxDocument> operation = Assert.Single(
            indexes,
            index => index.Options.Name == "uq_trip_audit_outbox_operation");
        CreateIndexModel<TripActivityOutboxDocument> pending = Assert.Single(
            indexes,
            index => index.Options.Name == "ix_trip_audit_outbox_pending");
        CreateIndexModel<TripActivityOutboxDocument> retention = Assert.Single(
            indexes,
            index => index.Options.Name == "ttl_trip_audit_outbox_materialized");
        Assert.Equal(3, indexes.Count);
        Assert.True(operation.Options.Unique);
        Assert.Equal(TripAuditRepository.MaterializedOutboxRetention, retention.Options.ExpireAfter);
        Assert.Equal(
            new BsonDocument { { "tripPlanId", 1 }, { "operationKey", 1 } },
            Render(operation.Keys));
        Assert.Equal(
            new BsonDocument { { "materializedAtUtc", 1 }, { "createdAt", 1 } },
            Render(pending.Keys));
        Assert.Equal(
            new BsonDocument { { "materializedAtUtc", 1 } },
            Render(retention.Keys));
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
    public void OutboxDocument_ShouldNeverPersistAnAccountIdentifier()
    {
        TripActivityOutboxDocument document = new TripActivityOutboxDocument
        {
            Id = "outbox-1",
            TripPlanId = "trip-1",
            ActorMemberId = "member-1",
            Kind = AmusementPark.Core.Domain.Trips.TripActivityKind.TripRenamed,
            OperationKey = "root:TripRenamed:2",
            AffectedCount = 1,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        BsonDocument serialized = document.ToBsonDocument();

        Assert.DoesNotContain("actorUserId", serialized.Names);
        Assert.DoesNotContain("userId", serialized.Names);
    }

    private static BsonDocument Render(IndexKeysDefinition<TripActivityEventDocument> keys)
    {
        return keys.Render(new RenderArgs<TripActivityEventDocument>(
            BsonSerializer.LookupSerializer<TripActivityEventDocument>(),
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(IndexKeysDefinition<TripActivityOutboxDocument> keys)
    {
        return keys.Render(new RenderArgs<TripActivityOutboxDocument>(
            BsonSerializer.LookupSerializer<TripActivityOutboxDocument>(),
            BsonSerializer.SerializerRegistry));
    }
}
