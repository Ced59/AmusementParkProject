using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminParkFitDataQualityControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnThePagedActionableAudit()
    {
        ParkFitDataQualityAssessment assessment = new ParkFitDataQualityAssessment
        {
            ParkId = "park-1",
            ParkName = "Parc témoin",
            Status = ParkFitDataQualityStatus.EligibleForDiscoveryOnly,
            CoveragePercent = 75,
            VisibleAttractionCount = 4,
            DecisionEligibleAttractionCount = 3,
            IssueItemCount = 1,
            MissingSourceItemCount = 1,
            Issues = new[] { ParkFitDataQualityIssue.IncompleteRestrictionCoverage },
            IssueSamples = new[]
            {
                new ParkFitDataQualityItemAssessment
                {
                    ParkItemId = "item-1",
                    ParkItemName = "Attraction témoin",
                    Issues = new[] { ParkFitDataQualityIssue.MissingAuthoritativeSource },
                },
            },
        };
        Mock<IQueryHandler<
            GetParkFitDataQualityPageQuery,
            ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>>> handler =
            new Mock<IQueryHandler<
                GetParkFitDataQualityPageQuery,
                ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<GetParkFitDataQualityPageQuery>(query =>
                    query.Paging.Page == 2 && query.Paging.PageSize == 10),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>.Success(
                new PagedResult<ParkFitDataQualityOperationsResult>(new[]
                {
                    new ParkFitDataQualityOperationsResult
                    {
                        Assessment = assessment,
                        RecommendationState = ParkFitRecommendationState.Active,
                        PendingReportCount = 2,
                    },
                }, 2, 10, 21)));
        AdminParkFitDataQualityController controller = new AdminParkFitDataQualityController(handler.Object);

        IActionResult response = await controller.GetAsync(
            new PaginationRequestDto { Page = 2, Size = 10 },
            CancellationToken.None);

        PagedResponseDto<ParkFitDataQualityDto> body =
            Assert.IsType<PagedResponseDto<ParkFitDataQualityDto>>(
                Assert.IsType<OkObjectResult>(response).Value);
        ParkFitDataQualityDto dto = Assert.Single(body.Data);
        Assert.Equal("EligibleForDiscoveryOnly", dto.Status);
        Assert.Equal(75, dto.CoveragePercent);
        Assert.Equal("Active", dto.RecommendationState);
        Assert.Equal(2, dto.PendingReportCount);
        Assert.Equal("MissingAuthoritativeSource", Assert.Single(dto.IssueSamples).Issues.Single());
        Assert.Equal(21, body.Pagination?.TotalItems);
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldBeAdminOnlyAndNoStore()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(AdminParkFitDataQualityController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("admin/park-fit/data-quality", route.Template);
        AuthorizeAttribute authorize = Assert.Single(
            typeof(AdminParkFitDataQualityController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(typeof(AdminParkFitDataQualityController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(AdminParkFitDataQualityController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
    }
}
