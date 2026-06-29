using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalPages.Commands;
using AmusementPark.Application.Features.TechnicalPages.Handlers;
using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.TechnicalPages.Handlers;

public sealed class UpsertTechnicalPagesJsonCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPagesAreValid_ShouldUpsertBySlugWithoutRefreshingSitemap()
    {
        TechnicalPage page = CreatePage(" Lap Bar ", " restraint ");
        Mock<ITechnicalPageRepository> repository = new Mock<ITechnicalPageRepository>(MockBehavior.Strict);

        repository
            .Setup(value => value.UpsertBySlugAsync(
                It.Is<TechnicalPage>(candidate => candidate.Slug == "lap-bar" && candidate.CategoryKey == "restraint"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TechnicalPage candidate, CancellationToken _) =>
            {
                candidate.Id = "technical-1";
                return new TechnicalPageUpsertOutcome(candidate, true);
            });

        UpsertTechnicalPagesJsonCommandHandler handler = new UpsertTechnicalPagesJsonCommandHandler(repository.Object);

        ApplicationResult<TechnicalPageJsonUpsertResult> result = await handler.HandleAsync(new UpsertTechnicalPagesJsonCommand(new[] { page }));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value!.CreatedCount);
        Assert.Equal(0, result.Value.UpdatedCount);
        Assert.Equal("technical-1", Assert.Single(result.Value.Pages).Id);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPagesAreEmpty_ShouldFailWithoutRepositoryCall()
    {
        Mock<ITechnicalPageRepository> repository = new Mock<ITechnicalPageRepository>(MockBehavior.Strict);
        UpsertTechnicalPagesJsonCommandHandler handler = new UpsertTechnicalPagesJsonCommandHandler(repository.Object);

        ApplicationResult<TechnicalPageJsonUpsertResult> result = await handler.HandleAsync(new UpsertTechnicalPagesJsonCommand(Array.Empty<TechnicalPage>()));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "validation.required");
        repository.VerifyNoOtherCalls();
    }

    private static TechnicalPage CreatePage(string title, string categoryKey)
    {
        return new TechnicalPage
        {
            CategoryKey = categoryKey,
            CategoryNames = LocalizedValues("Restraints"),
            Titles = LocalizedValues(title),
            Summaries = LocalizedValues("Detailed technical explanation."),
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }

    private static List<LocalizedText> LocalizedValues(string value)
    {
        return new List<LocalizedText>
        {
            new LocalizedText("fr", value),
            new LocalizedText("en", value),
            new LocalizedText("de", value),
            new LocalizedText("nl", value),
            new LocalizedText("it", value),
            new LocalizedText("es", value),
            new LocalizedText("pl", value),
            new LocalizedText("pt", value),
        };
    }
}
