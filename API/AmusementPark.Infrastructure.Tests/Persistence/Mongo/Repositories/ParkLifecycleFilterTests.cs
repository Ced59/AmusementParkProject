using AmusementPark.Application.Common.Requests;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkLifecycleFilterTests
{
    [Fact]
    public void BuildClosedFilter_WhenOpenOnly_ShouldMatchOperatingExactly()
    {
        BsonDocument filter = Render(ParkRepository.BuildClosedFilter(ClosedEntityFilter.OpenOnly));

        Assert.Equal("Operating", filter["status"].AsString);
    }

    [Fact]
    public void BuildClosedFilter_WhenClosedOnly_ShouldMatchDefinitivelyClosedExactly()
    {
        BsonDocument filter = Render(ParkRepository.BuildClosedFilter(ClosedEntityFilter.ClosedOnly));

        Assert.Equal("ClosedDefinitively", filter["status"].AsString);
    }

    [Fact]
    public void BuildClosedFilter_WhenAll_ShouldNotFilterLifecycleStatus()
    {
        BsonDocument filter = Render(ParkRepository.BuildClosedFilter(ClosedEntityFilter.All));

        Assert.Empty(filter);
    }

    private static BsonDocument Render(FilterDefinition<ParkDocument> filter)
    {
        IBsonSerializer<ParkDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<ParkDocument>();
        return filter.Render(new RenderArgs<ParkDocument>(serializer, BsonSerializer.SerializerRegistry));
    }
}
