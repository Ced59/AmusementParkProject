using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class SitemapSectionProvidersTests
{
    [Fact]
    public async Task StaticPagesProvider_WhenLanguagesAreBlank_ShouldFallbackToEnglishStaticPages()
    {
        StaticPagesSitemapSectionProvider provider = new StaticPagesSitemapSectionProvider();

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(new SitemapGenerationContext(), CancellationToken.None);

        Assert.Equal(7, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/en/home" && url.Priority == 1.0m);
        Assert.Contains(urls, static url => url.RelativePath == "/en/rankings" && url.Priority == 0.82m);
        Assert.Contains(urls, static url => url.RelativePath == "/en/privacy" && url.ChangeFrequency == "yearly");
        Assert.Contains(urls, static url => url.RelativePath == "/en/contact" && url.ChangeFrequency == "monthly");
        Assert.Contains(urls, static url => url.RelativePath == "/en/versions" && url.ChangeFrequency == "monthly");
    }

    [Fact]
    public async Task StaticPagesProvider_WhenLanguagesContainDuplicatesAndWhitespace_ShouldNormalizeLanguages()
    {
        StaticPagesSitemapSectionProvider provider = new StaticPagesSitemapSectionProvider();
        SitemapGenerationContext context = new SitemapGenerationContext
        {
            SupportedLanguages = new[] { " FR ", "fr", "EN", " " },
        };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(14, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/home");
        Assert.Contains(urls, static url => url.RelativePath == "/en/home");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/rankings");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/contact");
        Assert.Contains(urls, static url => url.RelativePath == "/en/versions");
    }

    [Fact]
    public void ParksProviderIsPublicPark_WhenParkIsVisibleAndRelevant_ShouldReturnTrue()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        bool result = ParksSitemapSectionProvider.IsPublicPark(park);

        Assert.True(result);
    }

    [Theory]
    [InlineData("", "Park", true, AdminReviewStatus.Validated)]
    [InlineData("park-1", "", true, AdminReviewStatus.Validated)]
    [InlineData("park-1", "Park", false, AdminReviewStatus.Validated)]
    [InlineData("park-1", "Park", true, AdminReviewStatus.NotRelevant)]
    public void ParksProviderIsPublicPark_WhenParkIsNotIndexable_ShouldReturnFalse(string id, string name, bool isVisible, AdminReviewStatus status)
    {
        Park park = new Park
        {
            Id = id,
            Name = name,
            IsVisible = isVisible,
            AdminReviewStatus = status,
        };

        bool result = ParksSitemapSectionProvider.IsPublicPark(park);

        Assert.False(result);
    }

    [Fact]
    public async Task ParksProvider_WhenRepositoryReturnsMixedParks_ShouldReturnUrlsOnlyForPublicParks()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        Park[] parks = new[]
        {
            new Park { Id = "park-1", Name = "Parc Astérix", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Park { Id = "hidden", Name = "Hidden", IsVisible = false, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> repository = new Mock<IParkRepository>(MockBehavior.Strict);
        SetupPublicSitemapParks(repository, parks);
        ParksSitemapSectionProvider provider = new ParksSitemapSectionProvider(repository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, cancellationToken);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix/weather" && url.ChangeFrequency == "daily" && url.Priority == 0.76m);
        Assert.DoesNotContain(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix/items");
        Assert.DoesNotContain(urls, static url => url.RelativePath.EndsWith("/images", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        repository.VerifyAll();
    }

    [Fact]
    public async Task ParkImagesProvider_WhenPublicParkHasPublishedImages_ShouldReturnImageUrlsOnly()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        Park[] parks = new[]
        {
            new Park { Id = "park-1", Name = "Parc Asterix", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Park { Id = "park-2", Name = "No Photos", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new Park { Id = "hidden", Name = "Hidden", IsVisible = false, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository(CreateImage("image-park-1", ImageOwnerType.Park, "park-1", ImageCategory.Park));
        SetupPublicSitemapParks(parkRepository, parks);
        ParkImagesSitemapSectionProvider provider = new ParkImagesSitemapSectionProvider(parkRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, cancellationToken);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix/images" && url.LastModifiedUtc == new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/parc-asterix/images");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("park-2", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemListsProvider_WhenPublicItemsExist_ShouldReturnParkItemsPageForEachLanguage()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Attraction familiale", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
            new ParkItem { Id = "item-2", ParkId = "park-1", Name = "Restaurant", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc) },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) } });
        ParkItemListsSitemapSectionProvider provider = new ParkItemListsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/items" && url.LastModifiedUtc == new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/items");
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkZonesProvider_WhenVisibleZoneHasPublicItems_ShouldReturnZoneOverviewAndDetailUrls()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-1", ZoneId = "zone-1", Name = "Attraction familiale", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
            new ParkItem { Id = "item-2", ParkId = "park-1", ZoneId = "zone-hidden", Name = "Hidden zone item", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new ParkItem { Id = "item-3", ParkId = "park-1", Name = "No zone item", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        ParkZone[] zones = new[]
        {
            new ParkZone { Id = "zone-1", ParkId = "park-1", Name = "Zone enfants", IsVisible = true, SortOrder = 1, UpdatedAtUtc = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc) },
            new ParkZone { Id = "zone-hidden", ParkId = "park-1", Name = "Zone cachee", IsVisible = false, SortOrder = 2 },
            new ParkZone { Id = "zone-empty", ParkId = "park-1", Name = "Zone vide", IsVisible = true, SortOrder = 3 },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        zoneRepository.Setup(item => item.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>())).ReturnsAsync(zones);
        ParkZonesSitemapSectionProvider provider = new ParkZonesSitemapSectionProvider(parkRepository.Object, zoneRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/zones");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/zone/zone-1/zone-enfants" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("zone-hidden", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("zone-empty", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        zoneRepository.VerifyAll();
        itemRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemsProvider_WhenVisibleItemHasVisibleParentPark_ShouldReturnUrlForEachLanguage()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Attraction familiale", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale");
        Assert.DoesNotContain(urls, static url => url.RelativePath.EndsWith("/images", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemImagesProvider_WhenVisibleItemHasPublishedImages_ShouldReturnImageUrlsOnly()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Attraction familiale", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc) },
            new ParkItem { Id = "item-2", ParkId = "park-1", Name = "No Photos", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository(CreateImage("image-item-1", ImageOwnerType.ParkItem, "item-1", ImageCategory.ParkItem));
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemImagesSitemapSectionProvider provider = new ParkItemImagesSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale/images" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale/images");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("item-2", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkVideosProvider_WhenPublicParkHasPublishedVideos_ShouldReturnListAndWatchUrls()
    {
        DateTime videoUpdatedAtUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IVideoRepository> videoRepository = CreateVideoRepository(
            CreateVideo("video-1", VideoOwnerType.Park, "park-1", "Front Row Ride", videoUpdatedAtUtc),
            CreateVideo("hidden-parent-video", VideoOwnerType.Park, "hidden-park", "Hidden Parent Video", new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateVideo("draft-video", VideoOwnerType.Park, "park-1", "Draft Video", new DateTime(2026, 3, 3, 0, 0, 0, DateTimeKind.Utc), isPublished: false));
        parkRepository.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.Contains("park-1") && ids.Contains("hidden-park")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc) },
                new Park { Id = "hidden-park", Name = "Hidden Park", IsVisible = false, AdminReviewStatus = AdminReviewStatus.Validated },
            });
        ParkVideosSitemapSectionProvider provider = new ParkVideosSitemapSectionProvider(parkRepository.Object, videoRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(4, urls.Count);
        Assert.Contains(urls, url => url.RelativePath == "/fr/park/park-1/visible-park/videos" && url.LastModifiedUtc == videoUpdatedAtUtc && url.Priority == 0.72m);
        Assert.Contains(urls, url => url.RelativePath == "/fr/park/park-1/visible-park/videos/video-1/front-row-ride" && url.LastModifiedUtc == videoUpdatedAtUtc && url.Priority == 0.66m);
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/videos");
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/videos/video-1/front-row-ride");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("draft", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        videoRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkVideosProvider_WhenVideoTargetsOneLanguage_ShouldReturnOnlyMatchingLanguageUrls()
    {
        DateTime videoUpdatedAtUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IVideoRepository> videoRepository = CreateVideoRepository(
            CreateVideo("video-1", VideoOwnerType.Park, "park-1", "Front Row Ride", videoUpdatedAtUtc, languageCodes: new[] { "fr" }));
        parkRepository.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc) },
            });
        ParkVideosSitemapSectionProvider provider = new ParkVideosSitemapSectionProvider(parkRepository.Object, videoRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, url => url.RelativePath == "/fr/park/park-1/visible-park/videos" && url.LastModifiedUtc == videoUpdatedAtUtc);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/videos/video-1/front-row-ride");
        Assert.DoesNotContain(urls, static url => url.RelativePath.StartsWith("/en/", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        videoRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemVideosProvider_WhenPublicParkItemHasPublishedVideos_ShouldReturnListAndWatchUrls()
    {
        DateTime videoUpdatedAtUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IVideoRepository> videoRepository = CreateVideoRepository(
            CreateVideo("video-1", VideoOwnerType.ParkItem, "item-1", "Front Row Ride", videoUpdatedAtUtc),
            CreateVideo("hidden-item-video", VideoOwnerType.ParkItem, "hidden-item", "Hidden Item Video", new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateVideo("park-video", VideoOwnerType.Park, "park-1", "Park Video", new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc)));
        itemRepository.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("item-1") && ids.Contains("hidden-item")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Big Coaster", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc) },
                new ParkItem { Id = "hidden-item", ParkId = "park-1", Name = "Hidden Coaster", IsVisible = false, AdminReviewStatus = AdminReviewStatus.Validated },
            });
        parkRepository.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc) } });
        ParkItemVideosSitemapSectionProvider provider = new ParkItemVideosSitemapSectionProvider(parkRepository.Object, itemRepository.Object, videoRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(4, urls.Count);
        Assert.Contains(urls, url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/big-coaster/videos" && url.LastModifiedUtc == videoUpdatedAtUtc && url.Priority == 0.62m);
        Assert.Contains(urls, url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/big-coaster/videos/video-1/front-row-ride" && url.LastModifiedUtc == videoUpdatedAtUtc && url.Priority == 0.6m);
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/big-coaster/videos");
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/big-coaster/videos/video-1/front-row-ride");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("park-video", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
        videoRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemVideosProvider_WhenVideoTargetsOneLanguage_ShouldReturnOnlyMatchingLanguageUrls()
    {
        DateTime videoUpdatedAtUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IVideoRepository> videoRepository = CreateVideoRepository(
            CreateVideo("video-1", VideoOwnerType.ParkItem, "item-1", "Front Row Ride", videoUpdatedAtUtc, languageCodes: new[] { "fr" }));
        itemRepository.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Big Coaster", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc) },
            });
        parkRepository.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc) } });
        ParkItemVideosSitemapSectionProvider provider = new ParkItemVideosSitemapSectionProvider(parkRepository.Object, itemRepository.Object, videoRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/big-coaster/videos" && url.LastModifiedUtc == videoUpdatedAtUtc);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/big-coaster/videos/video-1/front-row-ride");
        Assert.DoesNotContain(urls, static url => url.RelativePath.StartsWith("/en/", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
        videoRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemsProvider_WhenParentParkIsNotPublicOrMissing_ShouldSkipItems()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Attraction", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new ParkItem { Id = "item-2", ParkId = "hidden-park", Name = "Hidden parent", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new ParkItem { Id = "item-3", ParkId = "missing-park", Name = "Orphan", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
                new Park { Id = "hidden-park", Name = "Hidden Park", IsVisible = false, AdminReviewStatus = AdminReviewStatus.Validated },
            });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Single(urls);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("", "park-1", "Attraction", true, AdminReviewStatus.Validated)]
    [InlineData("item-1", "", "Attraction", true, AdminReviewStatus.Validated)]
    [InlineData("item-1", "park-1", "", true, AdminReviewStatus.Validated)]
    [InlineData("item-1", "park-1", "Attraction", false, AdminReviewStatus.Validated)]
    [InlineData("item-1", "park-1", "Attraction", true, AdminReviewStatus.NotRelevant)]
    public async Task ParkItemsProvider_WhenItemIsNotPublic_ShouldSkipItem(string id, string parkId, string name, bool isVisible, AdminReviewStatus status)
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = id, ParkId = parkId, Name = name, IsVisible = isVisible, AdminReviewStatus = status },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => !ids.Any()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Park>());
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Empty(urls);
    }

    [Fact]
    public async Task ParkItemsProvider_WhenMultipleVisibleItemsShareParents_ShouldReturnAllWithoutDuplicates()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-2", Name = "Wood Coaster", Category = ParkItemCategory.Attraction, Type = ParkItemType.RollerCoaster, IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new ParkItem { Id = "item-2", ParkId = "park-1", Name = "Water Ride", Category = ParkItemCategory.Attraction, Type = ParkItemType.WaterRide, IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new ParkItem { Id = "item-3", ParkId = "park-2", Name = "Dark Ride", Category = ParkItemCategory.Attraction, Type = ParkItemType.DarkRide, IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new ParkItem { Id = "item-4", ParkId = "park-1", Name = "Main Restaurant", Category = ParkItemCategory.Restaurant, Type = ParkItemType.Restaurant, IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new ParkItem { Id = "item-5", ParkId = "park-2", Name = "Guest Services", Category = ParkItemCategory.Service, Type = ParkItemType.Service, IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-1", Name = "Water Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
                new Park { Id = "park-2", Name = "Theme Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(5, urls.Count);
        Assert.Equal(urls.Count, urls.Select(static url => url.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-2/theme-park/item/item-1/wood-coaster");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/water-park/item/item-2/water-ride");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-2/theme-park/item/item-3/dark-ride");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/water-park/item/item-4/main-restaurant");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-2/theme-park/item/item-5/guest-services");
    }

    [Fact]
    public async Task ParkItemsProvider_WhenMoreThanTwelvePublicItemsExist_ShouldReturnEveryCandidate()
    {
        List<ParkItem> itemCandidates = Enumerable.Range(1, 23)
            .Select(static index => new ParkItem
            {
                Id = $"item-{index:D2}",
                ParkId = "park-1",
                Name = $"Visible Item {index:D2}",
                Category = index % 3 == 0 ? ParkItemCategory.Restaurant : ParkItemCategory.Attraction,
                Type = index % 3 == 0 ? ParkItemType.Restaurant : ParkItemType.FlatRide,
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.Validated,
            })
            .ToList();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(23, urls.Count);
        Assert.Equal(urls.Count, urls.Select(static url => url.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-01/visible-item-01");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-23/visible-item-23");
    }

    [Fact]
    public async Task ParkItemsProvider_WhenParentParkIsOutsideFirstParkPage_ShouldStillReturnItemUrl()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-99", ParkId = "park-99", Name = "Late Attraction", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-99" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-99", Name = "Late Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Single(urls);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-99/late-park/item/item-99/late-attraction");
        parkRepository.Verify(repository => repository.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<AdminReviewStatus?>(), It.IsAny<ParkType?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ParkImagesProvider_WhenPublicParkHasNoPublishedImages_ShouldSkipImagesUrl()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        Park[] parks = new[]
        {
            new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository();
        SetupPublicSitemapParks(parkRepository, parks);
        ParkImagesSitemapSectionProvider provider = new ParkImagesSitemapSectionProvider(parkRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, cancellationToken);

        Assert.Empty(urls);
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemImagesProvider_WhenPublicItemHasNoPublishedImages_ShouldSkipImagesUrl()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Attraction familiale", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository();
        SetupPublicSitemapItems(itemRepository, itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemImagesSitemapSectionProvider provider = new ParkItemImagesSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Empty(urls);
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    private static void SetupPublicSitemapParks(Mock<IParkRepository> repository, IReadOnlyCollection<Park> parks)
    {
        repository
            .Setup(item => item.GetPageAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                false,
                true,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns((
                int page,
                int pageSize,
                bool includeHidden,
                bool? isVisible,
                AdminReviewStatus? adminReviewStatus,
                ParkType? type,
                string? countryCode,
                bool? hasValidCoordinates,
                CancellationToken cancellationToken) =>
            {
                IReadOnlyCollection<Park> pageItems = parks
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult(new PagedResult<Park>(pageItems, page, pageSize, parks.Count));
            });
    }

    private static void SetupPublicSitemapItems(Mock<IParkItemRepository> repository, IReadOnlyCollection<ParkItem> items)
    {
        repository
            .Setup(item => item.GetPageAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                null,
                null,
                false,
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>(),
                ParkItemAdminSortField.ParkId,
                false))
            .Returns((
                int page,
                int pageSize,
                string? parkId,
                string? search,
                bool includeHidden,
                bool? isVisible,
                AdminReviewStatus? adminReviewStatus,
                ParkItemCategory? category,
                ParkItemType? type,
                string? zoneId,
                string? manufacturerId,
                ParkItemContentBacklogFilter? contentBacklogFilter,
                CancellationToken cancellationToken,
                ParkItemAdminSortField sortField,
                bool sortDescending) =>
            {
                IReadOnlyCollection<ParkItem> pageItems = items
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult(new PagedResult<ParkItem>(pageItems, page, pageSize, items.Count));
            });
    }

    private static Mock<IImageRepository> CreateImageRepository(params Image[] images)
    {
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ImageSearchCriteria>(), It.IsAny<CancellationToken>()))
            .Returns((int page, int pageSize, ImageSearchCriteria criteria, CancellationToken cancellationToken) =>
            {
                int safePage = Math.Max(1, page);
                int safePageSize = Math.Max(1, pageSize);
                List<Image> matchingImages = images
                    .Where(image => MatchesCriteria(image, criteria))
                    .ToList();
                IReadOnlyCollection<Image> pageItems = matchingImages
                    .Skip((safePage - 1) * safePageSize)
                    .Take(safePageSize)
                    .ToList();

                return Task.FromResult(new PagedResult<Image>(pageItems, safePage, safePageSize, matchingImages.Count));
            });

        return repository;
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

    private static Mock<IVideoRepository> CreateVideoRepository(params Video[] videos)
    {
        Mock<IVideoRepository> repository = new Mock<IVideoRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<VideoSearchCriteria>(), It.IsAny<CancellationToken>()))
            .Returns((int page, int pageSize, VideoSearchCriteria criteria, CancellationToken cancellationToken) =>
            {
                int safePage = Math.Max(1, page);
                int safePageSize = Math.Max(1, pageSize);
                List<Video> matchingVideos = videos
                    .Where(video => MatchesCriteria(video, criteria))
                    .ToList();
                IReadOnlyCollection<Video> pageItems = matchingVideos
                    .Skip((safePage - 1) * safePageSize)
                    .Take(safePageSize)
                    .ToList();

                return Task.FromResult(new PagedResult<Video>(pageItems, safePage, safePageSize, matchingVideos.Count));
            });

        return repository;
    }

    private static Video CreateVideo(
        string id,
        VideoOwnerType ownerType,
        string ownerId,
        string title,
        DateTime updatedAtUtc,
        bool isPublished = true,
        IReadOnlyCollection<string>? languageCodes = null)
    {
        return new Video
        {
            Id = id,
            OwnerType = ownerType,
            OwnerId = ownerId,
            Title = title,
            IsPublished = isPublished,
            LanguageCodes = languageCodes?.ToList() ?? new List<string>(),
            UpdatedAtUtc = updatedAtUtc,
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

    private static bool MatchesCriteria(Video video, VideoSearchCriteria criteria)
    {
        if (criteria.OwnerType.HasValue && video.OwnerType != criteria.OwnerType.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.OwnerId) && !string.Equals(video.OwnerId, criteria.OwnerId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (criteria.IsPublished.HasValue && video.IsPublished != criteria.IsPublished.Value)
        {
            return false;
        }

        return true;
    }
}
