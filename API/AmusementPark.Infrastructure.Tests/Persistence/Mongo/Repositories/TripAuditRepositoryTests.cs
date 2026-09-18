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

    private static BsonDocument Render(IndexKeysDefinition<TripActivityEventDocument> keys)
    {
        return keys.Render(new RenderArgs<TripActivityEventDocument>(
            BsonSerializer.LookupSerializer<TripActivityEventDocument>(),
            BsonSerializer.SerializerRegistry));
    }

}
