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

public sealed class UserVisitMongoDefinitionsTests
{
    [Fact]
    public void BuildOwnedVisitFilter_ShouldRequireBothVisitAndOwner()
    {
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedVisitFilter(
                " visit-1 ",
                " user-1 ");

        BsonDocument rendered = Render(filter);

        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.True(rendered[UserVisitMongoDefinitions.DeletedAtUtcPath].IsBsonNull);
    }

    [Fact]
    public void BuildOwnedAnyStateVisitFilter_ShouldAllowDeletionAuditAndPurgeToSeeATombstone()
    {
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedAnyStateVisitFilter(
                "visit-1",
                "user-1");

        BsonDocument rendered = Render(filter);

        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.False(rendered.Contains(UserVisitMongoDefinitions.DeletedAtUtcPath));
    }

    [Fact]
    public void BuildOwnedVersionFilter_ShouldFenceAWriteByOwnerAndVersion()
    {
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedVersionFilter(
                "visit-1",
                "user-1",
                7);

        BsonDocument rendered = Render(filter);

        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(7, rendered["version"].AsInt64);
    }

    [Fact]
    public void BuildOwnedMutableVersionFilter_ShouldRejectAnActiveContentLease()
    {
        DateTime mutationAtUtc = new DateTime(2026, 9, 3, 20, 0, 0, DateTimeKind.Utc);

        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedMutableVersionFilter(
                "visit-1",
                "user-1",
                7,
                mutationAtUtc);

        BsonDocument rendered = Render(filter);

        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(7, rendered["version"].AsInt64);
        BsonArray alternatives = rendered["$or"].AsBsonArray;
        Assert.False(alternatives[0]["contentMutationLeaseToken"]["$exists"].AsBoolean);
        Assert.Equal(
            mutationAtUtc,
            alternatives[1]["contentMutationLeaseExpiresAtUtc"]["$lte"].ToUniversalTime());
    }

    [Fact]
    public void BuildOwnedLeasedVersionFilter_ShouldRequireTheExactLeaseToken()
    {
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedLeasedVersionFilter(
                "visit-1",
                "user-1",
                7,
                " lease-1 ");

        BsonDocument rendered = Render(filter);

        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(7, rendered["version"].AsInt64);
        Assert.Equal("lease-1", rendered["contentMutationLeaseToken"].AsString);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildOwnedVersionFilter_ShouldRejectANonPositiveVersion(long version)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => UserVisitMongoDefinitions.BuildOwnedVersionFilter(
                "visit-1",
                "user-1",
                version));
    }

    [Fact]
    public void BuildNewestVisitSort_ShouldKeepExactDatesBeforePartialDatesInTheirPeriod()
    {
        BsonDocument rendered = Render(UserVisitMongoDefinitions.BuildNewestVisitSort());

        Assert.Equal(-1, rendered["dateSortKey"].AsInt32);
        Assert.Equal(-1, rendered["updatedAt"].AsInt32);
        Assert.Equal(1, rendered["_id"].AsInt32);
    }

    [Fact]
    public void BuildListProjection_ShouldExcludeTheEmbeddedAssessment()
    {
        ProjectionDefinition<UserVisitDocument> projection =
            UserVisitMongoDefinitions.BuildListProjection();

        BsonDocument rendered = Render(projection);

        Assert.Equal(0, rendered["parkAssessment"].AsInt32);
        Assert.Single(rendered);
    }

    [Fact]
    public void DateSortKeyBackfill_ShouldTargetOnlyLegacyDocumentsAndUseDomainEquivalentOrder()
    {
        BsonDocument filter = Render(
            UserVisitMongoDefinitions.BuildMissingDateSortKeyFilter());
        BsonArray pipeline = Render(
            UserVisitMongoDefinitions.BuildDateSortKeyBackfillUpdate());

        Assert.False(filter["dateSortKey"]["$exists"].AsBoolean);
        BsonValue expression = pipeline[0]["$set"]["dateSortKey"];
        Assert.Equal("$date.year", expression["$add"][0]["$multiply"][0].AsString);
        Assert.Equal(10000, expression["$add"][0]["$multiply"][1].AsInt32);
        Assert.Equal("$date.month", expression["$add"][1]["$multiply"][0]["$ifNull"][0].AsString);
        Assert.Equal("$date.day", expression["$add"][2]["$ifNull"][0].AsString);
    }

    [Fact]
    public void BuildIndexes_ShouldCoverOwnerDateParkYearAndStatusAccessPaths()
    {
        CreateIndexModel<UserVisitDocument>[] indexes =
            UserVisitMongoDefinitions.BuildIndexes().ToArray();

        Assert.Equal(6, indexes.Length);
        AssertIndex(
            indexes[0],
            "idx_user_visits_user_date",
            new BsonDocument
            {
                { "userId", 1 },
                { "date.year", -1 },
                { "date.month", -1 },
                { "date.day", -1 },
            });
        AssertIndex(
            indexes[1],
            "idx_user_visits_user_park_year",
            new BsonDocument
            {
                { "userId", 1 },
                { "parkId", 1 },
                { "date.year", -1 },
            });
        AssertIndex(
            indexes[2],
            "idx_user_visits_user_status_updated",
            new BsonDocument
            {
                { "userId", 1 },
                { "status", 1 },
                { "updatedAt", -1 },
            });
        AssertIndex(
            indexes[3],
            "idx_user_visits_user_cursor",
            new BsonDocument
            {
                { "userId", 1 },
                { "dateSortKey", -1 },
                { "updatedAt", -1 },
                { "_id", 1 },
            });
        AssertIndex(
            indexes[4],
            "idx_user_visits_user_creation_operation",
            new BsonDocument
            {
                { "userId", 1 },
                { "creationOperationKeyHash", 1 },
            });
        Assert.True(indexes[4].Options.Unique);
        Assert.NotNull(indexes[4].Options.PartialFilterExpression);
        AssertIndex(
            indexes[5],
            "idx_user_visits_pending_audit",
            new BsonDocument("pendingAuditEvents.eventId", 1));
        Assert.NotNull(indexes[5].Options.PartialFilterExpression);
        Assert.All(indexes.Take(4), static index => Assert.NotEqual(true, index.Options.Unique));
    }

    [Fact]
    public void BuildListFilter_ShouldApplyOwnerFiltersAndAnExclusiveCursor()
    {
        UserVisitListCriteria criteria = new UserVisitListCriteria(
            " user-1 ",
            25,
            " park-1 ",
            2026,
            VisitStatus.Completed,
            new UserVisitListCursor(
                VisitDate.ForDay(2026, 8, 31),
                new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
                VisitId.Parse("visit-9")));

        BsonDocument rendered = Render(UserVisitMongoDefinitions.BuildListFilter(criteria));

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal("park-1", rendered["parkId"].AsString);
        Assert.Equal(2026, rendered["date.year"].AsInt32);
        Assert.Equal("Completed", rendered["status"].AsString);
        Assert.True(rendered.Contains("$or"));
        Assert.Equal(3, rendered["$or"].AsBsonArray.Count);
    }

    [Fact]
    public void MongoSettings_ShouldUseTheVersionedCollectionNameByDefault()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal("user-visits", settings.UserVisitsCollectionName);
    }

    private static void AssertIndex(
        CreateIndexModel<UserVisitDocument> index,
        string expectedName,
        BsonDocument expectedKeys)
    {
        Assert.Equal(expectedName, index.Options.Name);
        Assert.Equal(expectedKeys, Render(index.Keys));
    }

    private static BsonDocument Render(
        FilterDefinition<UserVisitDocument> filter)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(
        SortDefinition<UserVisitDocument> sort)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return sort.Render(arguments);
    }

    private static BsonDocument Render(
        ProjectionDefinition<UserVisitDocument> projection)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return projection.Render(arguments);
    }

    private static BsonArray Render(
        UpdateDefinition<UserVisitDocument> update)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonArray;
    }

    private static BsonDocument Render(
        IndexKeysDefinition<UserVisitDocument> keys)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return keys.Render(arguments);
    }
}
