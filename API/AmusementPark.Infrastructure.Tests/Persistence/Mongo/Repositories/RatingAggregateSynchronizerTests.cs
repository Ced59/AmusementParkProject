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
