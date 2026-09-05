using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class GetLatestHistoryArticlesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_FiltersArticlesWhosePublicContextIsHidden()
    {
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        HistoryEvent hiddenArticle = CreateArticle("hidden-event", HistoryEntityType.Park, "hidden-park", null);
        HistoryEvent publicArticle = CreateArticle("public-event", HistoryEntityType.ParkItem, "public-park", "public-item");
        Park hiddenPark = new Park { Id = "hidden-park", Name = "Hidden park", IsVisible = false };
        Park publicPark = new Park
        {
            Id = "public-park",
            Name = "Public park",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        ParkItem publicItem = new ParkItem
        {
            Id = "public-item",
            ParkId = publicPark.Id,
            Name = "Public attraction",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        historyRepository
            .Setup(repository => repository.GetLatestPublishedArticlesAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { hiddenArticle, publicArticle });
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "hidden-park", "public-park" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { hiddenPark, publicPark });
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "public-item" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { publicItem });

        GetLatestHistoryArticlesQueryHandler handler = new GetLatestHistoryArticlesQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<IReadOnlyCollection<HistoryArticleResult>> result = await handler.HandleAsync(
            new GetLatestHistoryArticlesQuery(10));

        HistoryArticleResult article = Assert.Single(result.Value!);
        Assert.True(result.IsSuccess);
        Assert.Equal("public-event", article.Event.Id);
        Assert.Equal("public-park", article.ContextPark?.Id);
        Assert.Equal("public-item", article.ParkItem?.Id);
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    private static HistoryEvent CreateArticle(
        string eventId,
        HistoryEntityType entityType,
        string parkId,
        string? parkItemId)
    {
        return new HistoryEvent
        {
            Id = eventId,
            Key = eventId,
            EntityType = entityType,
            OwnerId = parkItemId ?? parkId,
            ParkId = parkId,
            ContextParkId = parkId,
            ParkItemId = parkItemId,
            IsVisible = true,
            IsMajor = true,
            Article = new HistoryArticle
            {
                IsPublished = true,
            },
        };
    }
}
