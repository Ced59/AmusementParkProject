using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class VisitTargetReadRepositoryTests
{
    [Fact]
    public void BuildProjection_ShouldOnlyReadFieldsRequiredByVisitRules()
    {
        ProjectionDefinition<ParkItemDocument> projection =
            VisitTargetReadRepository.BuildProjection();
        IBsonSerializer<ParkItemDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkItemDocument>();
        BsonDocument rendered = projection.Render(
            new RenderArgs<ParkItemDocument>(serializer, BsonSerializer.SerializerRegistry));

        Assert.Equal(8, rendered.ElementCount);
        Assert.Equal(1, rendered["_id"].AsInt32);
        Assert.Equal(1, rendered["parkId"].AsInt32);
        Assert.Equal(1, rendered["name"].AsInt32);
        Assert.Equal(1, rendered["category"].AsInt32);
        Assert.Equal(1, rendered["isVisible"].AsInt32);
        Assert.Equal(1, rendered["attractionDetails.openingDate"].AsInt32);
        Assert.Equal(1, rendered["attractionDetails.closingDate"].AsInt32);
        Assert.Equal(1, rendered["attractionDetails.status"].AsInt32);
    }
}
