using System.Reflection;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class SharedPublicationSocialImagesControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnTheVersionedImageWithBoundedPublicCaching()
    {
        ShareSocialImageRenderResult image = new ShareSocialImageRenderResult(
            new byte[] { 1, 2, 3 },
            "image/png",
            "Mon récap de visite",
            "\"etag-1\"");
        Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>> handler =
            new Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.Is<GetSharedPublicationSocialImageQuery>(query =>
                    query.ShareId == "opaque-token"
                    && query.PublicationType == SharePublicationType.VisitRecap
                    && query.PublicationVersion == 7
                    && query.Language == "fr"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ShareSocialImageRenderResult>.Success(image));
        SharedPublicationSocialImagesController controller = CreateController(handler);

        IActionResult result = await controller.GetAsync(
            "visit",
            "opaque-token",
            7,
            1,
            "fr",
            CancellationToken.None);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(image.Content, file.FileContents);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal("public,max-age=300,must-revalidate", controller.Response.Headers.CacheControl);
        Assert.Equal("\"etag-1\"", controller.Response.Headers.ETag);
        Assert.Equal("fr", controller.Response.Headers.ContentLanguage);
        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        handler.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_WhenTheBrowserAlreadyHasTheCurrentImage_ShouldReturnNotModified()
    {
        ShareSocialImageRenderResult image = new ShareSocialImageRenderResult(
            new byte[] { 1, 2, 3 },
            "image/png",
            "My visit recap",
            "\"etag-1\"");
        Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>> handler =
            new Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.IsAny<GetSharedPublicationSocialImageQuery>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ShareSocialImageRenderResult>.Success(image));
        SharedPublicationSocialImagesController controller = CreateController(handler);
        controller.Request.Headers.IfNoneMatch = "\"older\", \"etag-1\"";

        IActionResult result = await controller.GetAsync(
            "passport",
            "opaque-token",
            7,
            1,
            "en",
            CancellationToken.None);

        StatusCodeResult notModified = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, notModified.StatusCode);
    }

    [Theory]
    [InlineData("W/\"etag-1\"")]
    [InlineData("*")]
    public async Task GetAsync_WhenTheBrowserUsesAValidConditionalTag_ShouldReturnNotModified(
        string conditionalTag)
    {
        ShareSocialImageRenderResult image = new ShareSocialImageRenderResult(
            new byte[] { 1, 2, 3 },
            "image/png",
            "My visit recap",
            "\"etag-1\"");
        Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>> handler =
            new Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>>(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                It.IsAny<GetSharedPublicationSocialImageQuery>(),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<ShareSocialImageRenderResult>.Success(image));
        SharedPublicationSocialImagesController controller = CreateController(handler);
        controller.Request.Headers.IfNoneMatch = conditionalTag;

        IActionResult result = await controller.GetAsync(
            "passport",
            "opaque-token",
            7,
            1,
            "en",
            CancellationToken.None);

        StatusCodeResult notModified = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, notModified.StatusCode);
    }

    [Theory]
    [InlineData("comparison", 7, 1)]
    [InlineData("visit", 0, 1)]
    [InlineData("visit", 7, 2)]
    public async Task GetAsync_WhenTheRouteDoesNotIdentifyACurrentTemplate_ShouldReturnNotFound(
        string publicationType,
        long publicationVersion,
        int templateVersion)
    {
        Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>> handler =
            new Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>>(MockBehavior.Strict);
        SharedPublicationSocialImagesController controller = CreateController(handler);

        IActionResult result = await controller.GetAsync(
            publicationType,
            "opaque-token",
            publicationVersion,
            templateVersion,
            "fr",
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        handler.Verify(value => value.HandleAsync(
            It.IsAny<GetSharedPublicationSocialImageQuery>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetEndpoint_ShouldBeAnonymousAndBoundItsRenderingConcurrency()
    {
        MethodInfo action = typeof(SharedPublicationSocialImagesController).GetMethod(
            nameof(SharedPublicationSocialImagesController.GetAsync))
            ?? throw new InvalidOperationException("Get action not found.");

        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
        EnableRateLimitingAttribute rateLimit = Assert.IsType<EnableRateLimitingAttribute>(
            action.GetCustomAttribute<EnableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicyNames.ShareSocialImageRendering, rateLimit.PolicyName);
    }

    private static SharedPublicationSocialImagesController CreateController(
        Mock<IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>>> handler)
    {
        return new SharedPublicationSocialImagesController(handler.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
    }
}
