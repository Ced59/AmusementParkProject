using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class FactualChangeOutboxRepositoryTests
{
    [Fact]
    public void BuildPendingFilter_ShouldMatchTheCompoundIndexPrefix()
    {
        FilterDefinition<FactualChangeOutboxDocument> filter =
            FactualChangeOutboxRepository.BuildPendingFilter();

        Assert.Equal(
            new BsonDocument("materializedAtUtc", BsonNull.Value),
            Render(filter));
    }

    private static BsonDocument Render(FilterDefinition<FactualChangeOutboxDocument> filter)
    {
        IBsonSerializer<FactualChangeOutboxDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<FactualChangeOutboxDocument>();
        RenderArgs<FactualChangeOutboxDocument> arguments =
            new RenderArgs<FactualChangeOutboxDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
