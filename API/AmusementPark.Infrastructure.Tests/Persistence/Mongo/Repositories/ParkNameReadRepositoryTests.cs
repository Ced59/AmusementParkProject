using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkNameReadRepositoryTests
{
    [Fact]
    public void BuildNameProjection_ShouldOnlyReadTheIdentifierAndName()
    {
        ProjectionDefinition<ParkDocument> projection =
            ParkNameReadRepository.BuildNameProjection();
        IBsonSerializer<ParkDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkDocument>();
        BsonDocument rendered = projection.Render(
            new RenderArgs<ParkDocument>(serializer, BsonSerializer.SerializerRegistry));

        Assert.Equal(2, rendered.ElementCount);
        Assert.Equal(1, rendered["_id"].AsInt32);
        Assert.Equal(1, rendered["name"].AsInt32);
    }
}
