using System.Reflection;
using System.Security.Claims;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
