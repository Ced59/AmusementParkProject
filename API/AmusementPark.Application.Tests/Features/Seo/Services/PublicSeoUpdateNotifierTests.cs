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
using AmusementPark.Core.Domain.Videos;
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
    public async Task ResolveAsync_WhenPublicItemHasPublishedImage_ShouldReturnParentParkAndItemImageUrls()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository(CreateImage("image-item-1", ImageOwnerType.ParkItem, "item-1", ImageCategory.ParkItem));

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
                CurrentParkItems = new[]
                {
                    new PublicSeoParkItemSnapshot("item-1", "park-1", null, "New Name", true, AdminReviewStatus.Validated, null),
                },
            },
            new[] { "fr" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park/images", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/new-name/images", urls);
        parkRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        imageRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenPublicParkHasLifecycleDate_ShouldReturnParkHistoryUrl()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateEmptyImageRepository();

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
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
                CurrentParks = new[]
                {
                    new PublicSeoParkSnapshot(
                        "park-1",
                        "Magic Park",
                        true,
                        ParkStatus.Operating,
                        AdminReviewStatus.Validated,
                        null,
                        OpeningDate: new DateTime(1987, 5, 20)),
                },
            },
            new[] { "fr" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park", urls);
        Assert.Contains("/fr/park/park-1/magic-park/history", urls);
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_WhenParkUpdateHasClosedHistoryItemWithLifecycleDate_ShouldReturnItemHistoryUrls()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateEmptyImageRepository();

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ParkItem
                {
                    Id = "item-1",
                    ParkId = "park-1",
                    Name = "Closed Ride",
                    IsVisible = true,
                    AdminReviewStatus = AdminReviewStatus.Validated,
                    AttractionDetails = new AttractionDetails
                    {
                        Status = ParkItemStatusNormalizer.ClosedDefinitively,
                        OpeningDateText = "juin 2020",
                    },
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
                CurrentParks = new[]
                {
                    new PublicSeoParkSnapshot(
                        "park-1",
                        "Magic Park",
                        true,
                        ParkStatus.Operating,
                        AdminReviewStatus.Validated,
                        null),
                },
            },
            new[] { "fr" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park/history", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/closed-ride/history", urls);
        Assert.DoesNotContain("/fr/park/park-1/magic-park/item/item-1/closed-ride", urls);
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_WhenClosedPublicParkHasLifecycleDate_ShouldReturnHistoryUrlOnly()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
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
                CurrentParks = new[]
                {
                    new PublicSeoParkSnapshot(
                        "park-1",
                        "Closed Park",
                        true,
                        ParkStatus.ClosedDefinitively,
                        AdminReviewStatus.Validated,
                        null,
                        OpeningDate: new DateTime(1987, 5, 20)),
                },
            },
            new[] { "fr" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/closed-park/history", urls);
        Assert.DoesNotContain("/fr/park/park-1/closed-park", urls);
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenPublicItemHasLifecycleDate_ShouldReturnItemAndParentParkHistoryUrls()
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
                CurrentParkItems = new[]
                {
                    new PublicSeoParkItemSnapshot(
                        "item-1",
                        "park-1",
                        null,
                        "Big Coaster",
                        true,
                        AdminReviewStatus.Validated,
                        null,
                        OpeningDate: new DateTime(2001, 4, 1)),
                },
            },
            new[] { "fr" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park/history", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/big-coaster/history", urls);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        parkZoneRepository.VerifyAll();
        imageRepository.VerifyAll();
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

    [Fact]
    public async Task NotifyAsync_WhenSitemapRefreshIsSuppressed_ShouldSubmitImpactedUrlsWithoutSchedulingRefresh()
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
                SuppressSitemapRefresh = true,
            },
            CancellationToken.None);

        contextProvider.VerifyAll();
        parkRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        imageRepository.VerifyAll();
        settingsRepository.VerifyAll();
        refreshScheduler.VerifyNoOtherCalls();
        indexNowSubmitter.VerifyAll();
    }

    [Fact]
    public async Task NotifyAsync_WhenNoIndexNowUrlIsResolved_ShouldStillScheduleSitemapRefresh()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IPublicSeoContextProvider> contextProvider = new Mock<IPublicSeoContextProvider>(MockBehavior.Strict);
        Mock<ISeoSitemapSettingsRepository> settingsRepository = new Mock<ISeoSitemapSettingsRepository>(MockBehavior.Strict);
        Mock<IIndexNowSubmitter> indexNowSubmitter = new Mock<IIndexNowSubmitter>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> refreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);

        contextProvider
            .Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicSeoContext("https://example.com/", new[] { "fr" }));
        refreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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

        await notifier.NotifyAsync(new PublicSeoUpdate(), CancellationToken.None);

        contextProvider.VerifyAll();
        refreshScheduler.VerifyAll();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
        parkZoneRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
        settingsRepository.VerifyNoOtherCalls();
        indexNowSubmitter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenPublishedParkVideoChanges_ShouldReturnParkVideoListAndWatchUrls()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

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

        PublicSeoUrlResolver resolver = new PublicSeoUrlResolver(
            parkItemRepository.Object,
            parkRepository.Object,
            parkZoneRepository.Object,
            imageRepository.Object);

        IReadOnlyCollection<string> urls = await resolver.ResolveAsync(
            new PublicSeoUpdate
            {
                CurrentVideos = new[]
                {
                    new PublicSeoVideoSnapshot("video-1", VideoOwnerType.Park, "park-1", "Front Row Ride", true, null),
                },
            },
            new[] { "fr" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park", urls);
        Assert.Contains("/fr/park/park-1/magic-park/videos", urls);
        Assert.Contains("/fr/park/park-1/magic-park/videos/video-1/front-row-ride", urls);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        parkZoneRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenPublishedParkVideoTargetsOneLanguage_ShouldReturnOnlyMatchingVideoUrls()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

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

        PublicSeoUrlResolver resolver = new PublicSeoUrlResolver(
            parkItemRepository.Object,
            parkRepository.Object,
            parkZoneRepository.Object,
            imageRepository.Object);

        IReadOnlyCollection<string> urls = await resolver.ResolveAsync(
            new PublicSeoUpdate
            {
                CurrentVideos = new[]
                {
                    new PublicSeoVideoSnapshot("video-1", VideoOwnerType.Park, "park-1", "Front Row Ride", true, null)
                    {
                        LanguageCodes = new[] { "fr" },
                    },
                },
            },
            new[] { "fr", "en" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park/videos", urls);
        Assert.Contains("/fr/park/park-1/magic-park/videos/video-1/front-row-ride", urls);
        Assert.DoesNotContain("/en/park/park-1/magic-park/videos", urls);
        Assert.DoesNotContain("/en/park/park-1/magic-park/videos/video-1/front-row-ride", urls);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        parkZoneRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenPublishedParkItemVideoChanges_ShouldReturnParkItemVideoListAndWatchUrls()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ParkItem
                {
                    Id = "item-1",
                    ParkId = "park-1",
                    Name = "Big Coaster",
                    IsVisible = true,
                    AdminReviewStatus = AdminReviewStatus.Validated,
                },
            });
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

        PublicSeoUrlResolver resolver = new PublicSeoUrlResolver(
            parkItemRepository.Object,
            parkRepository.Object,
            parkZoneRepository.Object,
            imageRepository.Object);

        IReadOnlyCollection<string> urls = await resolver.ResolveAsync(
            new PublicSeoUpdate
            {
                CurrentVideos = new[]
                {
                    new PublicSeoVideoSnapshot("video-1", VideoOwnerType.ParkItem, "item-1", "Front Row Ride", true, null),
                },
            },
            new[] { "fr" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/big-coaster", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/big-coaster/videos", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/big-coaster/videos/video-1/front-row-ride", urls);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenPublishedParkItemVideoTargetsOneLanguage_ShouldReturnOnlyMatchingVideoUrls()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ParkItem
                {
                    Id = "item-1",
                    ParkId = "park-1",
                    Name = "Big Coaster",
                    IsVisible = true,
                    AdminReviewStatus = AdminReviewStatus.Validated,
                },
            });
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

        PublicSeoUrlResolver resolver = new PublicSeoUrlResolver(
            parkItemRepository.Object,
            parkRepository.Object,
            parkZoneRepository.Object,
            imageRepository.Object);

        IReadOnlyCollection<string> urls = await resolver.ResolveAsync(
            new PublicSeoUpdate
            {
                CurrentVideos = new[]
                {
                    new PublicSeoVideoSnapshot("video-1", VideoOwnerType.ParkItem, "item-1", "Front Row Ride", true, null)
                    {
                        LanguageCodes = new[] { "fr" },
                    },
                },
            },
            new[] { "fr", "en" },
            CancellationToken.None);

        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/big-coaster/videos", urls);
        Assert.Contains("/fr/park/park-1/magic-park/item/item-1/big-coaster/videos/video-1/front-row-ride", urls);
        Assert.DoesNotContain("/en/park/park-1/magic-park/item/item-1/big-coaster/videos", urls);
        Assert.DoesNotContain("/en/park/park-1/magic-park/item/item-1/big-coaster/videos/video-1/front-row-ride", urls);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    private static Mock<IImageRepository> CreateEmptyImageRepository()
    {
        return CreateImageRepository();
    }

    private static Mock<IImageRepository> CreateImageRepository(params Image[] images)
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                1,
                It.IsAny<ImageSearchCriteria>(),
                It.IsAny<CancellationToken>()))
            .Returns((int page, int pageSize, ImageSearchCriteria criteria, CancellationToken cancellationToken) =>
            {
                List<Image> matchingImages = images
                    .Where(image => MatchesCriteria(image, criteria))
                    .ToList();
                IReadOnlyCollection<Image> pageItems = matchingImages
                    .Take(1)
                    .ToList();

                return Task.FromResult(new PagedResult<Image>(pageItems, page, pageSize, matchingImages.Count));
            });

        return imageRepository;
    }

    private static Image CreateImage(string id, ImageOwnerType ownerType, string ownerId, ImageCategory category)
    {
        return new Image
        {
            Id = id,
            OwnerType = ownerType,
            OwnerId = ownerId,
            Category = category,
            IsPublished = true,
        };
    }

    private static bool MatchesCriteria(Image image, ImageSearchCriteria criteria)
    {
        if (criteria.Category.HasValue && image.Category != criteria.Category.Value)
        {
            return false;
        }

        if (criteria.OwnerType.HasValue && image.OwnerType != criteria.OwnerType.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.OwnerId) && !string.Equals(image.OwnerId, criteria.OwnerId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (criteria.OwnerIds is not null && !criteria.OwnerIds.Contains(image.OwnerId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (criteria.IsPublished.HasValue && image.IsPublished != criteria.IsPublished.Value)
        {
            return false;
        }

        if (criteria.HasOwner.HasValue)
        {
            bool hasOwner = image.OwnerType != ImageOwnerType.None && !string.IsNullOrWhiteSpace(image.OwnerId);
            if (hasOwner != criteria.HasOwner.Value)
            {
                return false;
            }
        }

        return true;
    }
}
