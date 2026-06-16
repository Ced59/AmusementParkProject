using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class SeoSitemapSnapshotRepositoryTests
{
    [Fact]
    public async Task GetLatestAsync_AfterSaveAndMemoryCompaction_ShouldReturnCachedSnapshot()
    {
        Mock<IMongoCollection<SeoSitemapSnapshotDocument>> collection = new Mock<IMongoCollection<SeoSitemapSnapshotDocument>>(MockBehavior.Strict);
        collection
            .Setup(value => value.ReplaceOneAsync(
                It.IsAny<FilterDefinition<SeoSitemapSnapshotDocument>>(),
                It.IsAny<SeoSitemapSnapshotDocument>(),
                It.IsAny<ReplaceOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<ReplaceOneResult>());

        MongoDbSettings settings = new MongoDbSettings();
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database
            .Setup(value => value.GetCollection<SeoSitemapSnapshotDocument>(
                settings.SeoSitemapSnapshotsCollectionName,
                null))
            .Returns(collection.Object);

        using MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
        SeoSitemapSnapshotRepository repository = new SeoSitemapSnapshotRepository(database.Object, settings, cache);
        SitemapSnapshot snapshot = new SitemapSnapshot
        {
            GeneratedAtUtc = new DateTime(2026, 6, 16, 8, 0, 0, DateTimeKind.Utc),
            PublicBaseUrl = "https://example.com",
            IndexXml = "<sitemapindex />",
            SectionXmlByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["static-fr"] = "<urlset />",
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

        Assert.Same(snapshot, result);
        collection.VerifyAll();
        database.VerifyAll();
    }
}
