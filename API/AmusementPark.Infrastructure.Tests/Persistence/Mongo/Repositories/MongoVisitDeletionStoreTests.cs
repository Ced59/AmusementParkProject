using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class MongoVisitDeletionStoreTests
{
    private static readonly DateTime DeletedAtUtc =
        new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildTombstoneFilter_ShouldRequireTheOwnedDeletedVisit()
    {
        BsonDocument filter = Render(
            MongoVisitDeletionStore.BuildTombstoneFilter(
                VisitId.Parse("visit-1"),
                "owner-1"));

        Assert.Equal("visit-1", filter["_id"].AsString);
        Assert.Equal("owner-1", filter["userId"].AsString);
        Assert.True(filter[MongoVisitDeletionStore.DeletedAtUtcPath]
            .AsBsonDocument["$exists"].AsBoolean);
        Assert.True(filter[MongoVisitDeletionStore.PurgeScheduledForUtcPath]
            .AsBsonDocument["$exists"].AsBoolean);
    }

    [Fact]
    public void BuildPurgeFilters_ShouldAlwaysRequireBothVisitAndOwner()
    {
        BsonDocument operationFilter = Render(
            MongoVisitDeletionStore.BuildOperationPurgeFilter("visit-1", "owner-1"));
        BsonDocument occurrenceFilter = Render(
            MongoVisitDeletionStore.BuildOccurrencePurgeFilter("visit-1", "owner-1"));
        BsonDocument auditFilter = Render(
            MongoVisitDeletionStore.BuildAuditPurgeFilter("visit-1", "owner-1"));

        Assert.Equal("visit-1", operationFilter["visitId"].AsString);
        Assert.Equal("owner-1", operationFilter["userId"].AsString);
        Assert.Equal("visit-1", occurrenceFilter["visitId"].AsString);
        Assert.Equal("owner-1", occurrenceFilter["userId"].AsString);
        Assert.Equal("visit-1", auditFilter["event.visitId"].AsString);
        Assert.Equal("owner-1", auditFilter["event.userId"].AsString);
    }

    [Fact]
    public void BuildPendingAuditFilters_ShouldFenceEveryDeletionSource()
    {
        BsonDocument visitFilter = Render(
            MongoVisitDeletionStore.BuildVisitPendingAuditFilter("visit-1", "owner-1"));
        BsonDocument occurrenceFilter = Render(
            MongoVisitDeletionStore.BuildOccurrencePendingAuditFilter(
                "visit-1",
                "owner-1"));
        BsonDocument operationFilter = Render(
            MongoVisitDeletionStore.BuildOperationPendingAuditFilter(
                "visit-1",
                "owner-1"));

        Assert.Equal("visit-1", visitFilter["_id"].AsString);
        Assert.Equal("owner-1", visitFilter["userId"].AsString);
        Assert.True(HasPendingAuditMarker(visitFilter));
        Assert.Equal("visit-1", occurrenceFilter["visitId"].AsString);
        Assert.Equal("owner-1", occurrenceFilter["userId"].AsString);
        Assert.True(HasPendingAuditMarker(occurrenceFilter));
        Assert.Equal("visit-1", operationFilter["visitId"].AsString);
        Assert.Equal("owner-1", operationFilter["userId"].AsString);
        Assert.True(HasPendingAuditMarker(operationFilter));
    }

    [Fact]
    public void BuildPendingDeletionReconciliationFilter_ShouldSelectUnensuredSideEffects()
    {
        BsonDocument filter = Render(
            MongoVisitDeletionStore.BuildPendingDeletionReconciliationFilter());

        Assert.True(filter[MongoVisitDeletionStore.DeletedAtUtcPath]
            .AsBsonDocument["$exists"].AsBoolean);
        Assert.True(filter[MongoVisitDeletionStore.PurgeScheduledForUtcPath]
            .AsBsonDocument["$exists"].AsBoolean);
        Assert.Equal(0, filter["version"].AsBsonDocument["$gt"].AsInt32);
        Assert.Equal(4, filter["$or"].AsBsonArray.Count);
        string rendered = filter.ToJson();
        Assert.Contains(MongoVisitDeletionStore.ExportInvalidationEnsuredAtUtcPath, rendered);
        Assert.Contains(MongoVisitDeletionStore.PurgeJobEnsuredAtUtcPath, rendered);
    }

    [Fact]
    public void BuildTombstoneUpdate_ShouldPersistReplayAndPurgeEvidenceAndReleaseTheLease()
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 4),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            DeletedAtUtc.AddDays(-1));
        VisitDeletionTombstoneRequest request = new VisitDeletionTombstoneRequest(
            visit.Id,
            visit.UserId,
            visit.Version,
            "delete-1",
            DeletedAtUtc,
            DeletedAtUtc.AddDays(7),
            "lease-1",
            VisitDeletionAuditEventFactory.Create(visit, DeletedAtUtc));

        BsonDocument update = Render(
            MongoVisitDeletionStore.BuildTombstoneUpdate(request, "operation-hash"));
        BsonDocument set = update["$set"].AsBsonDocument;
        BsonDocument unset = update["$unset"].AsBsonDocument;

        Assert.Equal(DeletedAtUtc, set[MongoVisitDeletionStore.DeletedAtUtcPath].ToUniversalTime());
        Assert.Equal(
            DeletedAtUtc.AddDays(7),
            set[MongoVisitDeletionStore.PurgeScheduledForUtcPath].ToUniversalTime());
        Assert.Equal(
            "operation-hash",
            set[MongoVisitDeletionStore.DeletionOperationKeyHashPath].AsString);
        Assert.Equal(visit.Version + 1, set["version"].AsInt64);
        Assert.True(update["$push"].AsBsonDocument.Contains("pendingAuditEvents"));
        Assert.True(unset.Contains(UserVisitMongoDefinitions.ContentMutationLeaseTokenPath));
        Assert.True(unset.Contains(UserVisitMongoDefinitions.ContentMutationLeaseExpiresAtUtcPath));
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        RenderArgs<TDocument> arguments =
            new RenderArgs<TDocument>(serializer, BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(UpdateDefinition<UserVisitDocument> update)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(serializer, BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
    }

    private static bool HasPendingAuditMarker(BsonDocument filter)
    {
        return filter[PassportAuditMongoDefinitions.PendingEventIdPath]
            .AsBsonDocument["$exists"]
            .AsBoolean;
    }
}
