using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.StandaloneAttractions;
using AmusementPark.WebAPI.Controllers;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkDataEditorStandaloneAttractionsControllerTests
{
    [Theory]
    [InlineData("  DreamFly  ", " pt ", "DreamFly", "PT")]
    [InlineData(" ", " ", null, null)]
    public async Task SearchAsync_ShouldIncludeHiddenAndRetainPagination(
        string query, string countryCode, string? expectedQuery, string? expectedCountryCode)
    {
        Mock<IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>>> handler =
            new Mock<IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>>>(MockBehavior.Strict);
        StandaloneAttraction existing = new StandaloneAttraction
        {
            Id = "hidden-attraction",
            Name = "DreamFly Porto",
            CountryCode = "PT",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };
        handler.Setup(candidate => candidate.HandleAsync(
                It.Is<GetStandaloneAttractionsPageQuery>(request =>
                    request.Paging.Page == 2 && request.Paging.PageSize == 25
                    && request.Search == expectedQuery && request.CountryCode == expectedCountryCode
                    && request.IncludeHidden && request.IsVisible == null && request.AdminReviewStatus == null
                    && request.Type == null && request.ManufacturerId == null
                    && request.SortField == StandaloneAttractionAdminSortField.Name && !request.SortDescending),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<PagedResult<StandaloneAttraction>>.Success(
                new PagedResult<StandaloneAttraction>(new[] { existing }, 2, 25, 26)));
        ParkDataEditorStandaloneAttractionsController controller = new ParkDataEditorStandaloneAttractionsController(
            handler.Object,
            Mock.Of<IQueryHandler<GetStandaloneAttractionDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>>(MockBehavior.Strict));

        IActionResult result = await controller.SearchAsync(new PaginationRequestDto { Page = 2, Size = 25 }, query, countryCode);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        PagedResponseDto<StandaloneAttractionDto> response = Assert.IsType<PagedResponseDto<StandaloneAttractionDto>>(ok.Value);
        StandaloneAttractionDto attraction = Assert.Single(response.Data);
        Assert.Equal("hidden-attraction", attraction.Id);
        Assert.False(attraction.IsVisible);
        Assert.NotNull(response.Pagination);
        Assert.Equal(26, response.Pagination.TotalItems);
        Assert.Equal(2, response.Pagination.CurrentPage);
        handler.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Completeness_ShouldForwardExactTargetAndProjectionWithHiddenAccess(bool projected)
    {
        Mock<IQueryHandler<GetStandaloneAttractionDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>> handler = new(MockBehavior.Strict);
        handler.Setup(candidate => candidate.HandleAsync(
                new GetStandaloneAttractionDataCompletenessScoreQuery("standalone-1", true, projected), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<DataCompletenessScore>.Success(DataCompletenessScore.FromPoints(100, 100,
                DataCompletenessScoringRules.ForbiddenPublicTextPublicationBlocker)));
        ParkDataEditorStandaloneAttractionsController controller = new(
            Mock.Of<IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>>>(MockBehavior.Strict),
            handler.Object);

        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetDataCompletenessAsync("standalone-1", projected));
        DataCompletenessScoreDto score = Assert.IsType<DataCompletenessScoreDto>(result.Value);

        Assert.Equal(95, score.CompletenessScore);
        Assert.Equal(DataCompletenessScoringRules.ForbiddenPublicTextPublicationBlocker, Assert.Single(score.PublicationBlockers));
        handler.VerifyAll();
    }
}
