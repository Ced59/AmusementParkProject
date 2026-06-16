using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
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

        Assert.Equal(4, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/en/home" && url.Priority == 1.0m);
        Assert.Contains(urls, static url => url.RelativePath == "/en/privacy" && url.ChangeFrequency == "yearly");
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

        Assert.Equal(8, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/home");
        Assert.Contains(urls, static url => url.RelativePath == "/en/home");
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
        PagedResult<Park> page = new PagedResult<Park>(new[]
        {
            new Park { Id = "park-1", Name = "Parc Astérix", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated, UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Park { Id = "hidden", Name = "Hidden", IsVisible = false, AdminReviewStatus = AdminReviewStatus.Validated },
        }, 1, int.MaxValue, 2);
        Mock<IParkRepository> repository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository(CreateImage("image-park-1", ImageOwnerType.Park, "park-1", ImageCategory.Park));
        repository.Setup(item => item.GetPageAsync(1, int.MaxValue, false, true, null, null, null, null, cancellationToken)).ReturnsAsync(page);
        ParksSitemapSectionProvider provider = new ParksSitemapSectionProvider(repository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, cancellationToken);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix/images");
        Assert.DoesNotContain(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix/items");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        repository.VerifyAll();
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
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
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
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
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
        Mock<IImageRepository> imageRepository = CreateImageRepository(CreateImage("image-item-1", ImageOwnerType.Attraction, "item-1", ImageCategory.Attraction));
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(4, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale/images" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale");
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale/images");
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
        imageRepository.VerifyAll();
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
        Mock<IImageRepository> imageRepository = CreateImageRepository(CreateImage("image-item-1", ImageOwnerType.Attraction, "item-1", ImageCategory.Attraction));
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
                new Park { Id = "hidden-park", Name = "Hidden Park", IsVisible = false, AdminReviewStatus = AdminReviewStatus.Validated },
            });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction/images");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("missing", StringComparison.OrdinalIgnoreCase));
        imageRepository.VerifyAll();
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
        Mock<IImageRepository> imageRepository = CreateImageRepository();
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => !ids.Any()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Park>());
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Empty(urls);
        imageRepository.VerifyAll();
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
        Mock<IImageRepository> imageRepository = CreateImageRepository(
            CreateImage("image-item-1", ImageOwnerType.Attraction, "item-1", ImageCategory.Attraction),
            CreateImage("image-item-2", ImageOwnerType.Attraction, "item-2", ImageCategory.Attraction),
            CreateImage("image-item-3", ImageOwnerType.Attraction, "item-3", ImageCategory.Attraction),
            CreateImage("image-item-4", ImageOwnerType.Attraction, "item-4", ImageCategory.Attraction),
            CreateImage("image-item-5", ImageOwnerType.Attraction, "item-5", ImageCategory.Attraction));
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-1", Name = "Water Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
                new Park { Id = "park-2", Name = "Theme Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(10, urls.Count);
        Assert.Equal(urls.Count, urls.Select(static url => url.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-2/theme-park/item/item-1/wood-coaster");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-2/theme-park/item/item-1/wood-coaster/images");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/water-park/item/item-2/water-ride");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-2/theme-park/item/item-3/dark-ride");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/water-park/item/item-4/main-restaurant");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-2/theme-park/item/item-5/guest-services");
        imageRepository.VerifyAll();
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
        Image[] itemImages = itemCandidates
            .Select(static item => CreateImage($"image-{item.Id}", ImageOwnerType.Attraction, item.Id!, ImageCategory.Attraction))
            .ToArray();
        Mock<IImageRepository> imageRepository = CreateImageRepository(itemImages);
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(46, urls.Count);
        Assert.Equal(urls.Count, urls.Select(static url => url.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-01/visible-item-01");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-01/visible-item-01/images");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-23/visible-item-23");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-23/visible-item-23/images");
        imageRepository.VerifyAll();
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
        Mock<IImageRepository> imageRepository = CreateImageRepository(CreateImage("image-item-99", ImageOwnerType.Attraction, "item-99", ImageCategory.Attraction));
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-99" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-99", Name = "Late Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-99/late-park/item/item-99/late-attraction");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-99/late-park/item/item-99/late-attraction/images");
        parkRepository.Verify(repository => repository.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<AdminReviewStatus?>(), It.IsAny<ParkType?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Never);
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task ParksProvider_WhenPublicParkHasNoPublishedImages_ShouldSkipImagesUrl()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        PagedResult<Park> page = new PagedResult<Park>(new[]
        {
            new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        }, 1, int.MaxValue, 1);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository();
        parkRepository.Setup(item => item.GetPageAsync(1, int.MaxValue, false, true, null, null, null, null, cancellationToken)).ReturnsAsync(page);
        ParksSitemapSectionProvider provider = new ParksSitemapSectionProvider(parkRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, cancellationToken);

        Assert.Single(urls);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park");
        Assert.DoesNotContain(urls, static url => url.RelativePath.EndsWith("/images", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemsProvider_WhenPublicItemHasNoPublishedImages_ShouldSkipImagesUrl()
    {
        ParkItem[] itemCandidates = new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Attraction familiale", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        };
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = CreateImageRepository();
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object, imageRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Single(urls);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale");
        Assert.DoesNotContain(urls, static url => url.RelativePath.EndsWith("/images", StringComparison.OrdinalIgnoreCase));
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
        imageRepository.VerifyAll();
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
}
