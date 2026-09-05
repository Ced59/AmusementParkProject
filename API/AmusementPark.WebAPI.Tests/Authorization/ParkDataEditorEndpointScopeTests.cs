using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Authorization;

public sealed class ParkDataEditorEndpointScopeTests
{
    [Fact]
    public void ParkGraphUpsertsController_ShouldRequireAdminOrDedicatedTokenAndExplicitMarker()
    {
        AuthorizeAttribute authorize = typeof(ParkGraphUpsertsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy == AuthorizationPolicyNames.AdminOrParkDataEditorToken);

        Assert.Equal(AuthorizationPolicyNames.AdminOrParkDataEditorToken, authorize.Policy);
        Assert.NotNull(typeof(ParkGraphUpsertsController).GetCustomAttribute<AllowParkDataEditorTokenAttribute>());
    }

    [Fact]
    public void ParkDataEditorOperationsController_ShouldRequireDedicatedTokenAndExplicitMarker()
    {
        AuthorizeAttribute authorize = typeof(ParkDataEditorOperationsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy == AuthorizationPolicyNames.ParkDataEditorToken);

        Assert.Equal(AuthorizationPolicyNames.ParkDataEditorToken, authorize.Policy);
        Assert.NotNull(typeof(ParkDataEditorOperationsController).GetCustomAttribute<AllowParkDataEditorTokenAttribute>());
    }

    [Fact]
    public void ParkDataEditorOfficialMapsController_ShouldRequireDedicatedTokenAndExplicitMarker()
    {
        AuthorizeAttribute authorize = typeof(ParkDataEditorOfficialMapsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy == AuthorizationPolicyNames.ParkDataEditorToken);

        Assert.Equal(AuthorizationPolicyNames.ParkDataEditorToken, authorize.Policy);
        Assert.NotNull(typeof(ParkDataEditorOfficialMapsController).GetCustomAttribute<AllowParkDataEditorTokenAttribute>());
    }

    [Fact]
    public void ParkDataEditorSocialPublicationsController_ShouldRequireAdminOrDedicatedTokenAndExplicitMarker()
    {
        AuthorizeAttribute authorize = typeof(ParkDataEditorSocialPublicationsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(attribute => attribute.Policy == AuthorizationPolicyNames.AdminOrParkDataEditorToken);

        Assert.Equal(AuthorizationPolicyNames.AdminOrParkDataEditorToken, authorize.Policy);
        Assert.NotNull(typeof(ParkDataEditorSocialPublicationsController).GetCustomAttribute<AllowParkDataEditorTokenAttribute>());
    }

    [Theory]
    [InlineData(ImageCategory.Park, ImageOwnerType.Park, true)]
    [InlineData(ImageCategory.Logo, ImageOwnerType.Park, true)]
    [InlineData(ImageCategory.ParkItem, ImageOwnerType.ParkItem, true)]
    [InlineData(ImageCategory.StandaloneAttraction, ImageOwnerType.StandaloneAttraction, true)]
    [InlineData(ImageCategory.Avatar, ImageOwnerType.User, false)]
    [InlineData(ImageCategory.Park, ImageOwnerType.User, false)]
    public void ParkDataEditorImages_ShouldRestrictCategoryAndOwner(
        ImageCategory category,
        ImageOwnerType ownerType,
        bool expected)
    {
        Assert.Equal(expected, ParkDataEditorImagesController.IsAllowedOwnership(category, ownerType, "owner-id"));
    }

    [Theory]
    [InlineData(ImageCategory.Park, ImageOwnerType.None, null, true)]
    [InlineData(ImageCategory.Park, ImageOwnerType.Park, "park-1", true)]
    [InlineData(ImageCategory.Avatar, ImageOwnerType.User, "user-1", false)]
    [InlineData(ImageCategory.Logo, ImageOwnerType.ParkOperator, "operator-1", false)]
    [InlineData(ImageCategory.VideoThumbnail, ImageOwnerType.Video, "video-1", false)]
    public void ParkDataEditorImages_ShouldValidateTheCurrentImageScope(
        ImageCategory category,
        ImageOwnerType ownerType,
        string? ownerId,
        bool expected)
    {
        Image image = new Image
        {
            Category = category,
            OwnerType = ownerType,
            OwnerId = ownerId,
        };

        Assert.Equal(expected, ParkDataEditorImagesController.IsAllowedImageScope(image));
    }

    [Fact]
    public async Task UpdateMetadata_ShouldRejectReclassificationOfAnOutOfScopeImage()
    {
        Mock<IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>> getImageHandler =
            new Mock<IQueryHandler<GetImageByIdQuery, ApplicationResult<Image>>>(MockBehavior.Strict);
        getImageHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<GetImageByIdQuery>(query => query.ImageId == "avatar-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<Image>.Success(new Image
            {
                Id = "avatar-1",
                Category = ImageCategory.Avatar,
                OwnerType = ImageOwnerType.User,
                OwnerId = "user-1",
            }));
        Mock<ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>> updateMetadataHandler =
            new Mock<ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>>(MockBehavior.Strict);
        ParkDataEditorImagesController controller = new ParkDataEditorImagesController(
            new Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>(MockBehavior.Strict).Object,
            new Mock<ICommandHandler<LinkImageCommand, ApplicationResult<Image>>>(MockBehavior.Strict).Object,
            new Mock<ICommandHandler<SetCurrentImageCommand, ApplicationResult<Image>>>(MockBehavior.Strict).Object,
            updateMetadataHandler.Object,
            getImageHandler.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        IActionResult result = await controller.UpdateMetadataAsync(
            "avatar-1",
            new UpdateImageAssetRequest
            {
                Category = ImageCategoryDto.PARK,
                OwnerType = ImageOwnerTypeDto.PARK,
                OwnerId = "park-1",
            },
            CancellationToken.None);

        ObjectResult forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        updateMetadataHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public void ParkDataEditorImageCreateDto_ShouldDisableWatermarkByDefault()
    {
        ParkDataEditorImageCreateDto request = new ParkDataEditorImageCreateDto();

        Assert.False(request.WithWatermark);
    }

    [Fact]
    public async Task RestrictedTokenRequirement_ShouldRejectDedicatedTokenOutsideMarkedEndpoint()
    {
        RestrictedParkDataEditorTokenAuthorizationHandler handler = new RestrictedParkDataEditorTokenAuthorizationHandler();
        AuthorizationHandlerContext context = new AuthorizationHandlerContext(
            new[] { RestrictedParkDataEditorTokenRequirement.Instance },
            CreateParkDataEditorPrincipal(includeAuthenticationMethod: true),
            new DefaultHttpContext());

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task RestrictedTokenRequirement_ShouldAllowDedicatedTokenOnMarkedEndpoint()
    {
        RestrictedParkDataEditorTokenAuthorizationHandler handler = new RestrictedParkDataEditorTokenAuthorizationHandler();
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(
            static _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowParkDataEditorTokenAttribute()),
            "park-data-editor"));
        AuthorizationHandlerContext context = new AuthorizationHandlerContext(
            new[] { RestrictedParkDataEditorTokenRequirement.Instance },
            CreateParkDataEditorPrincipal(includeAuthenticationMethod: true),
            httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task AdminOrDedicatedTokenRequirement_ShouldRejectParkDataEditorJwt()
    {
        AdminOrParkDataEditorTokenAuthorizationHandler handler = new AdminOrParkDataEditorTokenAuthorizationHandler();
        AuthorizationHandlerContext context = new AuthorizationHandlerContext(
            new[] { AdminOrParkDataEditorTokenRequirement.Instance },
            CreateParkDataEditorPrincipal(includeAuthenticationMethod: false),
            resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreateParkDataEditorPrincipal(bool includeAuthenticationMethod)
    {
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, AuthorizationRoleGroups.ParkDataEditor),
        };
        if (includeAuthenticationMethod)
        {
            claims.Add(new Claim(
                ParkDataEditorAuthenticationDefaults.AuthenticationMethodClaim,
                ParkDataEditorAuthenticationDefaults.AuthenticationMethod));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
