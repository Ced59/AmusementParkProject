using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class RatingAggregateSynchronizerTests
{
    [Fact]
    public void BuildReserveUpdate_WhenMetadataChanges_ShouldAssociateItWithTheNewVersion()
    {
        DateTime nowUtc = new DateTime(2026, 7, 29, 15, 30, 0, DateTimeKind.Utc);
        RatingAggregateTarget target = new RatingAggregateTarget(
            RatingTargetType.ParkItem,
            " item-1 ",
            " park-new ",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster);
        UpdateDefinition<RatingAggregateDocument> update =
            RatingAggregateSynchronizer.BuildReserveUpdate(target, nowUtc);
        IBsonSerializer<RatingAggregateDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<RatingAggregateDocument>();
        RenderArgs<RatingAggregateDocument> renderArguments =
            new RenderArgs<RatingAggregateDocument>(serializer, BsonSerializer.SerializerRegistry);

        BsonDocument renderedUpdate = update.Render(renderArguments).AsBsonDocument;

        BsonDocument metadataUpdate = renderedUpdate["$set"].AsBsonDocument;
        Assert.Equal(nameof(RatingTargetType.ParkItem), metadataUpdate["targetType"].AsString);
        Assert.Equal("item-1", metadataUpdate["targetId"].AsString);
        Assert.Equal("park-new", metadataUpdate["parkId"].AsString);
        Assert.Equal("Attraction", metadataUpdate["parkItemCategory"].AsString);
        Assert.Equal("RollerCoaster", metadataUpdate["parkItemType"].AsString);
        Assert.Equal(1, renderedUpdate["$inc"]["mutationVersion"].ToInt64());
        Assert.False(renderedUpdate["$setOnInsert"].AsBsonDocument.Contains("parkId"));
    }

    [Fact]
    public void ToPendingMutation_WhenWorkerAdoptsNewerVersion_ShouldAdoptItsMetadata()
    {
        RatingAggregateDocument currentDocument = new RatingAggregateDocument
        {
            MutationVersion = 8,
            TargetType = RatingTargetType.ParkItem,
            TargetId = "item-1",
            ParkId = "park-new",
            ParkItemCategory = ParkItemCategory.Restaurant,
            ParkItemType = ParkItemType.Restaurant,
        };

        RatingAggregatePendingMutation pendingMutation =
            RatingAggregateSynchronizer.ToPendingMutation(currentDocument);

        Assert.Equal(8, pendingMutation.Version);
        Assert.Equal(currentDocument.TargetType, pendingMutation.Target.TargetType);
        Assert.Equal(currentDocument.TargetId, pendingMutation.Target.TargetId);
        Assert.Equal(currentDocument.ParkId, pendingMutation.Target.ParkId);
        Assert.Equal(currentDocument.ParkItemCategory, pendingMutation.Target.ParkItemCategory);
        Assert.Equal(currentDocument.ParkItemType, pendingMutation.Target.ParkItemType);
    }

    [Fact]
    public void BuildCommitFilter_WhenSnapshotIsStale_ShouldRequireTheCurrentPendingVersion()
    {
        FilterDefinition<RatingAggregateDocument> filter = RatingAggregateSynchronizer.BuildCommitFilter(
            RatingTargetType.ParkItem,
            " item-1 ",
            7);
        IBsonSerializer<RatingAggregateDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<RatingAggregateDocument>();
        RenderArgs<RatingAggregateDocument> renderArguments =
            new RenderArgs<RatingAggregateDocument>(serializer, BsonSerializer.SerializerRegistry);

        BsonDocument renderedFilter = filter.Render(renderArguments);

        Assert.Equal(nameof(RatingTargetType.ParkItem), renderedFilter["targetType"].AsString);
        Assert.Equal("item-1", renderedFilter["targetId"].AsString);
        Assert.Equal(7, renderedFilter["mutationVersion"].ToInt64());
        BsonArray calculatedVersionAlternatives = renderedFilter["$or"].AsBsonArray;
        Assert.Contains(
            calculatedVersionAlternatives,
            static alternative =>
                alternative["calculatedVersion"].AsBsonDocument.Contains("$exists")
                && alternative["calculatedVersion"].AsBsonDocument["$exists"].AsBoolean == false);
        Assert.Contains(
            calculatedVersionAlternatives,
            static alternative =>
                alternative["calculatedVersion"].AsBsonDocument.Contains("$lt")
                && alternative["calculatedVersion"].AsBsonDocument["$lt"].ToInt64() == 7);
    }
}
