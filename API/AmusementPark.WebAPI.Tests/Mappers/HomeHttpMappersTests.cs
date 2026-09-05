using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using AmusementPark.WebAPI.Contracts.Home;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class HomeHttpMappersTests
{
    [Fact]
    public void ToHomeLatestHttp_MapsOnlyTheArticleCardPayload()
    {
        HistoryEvent historyEvent = new HistoryEvent
        {
            Id = "event-1",
            EntityType = HistoryEntityType.ParkItem,
            ParkId = "park-1",
            ParkItemId = "item-1",
            Summaries = new List<LocalizedText> { new LocalizedText("en", "Event summary") },
            MainImageId = "event-image",
            Article = new HistoryArticle
            {
                Slug = "article-slug",
                Titles = new List<LocalizedText> { new LocalizedText("en", "Article title") },
                Summaries = new List<LocalizedText> { new LocalizedText("en", "Article summary") },
                MainImageId = "article-image",
                IsPublished = true,
            },
        };
        HistoryArticleResult result = new HistoryArticleResult
        {
            Event = historyEvent,
            ContextPark = new Park { Id = "park-1", Name = "Park" },
            ParkItem = new ParkItem { Id = "item-1", Name = "Attraction" },
        };

        HomeLatestArticleDto dto = result.ToHomeLatestHttp();

        Assert.Equal("event-1", dto.EventId);
        Assert.Equal("ParkItem", dto.EntityType);
        Assert.Equal("Park", dto.ParkName);
        Assert.Equal("Attraction", dto.ParkItemName);
        Assert.Equal("article-slug", dto.Slug);
        Assert.Equal("Article title", Assert.Single(dto.Titles).Value);
        Assert.Equal("Article summary", Assert.Single(dto.Summaries).Value);
        Assert.Equal("article-image", dto.MainImageId);
    }
}
