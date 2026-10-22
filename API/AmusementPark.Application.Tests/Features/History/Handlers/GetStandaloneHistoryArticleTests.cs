using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class GetStandaloneHistoryArticleTests
{
    [Theory]
    [InlineData(true, true, true, true, "Operating", false, true)]
    [InlineData(false, true, true, true, "Operating", false, false)]
    [InlineData(true, false, true, true, "Operating", false, false)]
    [InlineData(true, true, false, true, "Operating", false, false)]
    [InlineData(true, true, true, false, "Operating", false, false)]
    [InlineData(true, true, true, true, "ClosedDefinitively", false, false)]
    [InlineData(false, false, false, true, "Operating", true, true)]
    public async Task HandleAsync_PreservesStandalonePublicationRules(bool visible, bool published, bool eventVisible, bool major, string status, bool includeHidden, bool expected)
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1", Name = "DreamFly Porto", IsVisible = visible,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetails { Status = status },
        };
        HistoryEvent historyEvent = new HistoryEvent
        {
            Id = "event-1", OwnerId = attraction.Id, EntityType = HistoryEntityType.StandaloneAttraction,
            IsVisible = eventVisible, IsMajor = major,
            Article = new HistoryArticle { IsPublished = published, Slug = "premiers-vols" },
        };
        Mock<IHistoryEventRepository> events = new Mock<IHistoryEventRepository>();
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> attractions = new Mock<IStandaloneAttractionRepository>();
        events.Setup(repository => repository.GetByIdAsync("event-1", includeHidden, It.IsAny<CancellationToken>())).ReturnsAsync(historyEvent);
        attractions.Setup(repository => repository.GetByIdAsync("standalone-1", includeHidden, It.IsAny<CancellationToken>())).ReturnsAsync(attraction);
        GetHistoryArticleQueryHandler handler = new GetHistoryArticleQueryHandler(events.Object, parks.Object, items.Object, images.Object, attractions.Object);

        ApplicationResult<HistoryArticleResult> result = await handler.HandleAsync(new GetHistoryArticleQuery("event-1", includeHidden));

        Assert.Equal(expected, result.IsSuccess);
        if (expected)
        {
            Assert.Same(attraction, result.Value!.StandaloneAttraction);
            Assert.Same(historyEvent.Article, result.Value.Event.Article);
            Assert.Null(result.Value.Park);
            Assert.Null(result.Value.ParkItem);
        }
        parks.VerifyNoOtherCalls();
        items.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithoutOwnerRepository_DoesNotExposeStandaloneArticle()
    {
        Mock<IHistoryEventRepository> events = new Mock<IHistoryEventRepository>();
        events.Setup(repository => repository.GetByIdAsync("event-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HistoryEvent { Id = "event-1", OwnerId = "missing", EntityType = HistoryEntityType.StandaloneAttraction,
                IsVisible = true, IsMajor = true, Article = new HistoryArticle { IsPublished = true } });
        GetHistoryArticleQueryHandler handler = new GetHistoryArticleQueryHandler(events.Object, Mock.Of<IParkRepository>(), Mock.Of<IParkItemRepository>(), Mock.Of<IImageRepository>());

        ApplicationResult<HistoryArticleResult> result = await handler.HandleAsync(new GetHistoryArticleQuery("event-1", false));

        Assert.False(result.IsSuccess);
    }
}
