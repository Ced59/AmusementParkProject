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

public sealed class ShareModerationControllerSecurityTests
{
    [Fact]
    public void PublicReport_ShouldBeAnonymousRateLimitedAndNeverCached()
    {
        MethodInfo action = typeof(ShareModerationReportsController)
            .GetMethod(nameof(ShareModerationReportsController.SubmitAsync))!;

        Assert.NotNull(action.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal(
            RateLimitPolicyNames.ShareModerationReports,
            action.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.True(action.GetCustomAttribute<ResponseCacheAttribute>()!.NoStore);
    }

    [Fact]
    public void Administration_ShouldRequireAdminAndAuditRateLimitedMutations()
    {
        AuthorizeAttribute authorization = Assert.Single(
            typeof(AdminShareModerationController).GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));
        MethodInfo review = typeof(AdminShareModerationController)
            .GetMethod(nameof(AdminShareModerationController.ReviewAsync))!;

        Assert.Equal(AuthorizationRoleGroups.Admin, authorization.Roles);
        Assert.NotNull(typeof(AdminShareModerationController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.Equal(
            RateLimitPolicyNames.ShareModerationAdministration,
            review.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.NotNull(review.GetCustomAttribute<AdminAuditAttribute>());
    }
}
