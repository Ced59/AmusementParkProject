using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class SharePublicationsControllerTests
{
    [Fact]
    public async Task PreviewAsync_ShouldUseAuthenticatedOwnerAndReturnPublicContract()
    {
        SharePublicationPreviewResult preview = new SharePublicationPreviewResult(
            SharePublicationType.PersonalRanking,
            8,
            1,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.GlobalRatings },
            new PersonalRankingSharePreviewResult(
                null,
                null,
                null,
                Array.Empty<PersonalRankingSharePreviewItemResult>(),
                false));
        Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>> handler =
            new Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<PreviewSharePublicationQuery>(query =>
                    query.OwnerUserId == "owner-1"
                    && query.PublicationType == SharePublicationType.PersonalRanking
                    && query.IncludedFields.SequenceEqual(new[] { ShareContentField.GlobalRatings })),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationPreviewResult>.Success(preview));
        SharePublicationsController controller = CreateController(handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.PreviewAsync(
            new SharePublicationPreviewRequestDto
            {
                PublicationType = "PersonalRanking",
                DatePrecision = "Hidden",
                IncludedFields = new List<string> { "GlobalRatings" },
            },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SharePublicationPreviewDto response = Assert.IsType<SharePublicationPreviewDto>(ok.Value);
        Assert.Equal("PersonalRanking", response.PublicationType);
        Assert.Equal(8, response.SourceVersion);
        Assert.DoesNotContain(
            response.ContentPolicy.IncludedFields,
            static field => field.Contains("Private", StringComparison.OrdinalIgnoreCase));
        handler.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenEnumIsInvalid_ShouldReturnBadRequestWithoutCallingApplication()
    {
        Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>> handler =
            new Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>>(MockBehavior.Strict);
        SharePublicationsController controller = CreateController(handler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.PreviewAsync(
            new SharePublicationPreviewRequestDto
            {
                PublicationType = "InternalSecrets",
                DatePrecision = "Hidden",
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        handler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PreviewAsync_WhenRollingCandidateIsNotCompatible_ShouldReturnServiceUnavailable()
    {
        Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>> handler =
            new Mock<IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>>>(MockBehavior.Strict);
        SharePublicationsController controller = CreateController(handler.Object, false);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.PreviewAsync(
            new SharePublicationPreviewRequestDto(),
            CancellationToken.None);

        StatusCodeResult unavailable = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        handler.VerifyNoOtherCalls();
    }

    [Fact]
    public void PreviewEndpoint_ShouldRequireAnActivatedAccountDisableCachingAndApplyTargetedRateLimit()
    {
        MethodInfo action = typeof(SharePublicationsController).GetMethod(
            nameof(SharePublicationsController.PreviewAsync))
            ?? throw new InvalidOperationException("Preview action not found.");

        AuthorizeAttribute authorize = Assert.Single(
            action.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.False(string.IsNullOrWhiteSpace(authorize.Roles));
        Assert.NotNull(action.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        ResponseCacheAttribute cache = Assert.IsType<ResponseCacheAttribute>(
            action.GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(cache.NoStore);
        Assert.Equal(ResponseCacheLocation.None, cache.Location);
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.SharePublicationPreviews, rateLimit.PolicyName);
    }

    private static ControllerContext CreateControllerContext(string userId)
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "USER"),
            },
            "Test");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity),
            },
        };
    }

    private static SharePublicationsController CreateController(
        IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>> handler,
        bool enabled = true)
    {
        return new SharePublicationsController(
            handler,
            Options.Create(new SharePublicationRolloutSettings { Enabled = enabled }));
    }
}
