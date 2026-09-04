using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportExportInvalidationTests
{
    [Fact]
    public void BuildInvalidationFilter_ShouldFenceEveryExistingUnexpiredExport()
    {
        DateTime sourceChangedAtUtc =
            new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);

        BsonDocument filter = Render(
            PassportExportRepository.BuildInvalidationFilter(
                "owner-1",
                sourceChangedAtUtc));

        Assert.Equal("owner-1", filter["userId"].AsString);
        Assert.False(filter.Contains("createdAt"));
        Assert.Equal(sourceChangedAtUtc, filter["expiresAtUtc"]["$gt"].ToUniversalTime());
    }

    private static BsonDocument Render(
        FilterDefinition<PassportExportDocument> filter)
    {
        IBsonSerializer<PassportExportDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<PassportExportDocument>();
        RenderArgs<PassportExportDocument> arguments =
            new RenderArgs<PassportExportDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
