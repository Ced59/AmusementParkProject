using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class SeoSitemapGenerationOrchestratorTests
{
    [Fact]
    public async Task GenerateAsync_WhenImageSectionsExist_ShouldPersistThemAndReferenceThemInSitemapIndex()
    {
        ISitemapSectionProvider[] providers = new ISitemapSectionProvider[]
        {
            new FakeSitemapSectionProvider(
                SitemapSectionKeys.ParkImages,
                "park-images.xml",
                "Images de parcs",
                new[] { new SitemapUrlEntry("/fr/park/park-1/visible-park/images", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)) }),
            new FakeSitemapSectionProvider(
                SitemapSectionKeys.ParkItemImages,
                "park-item-images.xml",
                "Images d'elements de parc",
                new[] { new SitemapUrlEntry("/fr/park/park-1/visible-park/item/item-1/visible-item/images", new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc)) }),
        };
        SitemapSnapshot? savedSnapshot = null;
        Mock<ISeoSitemapSnapshotRepository> snapshotRepository = new Mock<ISeoSitemapSnapshotRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapGenerationHistoryRepository> historyRepository = new Mock<ISeoSitemapGenerationHistoryRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapSettingsRepository> settingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        Mock<IIndexNowSubmitter> indexNowSubmitter = new Mock<IIndexNowSubmitter>(MockBehavior.Strict);

        snapshotRepository
            .Setup(repository => repository.SaveAsync(It.IsAny<SitemapSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<SitemapSnapshot, CancellationToken>((snapshot, _) => savedSnapshot = snapshot)
            .Returns(Task.CompletedTask);
        historyRepository
            .Setup(repository => repository.WriteAsync(It.IsAny<SitemapGenerationHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        settingsRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeoSitemapSettings());

        SeoSitemapGenerationOrchestrator orchestrator = new SeoSitemapGenerationOrchestrator(
            providers,
            new SitemapXmlWriter(),
            snapshotRepository.Object,
            historyRepository.Object,
            settingsRepository.Object,
            indexNowSubmitter.Object,
            new InMemorySeoSitemapRuntimeStateStore());

        SitemapGenerationResult result = await orchestrator.GenerateAsync(
            "https://example.com/",
            new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } },
            SitemapGenerationTrigger.Manual,
            submitToIndexNow: false,
            triggeredByUserId: "admin-1",
            triggeredByUserEmail: "admin@example.com",
            CancellationToken.None);

        Assert.Equal(SitemapGenerationStatus.Succeeded, result.Status);
        Assert.NotNull(savedSnapshot);
        SitemapSnapshot snapshot = savedSnapshot!;
        Assert.Contains(snapshot.Sections, static section => section.Key == "park-images-fr" && section.FileName == "park-images-fr.xml");
        Assert.Contains(snapshot.Sections, static section => section.Key == "park-item-images-fr" && section.FileName == "park-item-images-fr.xml");
        Assert.Contains("https://example.com/sitemaps/park-images-fr.xml", snapshot.IndexXml, StringComparison.Ordinal);
        Assert.Contains("https://example.com/sitemaps/park-item-images-fr.xml", snapshot.IndexXml, StringComparison.Ordinal);
        Assert.True(snapshot.SectionXmlByKey.ContainsKey("park-images-fr"));
        Assert.True(snapshot.SectionXmlByKey.ContainsKey("park-item-images-fr"));
        snapshotRepository.VerifyAll();
        historyRepository.VerifyAll();
        settingsRepository.VerifyAll();
        indexNowSubmitter.VerifyNoOtherCalls();
    }

    private sealed class FakeSitemapSectionProvider : ISitemapSectionProvider
    {
        private readonly IReadOnlyCollection<SitemapUrlEntry> urls;

        public FakeSitemapSectionProvider(string key, string fileName, string displayName, IReadOnlyCollection<SitemapUrlEntry> urls)
        {
            this.Key = key;
            this.FileName = fileName;
            this.DisplayName = displayName;
            this.urls = urls;
        }

        public string Key { get; }

        public string FileName { get; }

        public string DisplayName { get; }

        public Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.urls);
        }
    }
}
