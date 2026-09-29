using System.Reflection;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class FeatureFlagControllersTests
{
    [Fact]
    public void AdminController_ShouldRequireAdminAndAuditMutations()
    {
        Type type = typeof(AdminFeatureFlagsController);

        Assert.Equal("admin/feature-flags", type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorization = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorization.Roles);
        Assert.NotNull(type.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        MethodInfo update = type.GetMethod(nameof(AdminFeatureFlagsController.UpdateAsync))!;
        Assert.Equal(
            RateLimitPolicyNames.LiveDataAdministration,
            update.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        AdminAuditAttribute audit = Assert.IsType<AdminAuditAttribute>(
            update.GetCustomAttribute<AdminAuditAttribute>());
        Assert.Equal("key", audit.TargetIdRouteKey);
    }

    [Fact]
    public void PublicController_ShouldExposeOnlyAnonymousNoStoreContract()
    {
        Type type = typeof(PublicCapabilitiesController);

        Assert.Equal("public/capabilities", type.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.NotNull(type.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
    }
}
