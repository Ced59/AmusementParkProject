using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
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
        repository.Setup(item => item.GetPageAsync(1, int.MaxValue, false, true, null, null, null, null, cancellationToken)).ReturnsAsync(page);
        ParksSitemapSectionProvider provider = new ParksSitemapSectionProvider(repository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, cancellationToken);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix/images");
        Assert.DoesNotContain(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix/items");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        repository.VerifyAll();
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
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr", "en" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/visible-park/item/item-1/attraction-familiale" && url.LastModifiedUtc == new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(urls, static url => url.RelativePath == "/en/park/park-1/visible-park/item/item-1/attraction-familiale");
        parkRepository.VerifyAll();
        itemRepository.VerifyAll();
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
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
                new Park { Id = "hidden-park", Name = "Hidden Park", IsVisible = false, AdminReviewStatus = AdminReviewStatus.Validated },
            });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        SitemapUrlEntry url = Assert.Single(urls);
        Assert.Equal("/fr/park/park-1/visible-park/item/item-1/attraction", url.RelativePath);
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
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
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
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
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
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
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
        itemRepository.Setup(item => item.GetPublicSitemapCandidatesAsync(int.MaxValue, It.IsAny<CancellationToken>())).ReturnsAsync(itemCandidates);
        parkRepository.Setup(item => item.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-99" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Park { Id = "park-99", Name = "Late Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated } });
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" } };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        SitemapUrlEntry url = Assert.Single(urls);
        Assert.Equal("/fr/park/park-99/late-park/item/item-99/late-attraction", url.RelativePath);
        parkRepository.Verify(repository => repository.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<AdminReviewStatus?>(), It.IsAny<ParkType?>(), It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
