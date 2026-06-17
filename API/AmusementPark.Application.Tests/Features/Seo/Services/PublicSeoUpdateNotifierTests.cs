using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class PublicSeoUpdateNotifierTests
{
    [Fact]
    public async Task ResolveAsync_WhenPublicItemSlugChanges_ShouldReturnOldAndNewItemUrls()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateEmptyImageRepository();

        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Magic Park",
                    IsVisible = true,
                    AdminReviewStatus = AdminReviewStatus.Validated,
                },
            });
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());

        PublicSeoUrlResolver resolver = new PublicSeoUrlResolver(
            parkItemRepository.Object,
            parkRepository.Object,
            parkZoneRepository.Object,
            imageRepository.Object);

        IReadOnlyCollection<string> urls = await resolver.ResolveAsync(
            new PublicSeoUpdate
            {
                PreviousParkItems = new[]
                {
                    new PublicSeoParkItemSnapshot("item-1", "park-1", null, "Old Name", true, AdminReviewStatus.Validated, null),
                },
                CurrentParkItems = new[]
                {
                    new PublicSeoParkItemSnapshot("item-1", "park-1", null, "New Name", true, AdminReviewStatus.Validated, null),
                },
            },
            new[] { "fr" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park", urls);
        Assert.Contains("/fr/park/park-1/magic-park/items", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/old-name", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/new-name", urls);
        Assert.DoesNotContain(urls, static url => url.EndsWith("/images", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("/fr/parks", urls);
        parkRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        imageRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NotifyAsync_WhenIndexNowIsEnabled_ShouldScheduleSitemapRefreshAndSubmitImpactedUrls()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateEmptyImageRepository();
        Mock<IPublicSeoContextProvider> contextProvider = new Mock<IPublicSeoContextProvider>(MockBehavior.Strict);
        Mock<ISeoSitemapSettingsRepository> settingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        Mock<IIndexNowSubmitter> indexNowSubmitter = new Mock<IIndexNowSubmitter>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> refreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);

        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Magic Park",
                    IsVisible = true,
                    AdminReviewStatus = AdminReviewStatus.Validated,
                },
            });
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        contextProvider
            .Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicSeoContext("https://example.com/", new[] { "fr" }));
        settingsRepository
            .Setup(repository => repository.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SeoSitemapSettings
            {
                IsIndexNowEnabled = true,
                IndexNowKey = "key",
                IndexNowEndpoints = new[] { "https://api.indexnow.org/indexnow" },
            });
        refreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        indexNowSubmitter
            .Setup(submitter => submitter.SubmitAsync(
                It.Is<SeoSitemapSettings>(settings => settings.IsIndexNowEnabled),
                "https://example.com",
                It.Is<IReadOnlyCollection<string>>(urls =>
                    urls.Contains("https://example.com/fr/park/park-1/magic-park") &&
                    urls.Contains("https://example.com/fr/park/park-1/magic-park/items") &&
                    urls.Contains("https://example.com/fr/park/park-1/magic-park/item/item-1/new-name")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexNowSubmissionResult { WasRequested = true, IsEnabled = true, IsSuccess = true, SubmittedUrlCount = 3 });

        PublicSeoUrlResolver resolver = new PublicSeoUrlResolver(
            parkItemRepository.Object,
            parkRepository.Object,
            parkZoneRepository.Object,
            imageRepository.Object);
        PublicSeoUpdateNotifier notifier = new PublicSeoUpdateNotifier(
            resolver,
            contextProvider.Object,
            settingsRepository.Object,
            indexNowSubmitter.Object,
            refreshScheduler.Object);

        await notifier.NotifyAsync(
            new PublicSeoUpdate
            {
                CurrentParkItems = new[]
                {
                    new PublicSeoParkItemSnapshot("item-1", "park-1", null, "New Name", true, AdminReviewStatus.Validated, null),
                },
            },
            CancellationToken.None);

        contextProvider.VerifyAll();
        parkRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        imageRepository.VerifyAll();
        settingsRepository.VerifyAll();
        refreshScheduler.VerifyAll();
        indexNowSubmitter.VerifyAll();
    }

    private static Mock<IImageRepository> CreateEmptyImageRepository()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                1,
                It.IsAny<ImageSearchCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(Array.Empty<Image>(), 1, 1, 0));

        return imageRepository;
    }
}
