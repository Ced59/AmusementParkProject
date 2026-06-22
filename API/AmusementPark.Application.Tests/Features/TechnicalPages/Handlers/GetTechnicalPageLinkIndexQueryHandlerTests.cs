using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalPages.Handlers;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.TechnicalPages.Queries;
using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.TechnicalPages.Handlers;

public sealed class GetTechnicalPageLinkIndexQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCalled_ShouldUsePublicLinkIndexProjection()
    {
        TechnicalPage page = new TechnicalPage
        {
            Id = "technical-lap-bar",
            CategoryKey = "restraint",
            Slug = "lap-bar",
            Titles = new List<LocalizedText> { new LocalizedText("fr", "Lap bar") },
            Aliases = new List<TechnicalPageAlias>
            {
                new TechnicalPageAlias
                {
                    CategoryKey = "restraint",
                    Labels = new List<LocalizedText> { new LocalizedText("fr", "Lap bar") },
                },
            },
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Mock<ITechnicalPageRepository> repository = new Mock<ITechnicalPageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetPublicLinkIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { page });

        GetTechnicalPageLinkIndexQueryHandler handler = new GetTechnicalPageLinkIndexQueryHandler(repository.Object);

        ApplicationResult<IReadOnlyCollection<TechnicalPageResult>> result = await handler.HandleAsync(new GetTechnicalPageLinkIndexQuery());

        Assert.True(result.IsSuccess);
        TechnicalPageResult technicalPage = Assert.Single(result.Value!);
        Assert.Equal("lap-bar", technicalPage.Slug);
        Assert.Empty(technicalPage.ContentBlocks);
        repository.VerifyAll();
    }
}
