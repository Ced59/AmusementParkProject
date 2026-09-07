using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class VisitRecapShareSnapshotRepositoryTests
{
    [Fact]
    public void BuildSupersededFilter_ShouldKeepThePublishedAndNewerVersions()
    {
        FilterDefinition<VisitRecapShareSnapshotDocument> filter =
            VisitRecapShareSnapshotRepository.BuildSupersededFilter(
                SharePublicationId.Parse("publication-1"),
                4);

        IBsonSerializer<VisitRecapShareSnapshotDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<VisitRecapShareSnapshotDocument>();
        BsonDocument rendered = filter.Render(
            new RenderArgs<VisitRecapShareSnapshotDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));

        Assert.Equal("publication-1", rendered["publicationId"].AsString);
        Assert.Equal(4, rendered["publicationVersion"].AsBsonDocument["$lt"].AsInt64);
    }
}
