using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ImagesControllerTests
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("other-user", "MODERATOR", false)]
    [InlineData("draft-owner", "USER", false)]
    [InlineData("draft-owner", "MODERATOR", true)]
    [InlineData("draft-owner", "ADMIN", true)]
    public void DraftBinary_ShouldOnlyBeReadableByItsStaffOwner(
        string? userId,
        string? role,
        bool expected)
    {
        List<Claim> claims = new List<Claim>();
        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        ClaimsPrincipal user = new ClaimsPrincipal(
            new ClaimsIdentity(claims, userId is null ? null : "test"));
        Image draft = new Image
        {
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "draft-owner",
            IsPublished = false,
        };

        Assert.Equal(expected, ImagesController.CanReadOwnCommentDraft(draft, user));
    }

    [Theory]
    [InlineData(ImageOwnerTypeDto.COMMENT, ImageCategoryDto.PARK)]
    [InlineData(ImageOwnerTypeDto.COMMENT_DRAFT, ImageCategoryDto.PARK)]
    [InlineData(ImageOwnerTypeDto.PARK, ImageCategoryDto.COMMENT)]
    public void AnonymousOwnerRoutes_WhenCommentImageIsRequested_ShouldBeHidden(
        ImageOwnerTypeDto ownerType,
        ImageCategoryDto category)
    {
        Assert.True(ImagesController.IsCommentImageOwnerRoute(ownerType, category));
    }

    [Fact]
    public void AnonymousOwnerRoutes_WhenPublicParkImageIsRequested_ShouldRemainAvailable()
    {
        Assert.False(ImagesController.IsCommentImageOwnerRoute(
            ImageOwnerTypeDto.PARK,
            ImageCategoryDto.PARK));
    }

    [Fact]
    public void GetImageAsync_ShouldExposeAnonymousGetAndHeadRoutes()
    {
        MethodInfo method = typeof(ImagesController).GetMethod(nameof(ImagesController.GetImageAsync))
            ?? throw new InvalidOperationException("ImagesController.GetImageAsync was not found.");

        Assert.Contains(method.GetCustomAttributes<HttpGetAttribute>(), static attribute => attribute.Template == "{imageId}");
        Assert.Contains(method.GetCustomAttributes<HttpHeadAttribute>(), static attribute => attribute.Template == "{imageId}");
        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task GetSocialPreviewImageAsync_ShouldUseStableJpegVariantWithoutContentNegotiation()
    {
        byte[] imageContent = new byte[131_089];
        for (int index = 0; index < imageContent.Length; index++)
        {
            imageContent[index] = (byte)(index % 251);
        }

        Image image = new Image
        {
            Id = "image-1",
            Path = "images/image-1",
            IsPublished = true,
        };
        Mock<IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>> queryHandler =
            new Mock<IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>>(MockBehavior.Strict);
        queryHandler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<GetImageByIdQuery>(query => query.ImageId == "image-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<Image>.Success(image));
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage
            .Setup(candidate => candidate.GetSocialPreviewAsync(
                "images/image-1",
                ImagesController.SocialPreviewImageWidth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((imageContent, "image/jpeg"));
        ImagesController controller = CreateController(queryHandler.Object, storage.Object);

        IActionResult result = await controller.GetSocialPreviewImageAsync("image-1", CancellationToken.None);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Same(imageContent, file.FileContents);
        Assert.Equal("public,max-age=0,must-revalidate", controller.Response.Headers.CacheControl);
        Assert.False(controller.Response.Headers.ContainsKey("Vary"));

        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DefaultHttpContext responseContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
        };
        await using MemoryStream responseBody = new MemoryStream();
        responseContext.Response.Body = responseBody;
        ActionContext actionContext = new ActionContext(
            responseContext,
            new RouteData(),
            new ActionDescriptor());

        await file.ExecuteResultAsync(actionContext);

        Assert.True(imageContent.Length > 64 * 1024);
        Assert.Equal(imageContent.Length, responseContext.Response.ContentLength);
        Assert.Equal(imageContent.Length, responseBody.Length);
        Assert.Equal(imageContent, responseBody.ToArray());
        queryHandler.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task GetSocialPreviewImageAsync_ForHead_ShouldUseMetadataOnly()
    {
        Image image = new Image
        {
            Id = "image-1",
            Path = "images/image-1",
            IsPublished = true,
        };
        Mock<IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>> queryHandler =
            new Mock<IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>>(MockBehavior.Strict);
        queryHandler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<GetImageByIdQuery>(query => query.ImageId == "image-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<Image>.Success(image));
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage
            .Setup(candidate => candidate.GetSocialPreviewMetadataAsync(
                "images/image-1",
                ImagesController.SocialPreviewImageWidth,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((123L, "image/jpeg"));
        ImagesController controller = CreateController(queryHandler.Object, storage.Object);
        controller.Request.Method = HttpMethods.Head;

        IActionResult result = await controller.GetSocialPreviewImageAsync(
            "image-1",
            CancellationToken.None);

        Assert.IsType<EmptyResult>(result);
        Assert.Equal(123L, controller.Response.ContentLength);
        Assert.Equal("image/jpeg", controller.Response.ContentType);
        Assert.Equal("public,max-age=0,must-revalidate", controller.Response.Headers.CacheControl);
        Assert.False(controller.Response.Headers.ContainsKey("Vary"));
        queryHandler.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task GetSocialPreviewImageAsync_WhenImageIsNotPublished_ShouldRemainHidden()
    {
        Image image = new Image
        {
            Id = "image-1",
            Path = "images/image-1",
            IsPublished = false,
        };
        Mock<IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>> queryHandler =
            new Mock<IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>>(MockBehavior.Strict);
        queryHandler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<GetImageByIdQuery>(query => query.ImageId == "image-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<Image>.Success(image));
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        ImagesController controller = CreateController(queryHandler.Object, storage.Object);

        IActionResult result = await controller.GetSocialPreviewImageAsync("image-1", CancellationToken.None);

        ObjectResult notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        queryHandler.VerifyAll();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetSocialPreviewImageAsync_ShouldExposeVersionedAnonymousGetAndHeadRoutes()
    {
        MethodInfo method = typeof(ImagesController).GetMethod(nameof(ImagesController.GetSocialPreviewImageAsync))
            ?? throw new InvalidOperationException("ImagesController.GetSocialPreviewImageAsync was not found.");

        Assert.Contains(
            method.GetCustomAttributes<HttpGetAttribute>(),
            static attribute => attribute.Template == "binary/{imageId}/social-preview-v1");
        Assert.Contains(
            method.GetCustomAttributes<HttpHeadAttribute>(),
            static attribute => attribute.Template == "binary/{imageId}/social-preview-v1");
        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(ImagesController.UploadAsync))]
    [InlineData(nameof(ImagesController.ImportRemoteAsync))]
    public void ImageImportEndpoints_ShouldUseTheSharedProcessingQueue(string methodName)
    {
        MethodInfo method = typeof(ImagesController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"ImagesController.{methodName} was not found.");

        EnableRateLimitingAttribute attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>()
            ?? throw new InvalidOperationException($"ImagesController.{methodName} has no rate limiting policy.");

        Assert.Equal(RateLimitPolicyNames.ImageUploadProcessing, attribute.PolicyName);
    }

    private static ImagesController CreateController(
        IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>> queryHandler,
        IImageBinaryStorage storage)
    {
        ImagesController controller = new ImagesController(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            queryHandler,
            null!,
            null!,
            null!,
            null!,
            null!,
            storage);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }
}
