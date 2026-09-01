using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class RatingMethodologiesControllerTests
{
    [Fact]
    public async Task GetCurrentAsync_ShouldMapTheStructuredMethodology()
    {
        RatingMethodologyResult methodology = CreateMethodology();
        ControllerFixture fixture = CreateFixture(methodology);

        IActionResult response = await fixture.Controller.GetCurrentAsync(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(response);
        RatingMethodologyDto dto = Assert.IsType<RatingMethodologyDto>(ok.Value);
        Assert.Equal("ratings-2026-01", dto.Version);
        Assert.Equal(0.5m, dto.RatingScale.Minimum);
        Assert.Equal(5m, dto.RatingScale.Maximum);
        Assert.Equal(3.5d, dto.Bayesian.PriorMean);
        Assert.Equal(0.7d, dto.ParkComposition.DirectRatingWeight);
        Assert.Equal(10, dto.EvidenceThresholds.Eligible);
        Assert.Equal("competition", dto.PublicationRules.RankingConvention);
        fixture.CurrentHandler.VerifyAll();
    }

    [Fact]
    public async Task GetByVersionAsync_ShouldReturnProblemDetailsForAnUnknownVersion()
    {
        ControllerFixture fixture = CreateFixture(CreateMethodology(), missingVersion: true);

        IActionResult response = await fixture.Controller.GetByVersionAsync("missing", CancellationToken.None);

        ObjectResult notFound = Assert.IsType<ObjectResult>(response);
        Assert.Equal(404, notFound.StatusCode);
        fixture.VersionHandler.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldBePublicAndUseTheReferenceDataCacheOnEveryAction()
    {
        AllowAnonymousAttribute allowAnonymous = Assert.Single(
            typeof(RatingMethodologiesController).GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.NotNull(allowAnonymous);

        string[] actionNames =
        {
            nameof(RatingMethodologiesController.ListAsync),
            nameof(RatingMethodologiesController.GetCurrentAsync),
            nameof(RatingMethodologiesController.GetByVersionAsync),
        };
        foreach (string actionName in actionNames)
        {
            MethodInfo action = typeof(RatingMethodologiesController).GetMethod(actionName)!;
            OutputCacheAttribute cache = Assert.Single(action.GetCustomAttributes<OutputCacheAttribute>());
            Assert.Equal(ApiOutputCachePolicyNames.PublicReferenceData, cache.PolicyName);
        }
    }

    private static ControllerFixture CreateFixture(
        RatingMethodologyResult methodology,
        bool missingVersion = false)
    {
        Mock<IQueryHandler<GetCurrentRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>> currentHandler =
            new Mock<IQueryHandler<GetCurrentRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>>(MockBehavior.Strict);
        Mock<IQueryHandler<GetRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>> versionHandler =
            new Mock<IQueryHandler<GetRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>>(MockBehavior.Strict);
        Mock<IQueryHandler<ListRatingMethodologiesQuery, ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>>> listHandler =
            new Mock<IQueryHandler<ListRatingMethodologiesQuery, ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>>>(MockBehavior.Strict);
        currentHandler.Setup(handler => handler.HandleAsync(It.IsAny<GetCurrentRatingMethodologyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<RatingMethodologyResult>.Success(methodology));
        if (missingVersion)
        {
            versionHandler.Setup(handler => handler.HandleAsync(It.IsAny<GetRatingMethodologyQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApplicationResult<RatingMethodologyResult>.Failure(RatingApplicationErrors.MethodologyNotFound()));
        }

        RatingMethodologiesController controller = new RatingMethodologiesController(
            currentHandler.Object,
            versionHandler.Object,
            listHandler.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return new ControllerFixture(controller, currentHandler, versionHandler);
    }

    private static RatingMethodologyResult CreateMethodology()
    {
        return new RatingMethodologyResult(
            RatingMethodologyVersion.Parse("ratings-2026-01"),
            new DateOnly(2026, 8, 31),
            true,
            null,
            0.5m,
            5m,
            0.5m,
            3.5d,
            10,
            0.7d,
            0.3d,
            true,
            3,
            10,
            30,
            100,
            3,
            5,
            2,
            2,
            0.0001m,
            "competition");
    }

    private sealed record ControllerFixture(
        RatingMethodologiesController Controller,
        Mock<IQueryHandler<GetCurrentRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>> CurrentHandler,
        Mock<IQueryHandler<GetRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>> VersionHandler);
}
