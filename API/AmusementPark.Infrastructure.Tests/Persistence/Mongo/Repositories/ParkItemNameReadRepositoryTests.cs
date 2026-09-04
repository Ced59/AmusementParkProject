using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkItemNameReadRepositoryTests
{
    [Fact]
    public void BuildNameProjection_ShouldOnlyReadTheIdentifierAndName()
    {
        ProjectionDefinition<ParkItemDocument> projection =
            ParkItemNameReadRepository.BuildNameProjection();
        IBsonSerializer<ParkItemDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkItemDocument>();
        BsonDocument rendered = projection.Render(
            new RenderArgs<ParkItemDocument>(serializer, BsonSerializer.SerializerRegistry));

        Assert.Equal(2, rendered.ElementCount);
        Assert.Equal(1, rendered["_id"].AsInt32);
        Assert.Equal(1, rendered["name"].AsInt32);
    }
}
