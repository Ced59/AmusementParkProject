using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class StandaloneHistoryArticleSitemapTests
{
    [Theory]
    [InlineData(true, true, true, 8)]
    [InlineData(false, true, true, 0)]
    [InlineData(true, false, true, 0)]
    [InlineData(true, true, false, 0)]
    public async Task GetUrlsAsync_RequiresPublicOwnerAndPublishedMajorArticle(bool visible, bool published, bool major, int expected)
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1", Name = "DreamFly Porto", IsVisible = visible,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetails { Status = "Operating" },
        };
        HistoryEvent historyEvent = new HistoryEvent
        {
            Id = "event-1", OwnerId = attraction.Id, EntityType = HistoryEntityType.StandaloneAttraction,
            IsVisible = true, IsMajor = major,
            Article = new HistoryArticle { IsPublished = published, Slug = "premiers-vols" },
        };
        Mock<IHistoryEventRepository> events = new Mock<IHistoryEventRepository>();
        Mock<IParkRepository> parks = new Mock<IParkRepository>();
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>();
        Mock<IStandaloneAttractionRepository> attractions = new Mock<IStandaloneAttractionRepository>();
        events.Setup(repository => repository.GetPublicSitemapCandidatesAsync(50000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { historyEvent });
        parks.Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Park>());
        items.Setup(repository => repository.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        attractions.Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "standalone-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { attraction });
        HistoryArticlesSitemapSectionProvider provider = new HistoryArticlesSitemapSectionProvider(events.Object, parks.Object, items.Object, attractions.Object);
        string[] languages = { "fr", "en", "de", "nl", "it", "es", "pl", "pt" };

        IReadOnlyCollection<SitemapUrlEntry> urls = await provider.GetUrlsAsync(new SitemapGenerationContext { SupportedLanguages = languages }, CancellationToken.None);

        Assert.Equal(expected, urls.Count);
        if (expected > 0)
        {
            foreach (string language in languages)
            {
                Assert.Contains(urls, entry => entry.RelativePath == $"/{language}/attraction/standalone-1/dreamfly-porto/history/event-1/premiers-vols");
            }
        }
        parks.Verify(repository => repository.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => !ids.Any()), It.IsAny<CancellationToken>()), Times.Once);
    }
}
