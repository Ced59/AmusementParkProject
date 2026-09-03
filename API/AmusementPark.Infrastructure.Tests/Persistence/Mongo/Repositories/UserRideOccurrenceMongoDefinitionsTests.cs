using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceMongoDefinitionsTests
{
    [Fact]
    public void BuildOwnedOccurrenceByIdFilter_ShouldFenceByOccurrenceOwnerAndTombstone()
    {
        FilterDefinition<UserRideOccurrenceDocument> filter =
            UserRideOccurrenceMongoDefinitions.BuildOwnedOccurrenceByIdFilter(
                " occurrence-1 ",
                " user-1 ");

        BsonDocument rendered = Render(filter);

        Assert.Equal("occurrence-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.True(rendered["deletedAtUtc"].IsBsonNull);
        Assert.False(rendered.Contains("visitId"));
    }

    [Fact]
    public void BuildOwnedOccurrenceFilter_ShouldFenceByOccurrenceVisitOwnerAndTombstone()
    {
        FilterDefinition<UserRideOccurrenceDocument> filter =
            UserRideOccurrenceMongoDefinitions.BuildOwnedOccurrenceFilter(
                " occurrence-1 ",
                " visit-1 ",
                " user-1 ");

        BsonDocument rendered = Render(filter);

        Assert.Equal("occurrence-1", rendered["_id"].AsString);
        Assert.Equal("visit-1", rendered["visitId"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.True(rendered["deletedAtUtc"].IsBsonNull);
    }

    [Fact]
    public void BuildOwnedVersionFilter_ShouldAddOptimisticConcurrency()
    {
        FilterDefinition<UserRideOccurrenceDocument> filter =
            UserRideOccurrenceMongoDefinitions.BuildOwnedVersionFilter(
                "occurrence-1",
                "visit-1",
                "user-1",
                4);

        BsonDocument rendered = Render(filter);

        Assert.Equal(4, rendered["version"].AsInt64);
    }

    [Fact]
    public void BuildVisitOrderSort_ShouldBeDeterministicWithoutInventingATime()
    {
        BsonDocument rendered = Render(
            UserRideOccurrenceMongoDefinitions.BuildVisitOrderSort());

        Assert.Equal(1, rendered["sortPosition"].AsInt32);
        Assert.Equal(1, rendered["createdAt"].AsInt32);
        Assert.Equal(1, rendered["_id"].AsInt32);
        Assert.False(rendered.Contains("moment.localTime"));
    }

    [Fact]
    public void BuildListFilter_ShouldApplyOwnerVisitTombstoneAndExclusiveCursor()
    {
        RideOccurrenceListCriteria criteria = new RideOccurrenceListCriteria(
            VisitId.Parse("visit-1"),
            " user-1 ",
            100,
            new RideOccurrenceListCursor(
                2048,
                new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc),
                RideOccurrenceId.Parse("occurrence-9")));

        BsonDocument rendered = Render(
            UserRideOccurrenceMongoDefinitions.BuildListFilter(criteria));

        Assert.Equal("visit-1", rendered["visitId"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.True(rendered["deletedAtUtc"].IsBsonNull);
        Assert.Equal(3, rendered["$or"].AsBsonArray.Count);
    }

    [Fact]
    public void BuildIndexes_ShouldCoverEveryDocumentedAccessPath()
    {
        CreateIndexModel<UserRideOccurrenceDocument>[] indexes =
            UserRideOccurrenceMongoDefinitions.BuildIndexes().ToArray();

        Assert.Equal(8, indexes.Length);
        AssertIndex(
            indexes[0],
            "idx_user_ride_occurrences_visit_order",
            new BsonDocument
            {
                { "visitId", 1 },
                { "sortPosition", 1 },
                { "createdAt", 1 },
                { "_id", 1 },
            });
        AssertIndex(
            indexes[1],
            "idx_user_ride_occurrences_visit_fenced_order",
            new BsonDocument
            {
                { "visitId", 1 },
                { "userId", 1 },
                { "contentMutationFenceToken", 1 },
                { "sortPosition", 1 },
                { "createdAt", 1 },
                { "_id", 1 },
            });
        AssertIndex(
            indexes[2],
            "idx_user_ride_occurrences_user_item_visit",
            new BsonDocument
            {
                { "userId", 1 },
                { "parkItemId", 1 },
                { "visitId", 1 },
            });
        AssertIndex(
            indexes[3],
            "idx_user_ride_occurrences_user_park_visit",
            new BsonDocument
            {
                { "userId", 1 },
                { "parkId", 1 },
                { "visitId", 1 },
            });
        AssertIndex(
            indexes[4],
            "idx_user_ride_occurrences_visit_status",
            new BsonDocument
            {
                { "visitId", 1 },
                { "status", 1 },
            });
        AssertIndex(
            indexes[5],
            "idx_user_ride_occurrences_user_deleted",
            new BsonDocument
            {
                { "userId", 1 },
                { "deletedAtUtc", 1 },
            });
        AssertIndex(
            indexes[6],
            "idx_user_ride_occurrences_user_creation_operation_item",
            new BsonDocument
            {
                { "userId", 1 },
                { "creationOperationKeyHash", 1 },
                { "creationOperationIndex", 1 },
            });
        Assert.True(indexes[6].Options.Unique);
        Assert.NotNull(indexes[6].Options.PartialFilterExpression);
        AssertIndex(
            indexes[7],
            "idx_user_ride_occurrences_pending_audit",
            new BsonDocument("pendingAuditEvents.eventId", 1));
        Assert.NotNull(indexes[7].Options.PartialFilterExpression);
        Assert.All(indexes.Take(6), static index => Assert.NotEqual(true, index.Options.Unique));
    }

    [Fact]
    public void OperationIndexes_ShouldFenceKeysAndSerializeVisitOrderMutations()
    {
        CreateIndexModel<UserRideOccurrenceCreationOperationDocument>[] indexes =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildIndexes().ToArray();
        Assert.Equal(5, indexes.Length);
        CreateIndexModel<UserRideOccurrenceCreationOperationDocument> index = indexes[0];

        Assert.Equal(
            "idx_user_ride_occurrence_operations_user_key",
            index.Options.Name);
        Assert.True(index.Options.Unique);
        Assert.Equal(
            new BsonDocument
            {
                { "userId", 1 },
                { "operationKeyHash", 1 },
            },
            Render(index.Keys));

        CreateIndexModel<UserRideOccurrenceCreationOperationDocument> activeMutation =
            indexes[1];
        Assert.Equal(
            "idx_user_ride_occurrence_operations_active_order_mutation",
            activeMutation.Options.Name);
        Assert.True(activeMutation.Options.Unique);
        Assert.NotNull(activeMutation.Options.PartialFilterExpression);
        Assert.Equal(
            new BsonDocument("operationState", "pending"),
            Render(activeMutation.Options.PartialFilterExpression));
        Assert.Equal(
            new BsonDocument
            {
                { "userId", 1 },
                { "visitId", 1 },
            },
            Render(activeMutation.Keys));

        CreateIndexModel<UserRideOccurrenceCreationOperationDocument> normalization =
            indexes[2];
        Assert.Equal(
            "idx_user_ride_occurrence_operations_creation_normalization",
            normalization.Options.Name);
        Assert.NotEqual(true, normalization.Options.Unique);
        Assert.NotNull(normalization.Options.PartialFilterExpression);
        Assert.Equal(
            new BsonDocument(
                "relatedCreationOperationKeyHash",
                new BsonDocument("$exists", true)),
            Render(normalization.Options.PartialFilterExpression));
        Assert.Equal(
            new BsonDocument
            {
                { "userId", 1 },
                { "relatedCreationOperationKeyHash", 1 },
                { "visitId", 1 },
                { "operationState", 1 },
            },
            Render(normalization.Keys));

        Assert.Equal(
            "idx_user_ride_occurrence_operations_visit_fence_state",
            indexes[3].Options.Name);
        Assert.Equal(
            new BsonDocument
            {
                { "userId", 1 },
                { "visitId", 1 },
                { "contentMutationFenceToken", 1 },
                { "operationState", 1 },
            },
            Render(indexes[3].Keys));

        Assert.Equal(
            "idx_user_ride_occurrence_operations_pending_audit",
            indexes[4].Options.Name);
        Assert.Equal(
            new BsonDocument("pendingAuditEvents.eventId", 1),
            Render(indexes[4].Keys));
    }

    [Fact]
    public void CompletedCreationNormalizationFilter_ShouldRequireTheDurableLink()
    {
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions
                .BuildCompletedCreationNormalizationFilter(
                    " user-1 ",
                    " visit-1 ",
                    " creation-hash ");

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal("visit-1", rendered["visitId"].AsString);
        Assert.Equal(
            "creation-hash",
            rendered["relatedCreationOperationKeyHash"].AsString);
        Assert.Equal("reorder", rendered["operationKind"].AsString);
        Assert.True(rendered["wasNormalized"].AsBoolean);
        Assert.Equal("completed", rendered["operationState"].AsString);
    }

    [Fact]
    public void PendingVisitFilter_ShouldFindAnyOrderMutationForTheVisit()
    {
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildPendingVisitFilter(
                " user-1 ",
                " visit-1 ");

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal("visit-1", rendered["visitId"].AsString);
        Assert.Equal("pending", rendered["operationState"].AsString);
        Assert.False(rendered.Contains("operationKind"));
    }

    [Fact]
    public void MongoSettings_ShouldUseTheDedicatedCollectionByDefault()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal("user-ride-occurrences", settings.UserRideOccurrencesCollectionName);
        Assert.Equal(
            "user-ride-occurrence-operations",
            settings.UserRideOccurrenceOperationsCollectionName);
    }

    private static void AssertIndex(
        CreateIndexModel<UserRideOccurrenceDocument> index,
        string expectedName,
        BsonDocument expectedKeys)
    {
        Assert.Equal(expectedName, index.Options.Name);
        Assert.Equal(expectedKeys, Render(index.Keys));
    }

    private static BsonDocument Render(
        FilterDefinition<UserRideOccurrenceDocument> filter)
    {
        IBsonSerializer<UserRideOccurrenceDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserRideOccurrenceDocument>();
        RenderArgs<UserRideOccurrenceDocument> arguments =
            new RenderArgs<UserRideOccurrenceDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(
        SortDefinition<UserRideOccurrenceDocument> sort)
    {
        IBsonSerializer<UserRideOccurrenceDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserRideOccurrenceDocument>();
        RenderArgs<UserRideOccurrenceDocument> arguments =
            new RenderArgs<UserRideOccurrenceDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return sort.Render(arguments);
    }

    private static BsonDocument Render(
        IndexKeysDefinition<UserRideOccurrenceDocument> keys)
    {
        IBsonSerializer<UserRideOccurrenceDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserRideOccurrenceDocument>();
        RenderArgs<UserRideOccurrenceDocument> arguments =
            new RenderArgs<UserRideOccurrenceDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return keys.Render(arguments);
    }

    private static BsonDocument Render(
        IndexKeysDefinition<UserRideOccurrenceCreationOperationDocument> keys)
    {
        IBsonSerializer<UserRideOccurrenceCreationOperationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<
                UserRideOccurrenceCreationOperationDocument>();
        RenderArgs<UserRideOccurrenceCreationOperationDocument> arguments =
            new RenderArgs<UserRideOccurrenceCreationOperationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return keys.Render(arguments);
    }

    private static BsonDocument Render(
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter)
    {
        IBsonSerializer<UserRideOccurrenceCreationOperationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<
                UserRideOccurrenceCreationOperationDocument>();
        RenderArgs<UserRideOccurrenceCreationOperationDocument> arguments =
            new RenderArgs<UserRideOccurrenceCreationOperationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
