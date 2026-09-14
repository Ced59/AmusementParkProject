using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class PublicParkFitControllerTests
{
    [Fact]
    public async Task SearchAsync_ShouldForwardAnonymousCriteriaAndReturnTheBoundedResult()
    {
        DateOnly evaluationDate = new DateOnly(2026, 10, 10);
        ParkFitSearchResult applicationResult = new ParkFitSearchResult
        {
            MethodVersion = "park-fit-2026-01",
            EvaluationDate = evaluationDate,
            TotalCandidateCount = 2,
            InspectedCandidateCount = 2,
        };
        Mock<IQueryHandler<
            SearchParksByFitQuery,
            ApplicationResult<ParkFitSearchResult>>> handler =
            new Mock<IQueryHandler<
                SearchParksByFitQuery,
                ApplicationResult<ParkFitSearchResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<SearchParksByFitQuery>(query =>
                    query.EvaluationDate == evaluationDate
                    && query.Members.Single().MemberKey == "member-1"
                    && query.MaximumResults == 5),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ParkFitSearchResult>.Success(applicationResult));
        PublicParkFitController controller = new PublicParkFitController(handler.Object);
        ParkFitSearchRequestDto request = new ParkFitSearchRequestDto
        {
            EvaluationDate = evaluationDate,
            Members = new[]
            {
                new ParkFitSearchMemberCriteriaDto
                {
                    HeightCentimeters = 120,
                },
            },
            MaximumResults = 5,
        };

        IActionResult response = await controller.SearchAsync(request, CancellationToken.None);

        ParkFitSearchResponseDto body = Assert.IsType<ParkFitSearchResponseDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(2, body.TotalCandidateCount);
        Assert.Equal("park-fit-2026-01", body.MethodVersion);
        handler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldBePublicNoStoreAndRateLimited()
    {
        RouteAttribute route = Assert.IsType<RouteAttribute>(
            typeof(PublicParkFitController).GetCustomAttribute<RouteAttribute>());
        Assert.Equal("public/park-fit", route.Template);
        Assert.NotNull(typeof(PublicParkFitController)
            .GetCustomAttribute<AllowAnonymousAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            typeof(PublicParkFitController).GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);

        MethodInfo action = typeof(PublicParkFitController).GetMethod(
            nameof(PublicParkFitController.SearchAsync))!;
        HttpPostAttribute post = Assert.IsType<HttpPostAttribute>(
            action.GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal("search", post.Template);
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.ParkFitSearch, rateLimit.PolicyName);
    }
}
