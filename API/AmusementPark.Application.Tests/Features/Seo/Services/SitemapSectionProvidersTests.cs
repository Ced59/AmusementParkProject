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
    public void ParksProviderNormalizeDynamicLimit_WhenValueOutsideRange_ShouldClamp()
    {
        Assert.Equal(1, ParksSitemapSectionProvider.NormalizeDynamicLimit(0));
        Assert.Equal(50000, ParksSitemapSectionProvider.NormalizeDynamicLimit(50001));
        Assert.Equal(250, ParksSitemapSectionProvider.NormalizeDynamicLimit(250));
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
        }, 1, 10, 2);
        Mock<IParkRepository> repository = new Mock<IParkRepository>(MockBehavior.Strict);
        repository.Setup(item => item.GetPageAsync(1, 10, false, true, null, null, null, cancellationToken)).ReturnsAsync(page);
        ParksSitemapSectionProvider provider = new ParksSitemapSectionProvider(repository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" }, MaxDynamicUrlsPerType = 10 };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, cancellationToken);

        Assert.Equal(2, urls.Count);
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix");
        Assert.Contains(urls, static url => url.RelativePath == "/fr/park/park-1/parc-asterix/items");
        Assert.DoesNotContain(urls, static url => url.RelativePath.Contains("hidden", StringComparison.OrdinalIgnoreCase));
        repository.VerifyAll();
    }

    [Fact]
    public async Task ParkItemsProvider_WhenItemParentParkIsNotPublic_ShouldSkipItem()
    {
        PagedResult<Park> parkPage = new PagedResult<Park>(new[]
        {
            new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        }, 1, 10, 1);
        PagedResult<ParkItem> itemPage = new PagedResult<ParkItem>(new[]
        {
            new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Attraction", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
            new ParkItem { Id = "item-2", ParkId = "missing-park", Name = "Orphan", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated },
        }, 1, 10, 2);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkRepository.Setup(item => item.GetPageAsync(1, 10, false, true, null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(parkPage);
        itemRepository.Setup(item => item.GetPageAsync(1, 10, null, null, false, true, null, null, null, null, It.IsAny<CancellationToken>(), ParkItemAdminSortField.Default, false)).ReturnsAsync(itemPage);
        ParkItemsSitemapSectionProvider provider = new ParkItemsSitemapSectionProvider(parkRepository.Object, itemRepository.Object);
        SitemapGenerationContext context = new SitemapGenerationContext { SupportedLanguages = new[] { "fr" }, MaxDynamicUrlsPerType = 10 };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(context, CancellationToken.None);

        SitemapUrlEntry url = Assert.Single(urls);
        Assert.Equal("/fr/park/park-1/visible-park/item/item-1/attraction", url.RelativePath);
    }
}
