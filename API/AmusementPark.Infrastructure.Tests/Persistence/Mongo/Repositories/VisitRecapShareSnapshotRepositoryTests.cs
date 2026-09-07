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
    public void BuildIndexes_ShouldSupportPublicationCleanupWithoutACollectionScan()
    {
        CreateIndexModel<VisitRecapShareSnapshotDocument> index = Assert.Single(
            VisitRecapShareSnapshotMongoDefinitions.BuildIndexes());
        IBsonSerializer<VisitRecapShareSnapshotDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<VisitRecapShareSnapshotDocument>();
        BsonDocument rendered = index.Keys.Render(
            new RenderArgs<VisitRecapShareSnapshotDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));

        Assert.Equal(
            VisitRecapShareSnapshotMongoDefinitions.PublicationVersionIndexName,
            index.Options.Name);
        Assert.Equal(
            new BsonDocument
            {
                { "publicationId", 1 },
                { "publicationVersion", 1 },
            },
            rendered);
        Assert.Null(index.Options.ExpireAfter);
    }

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
