using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class SeoSitemapSnapshotRepositoryTests
{
    [Fact]
    public async Task SaveAsync_WhenSectionXmlIsLarge_ShouldStoreChunkedSectionsOutsideSnapshotDocument()
    {
        SeoSitemapSnapshotDocument? savedSnapshotDocument = null;
        List<SeoSitemapSnapshotSectionChunkDocument> insertedChunks = new List<SeoSitemapSnapshotSectionChunkDocument>();
        Mock<IMongoCollection<SeoSitemapSnapshotDocument>> snapshotsCollection = new Mock<IMongoCollection<SeoSitemapSnapshotDocument>>(MockBehavior.Strict);
        snapshotsCollection
            .Setup(value => value.ReplaceOneAsync(
                It.IsAny<FilterDefinition<SeoSitemapSnapshotDocument>>(),
                It.IsAny<SeoSitemapSnapshotDocument>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<SeoSitemapSnapshotDocument>, SeoSitemapSnapshotDocument, ReplaceOptions, CancellationToken>((_, document, _, _) => savedSnapshotDocument = document)
            .ReturnsAsync(Mock.Of<ReplaceOneResult>());

        Mock<IMongoCollection<SeoSitemapSnapshotSectionChunkDocument>> sectionChunksCollection = new Mock<IMongoCollection<SeoSitemapSnapshotSectionChunkDocument>>(MockBehavior.Strict);
        sectionChunksCollection
            .Setup(value => value.InsertManyAsync(
                It.IsAny<IEnumerable<SeoSitemapSnapshotSectionChunkDocument>>(),
                It.IsAny<InsertManyOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<SeoSitemapSnapshotSectionChunkDocument>, InsertManyOptions?, CancellationToken>((documents, _, _) => insertedChunks.AddRange(documents))
            .Returns(Task.CompletedTask);
        sectionChunksCollection
            .Setup(value => value.DeleteManyAsync(
                It.IsAny<FilterDefinition<SeoSitemapSnapshotSectionChunkDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<DeleteResult>());

        MongoDbSettings settings = new MongoDbSettings();
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database
            .Setup(value => value.GetCollection<SeoSitemapSnapshotDocument>(
                settings.SeoSitemapSnapshotsCollectionName,
                null))
            .Returns(snapshotsCollection.Object);
        database
            .Setup(value => value.GetCollection<SeoSitemapSnapshotSectionChunkDocument>(
                settings.SeoSitemapSnapshotSectionsCollectionName,
                null))
            .Returns(sectionChunksCollection.Object);

        using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        SeoSitemapSnapshotRepository repository = new SeoSitemapSnapshotRepository(database.Object, settings, cache, Mock.Of<ILogger<SeoSitemapSnapshotRepository>>());
        string largeSectionXml = new string('x', (2 * 1024 * 1024) - 1) + char.ConvertFromUtf32(0x1F600) + "tail";
        SitemapSnapshot snapshot = new SitemapSnapshot
        {
            GeneratedAtUtc = new DateTime(2026, 6, 16, 8, 0, 0, DateTimeKind.Utc),
            PublicBaseUrl = "https://example.com",
            IndexXml = "<sitemapindex />",
            SectionXmlByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["static-fr"] = largeSectionXml,
            },
            Sections = new[]
            {
                new SitemapSectionStats("static-fr", "static-fr.xml", "Static FR", 1, null),
            },
            TotalUrlCount = 1,
        };

        await repository.SaveAsync(snapshot, CancellationToken.None);
        cache.Compact(1.0);

        SitemapSnapshot? result = await repository.GetLatestAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(snapshot.GeneratedAtUtc, result.GeneratedAtUtc);
        Assert.Empty(result.SectionXmlByKey);
        Assert.NotNull(savedSnapshotDocument);
        Assert.Null(savedSnapshotDocument.SectionXmlByKey);
        Assert.False(string.IsNullOrWhiteSpace(savedSnapshotDocument.SectionsStorageId));
        Assert.True(insertedChunks.Count > 1);
        Assert.All(insertedChunks, chunk => Assert.True(chunk.XmlChunk.Length <= 2 * 1024 * 1024));
        Assert.Equal(largeSectionXml, string.Concat(insertedChunks.OrderBy(static chunk => chunk.ChunkIndex).Select(static chunk => chunk.XmlChunk)));
        snapshotsCollection.VerifyAll();
        sectionChunksCollection.VerifyAll();
        database.VerifyAll();
    }
}
