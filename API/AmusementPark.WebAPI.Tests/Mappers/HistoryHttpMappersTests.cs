using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class HistoryHttpMappersTests
{
    [Fact]
    public void ToHttp_WhenMappingPublicTimeline_ShouldOmitArticleBlocks()
    {
        HistoryEvent historyEvent = CreateHistoryEventWithArticle();
        HistoryTimelineResult result = new HistoryTimelineResult
        {
            EntityType = HistoryEntityType.Park,
            Events = new[]
            {
                new HistoryTimelineEventResult
                {
                    Event = historyEvent,
                },
            },
            Pagination = new HistoryTimelinePaginationResult
            {
                TotalItems = 1,
                TotalPages = 1,
                CurrentPage = 1,
                ItemsPerPage = 24,
            },
        };

        HistoryTimelineDto dto = result.ToHttp();

        HistoryArticleContentDto? article = dto.Events.Single().Event.Article;
        Assert.NotNull(article);
        Assert.Equal("opening-story", article!.Slug);
        Assert.Equal("image-1", article.MainImageId);
        Assert.True(article.IsPublished);
        Assert.Empty(article.Blocks);
        Assert.Empty(article.Sources);
    }

    [Fact]
    public void ToHttp_WhenMappingHistoryArticle_ShouldKeepArticleBlocks()
    {
        HistoryEvent historyEvent = CreateHistoryEventWithArticle();
        HistoryArticleResult result = new HistoryArticleResult
        {
            Event = historyEvent,
        };

        HistoryArticleDto dto = result.ToHttp();

        HistoryArticleContentDto? article = dto.Event.Article;
        Assert.NotNull(article);
        Assert.Single(article!.Blocks);
        Assert.Single(article.Sources);
    }

    private static HistoryEvent CreateHistoryEventWithArticle()
    {
        return new HistoryEvent
        {
            Id = "history-1",
            Key = "opening",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            ParkId = "park-1",
            Year = 1987,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsMajor = true,
            IsVisible = true,
            Article = new HistoryArticle
            {
                Slug = "opening-story",
                MainImageId = "image-1",
                IsPublished = true,
                Blocks = new List<HistoryArticleBlock>
                {
                    new HistoryArticleBlock
                    {
                        Id = "block-1",
                        Type = HistoryArticleBlockType.Paragraph,
                        SortOrder = 1,
                    },
                },
                Sources = new List<HistorySourceReference>
                {
                    new HistorySourceReference
                    {
                        Url = "https://example.test/source",
                    },
                },
            },
        };
    }
}
