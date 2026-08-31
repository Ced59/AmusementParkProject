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

        BsonDocument pendingMetadataUpdate = renderedUpdate["$set"].AsBsonDocument;
        Assert.Equal("park-new", pendingMetadataUpdate["pendingParkId"].AsString);
        Assert.Equal("Attraction", pendingMetadataUpdate["pendingParkItemCategory"].AsString);
        Assert.Equal("RollerCoaster", pendingMetadataUpdate["pendingParkItemType"].AsString);
        Assert.False(pendingMetadataUpdate.Contains("parkId"));
        Assert.False(pendingMetadataUpdate.Contains("parkItemCategory"));
        Assert.False(pendingMetadataUpdate.Contains("parkItemType"));
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
            ParkId = "park-committed",
            ParkItemCategory = ParkItemCategory.Attraction,
            ParkItemType = ParkItemType.RollerCoaster,
            PendingParkId = "park-new",
            PendingParkItemCategory = ParkItemCategory.Restaurant,
            PendingParkItemType = ParkItemType.Restaurant,
        };

        RatingAggregatePendingMutation pendingMutation =
            RatingAggregateSynchronizer.ToPendingMutation(currentDocument);

        Assert.Equal(8, pendingMutation.Version);
        Assert.Equal(currentDocument.TargetType, pendingMutation.Target.TargetType);
        Assert.Equal(currentDocument.TargetId, pendingMutation.Target.TargetId);
        Assert.Equal(currentDocument.PendingParkId, pendingMutation.Target.ParkId);
        Assert.Equal(currentDocument.PendingParkItemCategory, pendingMutation.Target.ParkItemCategory);
        Assert.Equal(currentDocument.PendingParkItemType, pendingMutation.Target.ParkItemType);
    }

    [Fact]
    public void BuildCommitUpdate_WhenSnapshotSucceeds_ShouldPromoteAndClearPendingMetadata()
    {
        RatingAggregateTarget target = new RatingAggregateTarget(
            RatingTargetType.ParkItem,
            " item-1 ",
            " park-new ",
            ParkItemCategory.Restaurant,
            ParkItemType.Restaurant);
        UpdateDefinition<RatingAggregateDocument> update = RatingAggregateSynchronizer.BuildCommitUpdate(
            target,
            8,
            2,
            2,
            9d,
            4.5d,
            4.2d,
            new DateTime(2026, 7, 29, 15, 31, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 29, 15, 32, 0, DateTimeKind.Utc));
        IBsonSerializer<RatingAggregateDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<RatingAggregateDocument>();
        RenderArgs<RatingAggregateDocument> renderArguments =
            new RenderArgs<RatingAggregateDocument>(serializer, BsonSerializer.SerializerRegistry);

        BsonDocument renderedUpdate = update.Render(renderArguments).AsBsonDocument;

        BsonDocument publishedSnapshot = renderedUpdate["$set"].AsBsonDocument;
        Assert.Equal(8, publishedSnapshot["calculatedVersion"].ToInt64());
        Assert.Equal("park-new", publishedSnapshot["parkId"].AsString);
        Assert.Equal("Restaurant", publishedSnapshot["parkItemCategory"].AsString);
        Assert.Equal("Restaurant", publishedSnapshot["parkItemType"].AsString);
        Assert.Equal(2, publishedSnapshot["ratingCount"].ToInt64());
        Assert.Equal(2, publishedSnapshot["uniqueContributorCount"].ToInt64());
        BsonDocument clearedPendingMetadata = renderedUpdate["$unset"].AsBsonDocument;
        Assert.True(clearedPendingMetadata.Contains("pendingParkId"));
        Assert.True(clearedPendingMetadata.Contains("pendingParkItemCategory"));
        Assert.True(clearedPendingMetadata.Contains("pendingParkItemType"));
    }

    [Fact]
    public void AggregateSourceStages_ShouldFilterInvalidRatingsAndDeduplicateUserIds()
    {
        BsonDocument matchStage = RatingAggregateSynchronizer.BuildValidRatingSourceMatchStage();
        BsonDocument contributorGroupStage = RatingAggregateSynchronizer.BuildPerContributorGroupStage();
        BsonDocument groupStage = RatingAggregateSynchronizer.BuildAggregateValuesGroupStage();

        Assert.Contains("$isNumber", matchStage.ToJson(), StringComparison.Ordinal);
        Assert.Contains("$mod", matchStage.ToJson(), StringComparison.Ordinal);
        Assert.Contains("$convert", matchStage.ToJson(), StringComparison.Ordinal);
        Assert.Contains("$userId", matchStage.ToJson(), StringComparison.Ordinal);
        Assert.Equal("$userId", contributorGroupStage["$group"]["_id"].AsString);
        Assert.Equal(1, contributorGroupStage["$group"]["observationCount"]["$sum"].AsInt32);
        Assert.Equal("$observationCount", groupStage["$group"]["count"]["$sum"].AsString);
        Assert.Equal(1, groupStage["$group"]["uniqueContributorCount"]["$sum"].AsInt32);
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
