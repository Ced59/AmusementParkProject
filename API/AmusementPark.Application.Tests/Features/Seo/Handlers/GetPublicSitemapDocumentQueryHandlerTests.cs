using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.Application.Features.Seo.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Handlers;

public sealed class GetPublicSitemapDocumentQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSectionIsRequested_ShouldReadOnlyRequestedSection()
    {
        Mock<ISeoSitemapSnapshotRepository> snapshotRepository = new Mock<ISeoSitemapSnapshotRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapGenerationHistoryRepository> historyRepository = new Mock<ISeoSitemapGenerationHistoryRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapSettingsRepository> settingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        Mock<IIndexNowSubmitter> indexNowSubmitter = new Mock<IIndexNowSubmitter>(MockBehavior.Strict);
        SitemapSnapshot snapshot = new SitemapSnapshot
        {
            GeneratedAtUtc = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc),
            PublicBaseUrl = "https://example.com",
            IndexXml = "<sitemapindex />",
            Sections = new[]
            {
                new SitemapSectionStats("static-fr", "static-fr.xml", "Static FR", 1, null),
            },
            TotalUrlCount = 1,
        };

        snapshotRepository
            .Setup(repository => repository.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        snapshotRepository
            .Setup(repository => repository.GetSectionXmlAsync("static-fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("<urlset />");

        SeoSitemapGenerationOrchestrator orchestrator = new SeoSitemapGenerationOrchestrator(
            Array.Empty<ISitemapSectionProvider>(),
            new SitemapXmlWriter(),
            snapshotRepository.Object,
            historyRepository.Object,
            settingsRepository.Object,
            indexNowSubmitter.Object,
            new InMemorySeoSitemapRuntimeStateStore());
        GetPublicSitemapDocumentQueryHandler handler = new GetPublicSitemapDocumentQueryHandler(
            snapshotRepository.Object,
            orchestrator,
            new SitemapXmlWriter());

        ApplicationResult<SitemapDocumentResult> result = await handler.HandleAsync(
            new GetPublicSitemapDocumentQuery("static-fr.xml", "https://example.com", new[] { "fr" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("<urlset />", result.Value.Content);
        snapshotRepository.VerifyAll();
        historyRepository.VerifyNoOtherCalls();
        settingsRepository.VerifyNoOtherCalls();
        indexNowSubmitter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenSnapshotIsMissing_ShouldGenerateFallbackSnapshotAndReturnIndex()
    {
        SitemapSnapshot? savedSnapshot = null;
        SitemapGenerationHistoryEntry? historyEntry = null;
        Mock<ISeoSitemapSnapshotRepository> snapshotRepository = new Mock<ISeoSitemapSnapshotRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapGenerationHistoryRepository> historyRepository = new Mock<ISeoSitemapGenerationHistoryRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapSettingsRepository> settingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        Mock<IIndexNowSubmitter> indexNowSubmitter = new Mock<IIndexNowSubmitter>(MockBehavior.Strict);

        snapshotRepository
            .Setup(repository => repository.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedSnapshot);
        snapshotRepository
            .Setup(repository => repository.SaveAsync(It.IsAny<SitemapSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<SitemapSnapshot, CancellationToken>((snapshot, _) => savedSnapshot = snapshot)
            .Returns(Task.CompletedTask);
        historyRepository
            .Setup(repository => repository.WriteAsync(It.IsAny<SitemapGenerationHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Callback<SitemapGenerationHistoryEntry, CancellationToken>((entry, _) => historyEntry = entry)
            .Returns(Task.CompletedTask);
        settingsRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeoSitemapSettings());

        SeoSitemapGenerationOrchestrator orchestrator = new SeoSitemapGenerationOrchestrator(
            new[] { new FakeSitemapSectionProvider() },
            new SitemapXmlWriter(),
            snapshotRepository.Object,
            historyRepository.Object,
            settingsRepository.Object,
            indexNowSubmitter.Object,
            new InMemorySeoSitemapRuntimeStateStore());
        GetPublicSitemapDocumentQueryHandler handler = new GetPublicSitemapDocumentQueryHandler(
            snapshotRepository.Object,
            orchestrator,
            new SitemapXmlWriter());

        ApplicationResult<SitemapDocumentResult> result = await handler.HandleAsync(
            new GetPublicSitemapDocumentQuery(null, "https://example.com", new[] { "fr" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.WasGeneratedOnDemand);
        Assert.Contains("https://example.com/static-fr.xml", result.Value.Content, StringComparison.Ordinal);
        Assert.NotNull(savedSnapshot);
        Assert.NotNull(historyEntry);
        Assert.Equal(SitemapGenerationTrigger.PublicFallback, historyEntry!.Trigger);
        snapshotRepository.VerifyAll();
        historyRepository.VerifyAll();
        settingsRepository.VerifyAll();
        indexNowSubmitter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPersistedIndexUsesLegacySectionLocations_ShouldReturnRootSectionLocations()
    {
        Mock<ISeoSitemapSnapshotRepository> snapshotRepository = new Mock<ISeoSitemapSnapshotRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapGenerationHistoryRepository> historyRepository = new Mock<ISeoSitemapGenerationHistoryRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapSettingsRepository> settingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        Mock<IIndexNowSubmitter> indexNowSubmitter = new Mock<IIndexNowSubmitter>(MockBehavior.Strict);
        SitemapSnapshot snapshot = new SitemapSnapshot
        {
            GeneratedAtUtc = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc),
            PublicBaseUrl = "https://example.com",
            IndexXml = """
                <sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
                  <sitemap>
                    <loc>https://example.com/sitemaps/static-fr.xml</loc>
                    <lastmod>2026-06-20</lastmod>
                  </sitemap>
                </sitemapindex>
                """,
            Sections = new[]
            {
                new SitemapSectionStats("static-fr", "static-fr.xml", "Static FR", 1, null),
            },
            TotalUrlCount = 1,
        };

        snapshotRepository
            .Setup(repository => repository.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        SeoSitemapGenerationOrchestrator orchestrator = new SeoSitemapGenerationOrchestrator(
            Array.Empty<ISitemapSectionProvider>(),
            new SitemapXmlWriter(),
            snapshotRepository.Object,
            historyRepository.Object,
            settingsRepository.Object,
            indexNowSubmitter.Object,
            new InMemorySeoSitemapRuntimeStateStore());
        GetPublicSitemapDocumentQueryHandler handler = new GetPublicSitemapDocumentQueryHandler(
            snapshotRepository.Object,
            orchestrator,
            new SitemapXmlWriter());

        ApplicationResult<SitemapDocumentResult> result = await handler.HandleAsync(
            new GetPublicSitemapDocumentQuery(null, "https://example.com", new[] { "fr" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("https://example.com/static-fr.xml", result.Value.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("https://example.com/sitemaps/static-fr.xml", result.Value.Content, StringComparison.Ordinal);
        snapshotRepository.VerifyAll();
        historyRepository.VerifyNoOtherCalls();
        settingsRepository.VerifyNoOtherCalls();
        indexNowSubmitter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenIndexContainsLargeSection_ShouldExposeChunkLocations()
    {
        Mock<ISeoSitemapSnapshotRepository> snapshotRepository = new Mock<ISeoSitemapSnapshotRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapGenerationHistoryRepository> historyRepository = new Mock<ISeoSitemapGenerationHistoryRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapSettingsRepository> settingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        Mock<IIndexNowSubmitter> indexNowSubmitter = new Mock<IIndexNowSubmitter>(MockBehavior.Strict);
        SitemapSnapshot snapshot = new SitemapSnapshot
        {
            GeneratedAtUtc = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc),
            PublicBaseUrl = "https://example.com",
            IndexXml = "<sitemapindex />",
            Sections = new[]
            {
                new SitemapSectionStats("park-items-fr", "park-items-fr.xml", "Items FR", 401, new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc)),
            },
            TotalUrlCount = 401,
        };

        snapshotRepository
            .Setup(repository => repository.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        SeoSitemapGenerationOrchestrator orchestrator = new SeoSitemapGenerationOrchestrator(
            Array.Empty<ISitemapSectionProvider>(),
            new SitemapXmlWriter(),
            snapshotRepository.Object,
            historyRepository.Object,
            settingsRepository.Object,
            indexNowSubmitter.Object,
            new InMemorySeoSitemapRuntimeStateStore());
        GetPublicSitemapDocumentQueryHandler handler = new GetPublicSitemapDocumentQueryHandler(
            snapshotRepository.Object,
            orchestrator,
            new SitemapXmlWriter());

        ApplicationResult<SitemapDocumentResult> result = await handler.HandleAsync(
            new GetPublicSitemapDocumentQuery(null, "https://example.com", new[] { "fr" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("https://example.com/park-items-fr-1.xml", result.Value.Content, StringComparison.Ordinal);
        Assert.Contains("https://example.com/park-items-fr-2.xml", result.Value.Content, StringComparison.Ordinal);
        Assert.Contains("https://example.com/park-items-fr-3.xml", result.Value.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("https://example.com/park-items-fr.xml", result.Value.Content, StringComparison.Ordinal);
        snapshotRepository.VerifyAll();
        historyRepository.VerifyNoOtherCalls();
        settingsRepository.VerifyNoOtherCalls();
        indexNowSubmitter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenVirtualChunkIsRequested_ShouldReturnOnlyThatChunk()
    {
        Mock<ISeoSitemapSnapshotRepository> snapshotRepository = new Mock<ISeoSitemapSnapshotRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapGenerationHistoryRepository> historyRepository = new Mock<ISeoSitemapGenerationHistoryRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapSettingsRepository> settingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        Mock<IIndexNowSubmitter> indexNowSubmitter = new Mock<IIndexNowSubmitter>(MockBehavior.Strict);
        SitemapSnapshot snapshot = new SitemapSnapshot
        {
            GeneratedAtUtc = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc),
            PublicBaseUrl = "https://example.com",
            IndexXml = "<sitemapindex />",
            Sections = new[]
            {
                new SitemapSectionStats("park-items-fr", "park-items-fr.xml", "Items FR", 201, null),
            },
            TotalUrlCount = 201,
        };
        string baseXml = new SitemapXmlWriter().WriteUrlSet(
            "https://example.com",
            Enumerable.Range(1, 201)
                .Select(index => new SitemapUrlEntry($"/fr/park/item-{index:000}"))
                .ToList());

        snapshotRepository
            .Setup(repository => repository.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        snapshotRepository
            .Setup(repository => repository.GetSectionXmlAsync("park-items-fr-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        snapshotRepository
            .Setup(repository => repository.GetSectionXmlAsync("park-items-fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseXml);

        SeoSitemapGenerationOrchestrator orchestrator = new SeoSitemapGenerationOrchestrator(
            Array.Empty<ISitemapSectionProvider>(),
            new SitemapXmlWriter(),
            snapshotRepository.Object,
            historyRepository.Object,
            settingsRepository.Object,
            indexNowSubmitter.Object,
            new InMemorySeoSitemapRuntimeStateStore());
        GetPublicSitemapDocumentQueryHandler handler = new GetPublicSitemapDocumentQueryHandler(
            snapshotRepository.Object,
            orchestrator,
            new SitemapXmlWriter());

        ApplicationResult<SitemapDocumentResult> result = await handler.HandleAsync(
            new GetPublicSitemapDocumentQuery("park-items-fr-2.xml", "https://example.com", new[] { "fr" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("https://example.com/fr/park/item-201", result.Value.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("https://example.com/fr/park/item-200", result.Value.Content, StringComparison.Ordinal);
        snapshotRepository.VerifyAll();
        historyRepository.VerifyNoOtherCalls();
        settingsRepository.VerifyNoOtherCalls();
        indexNowSubmitter.VerifyNoOtherCalls();
    }

    private sealed class FakeSitemapSectionProvider : ISitemapSectionProvider
    {
        public string Key => SitemapSectionKeys.Static;

        public string FileName => "static.xml";

        public string DisplayName => "Pages statiques";

        public Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<SitemapUrlEntry> urls = new[]
            {
                new SitemapUrlEntry("/fr/home", new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc), "daily", 1.0m),
            };

            return Task.FromResult(urls);
        }
    }
}
