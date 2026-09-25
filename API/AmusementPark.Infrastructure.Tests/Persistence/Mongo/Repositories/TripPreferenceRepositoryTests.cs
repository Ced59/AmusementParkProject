using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripPreferenceRepositoryTests
{
    [Fact]
    public void BuildDepartureAuditRelocation_ShouldIdempotentlyPreserveEveryPendingMarker()
    {
        TripActivityPendingDocument[] pending =
        {
            CreatePending("marker-1", "preference-operation", 1),
            CreatePending("marker-2", "preference-operation", 1),
        };

        UpdateDefinition<TripPlanDocument> update =
            TripPreferenceRepository.BuildDepartureAuditRelocation(pending);
        BsonDocument rendered = update.Render(new RenderArgs<TripPlanDocument>(
                BsonSerializer.LookupSerializer<TripPlanDocument>(),
                BsonSerializer.SerializerRegistry))
            .AsBsonDocument;

        BsonArray preserved = rendered["$addToSet"]["pendingAuditEvents"]["$each"].AsBsonArray;
        Assert.Equal(2, preserved.Count);
        Assert.All(
            preserved,
            marker => Assert.Equal("preference-operation", marker["operationKey"].AsString));
    }

    [Fact]
    public void BuildDepartureSafeDeletionFilter_ShouldRejectAnUnrelocatedConcurrentMarker()
    {
        FilterDefinition<TripItemPreferenceDocument> filter =
            TripPreferenceRepository.BuildDepartureSafeDeletionFilter(
                new[] { "marker-1", "marker-2" });
        BsonDocument rendered = filter.Render(new RenderArgs<TripItemPreferenceDocument>(
                BsonSerializer.LookupSerializer<TripItemPreferenceDocument>(),
                BsonSerializer.SerializerRegistry))
            .AsBsonDocument;

        BsonArray relocated = rendered["pendingAuditEvents"]["$not"]["$elemMatch"]
            ["markerId"]["$nin"].AsBsonArray;
        Assert.Equal(new BsonArray { "marker-1", "marker-2" }, relocated);
    }

    [Fact]
    public void BuildDepartureSafeDeletionFilter_WithoutRelocatedMarkers_ShouldRequireAnEmptyQueue()
    {
        FilterDefinition<TripItemPreferenceDocument> filter =
            TripPreferenceRepository.BuildDepartureSafeDeletionFilter(Array.Empty<string>());
        BsonDocument rendered = filter.Render(new RenderArgs<TripItemPreferenceDocument>(
                BsonSerializer.LookupSerializer<TripItemPreferenceDocument>(),
                BsonSerializer.SerializerRegistry))
            .AsBsonDocument;

        Assert.False(rendered["pendingAuditEvents.0"]["$exists"].AsBoolean);
    }

    private static TripActivityPendingDocument CreatePending(
        string markerId,
        string operationKey,
        int affectedCount)
    {
        return new TripActivityPendingDocument
        {
            MarkerId = markerId,
            TripPlanId = "trip-1",
            ActorMemberId = "member-1",
            ActorRole = TripEffectiveRole.Participant,
            Kind = TripActivityKind.PreferencesUpdated,
            OperationKey = operationKey,
            AffectedCount = affectedCount,
            OccurredAtUtc = new DateTime(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc),
        };
    }
}
