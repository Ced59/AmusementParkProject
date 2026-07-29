using System.Reflection;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using AmusementPark.WebAPI.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class CommentsControllerTests
{
    [Fact]
    public void CreateAsync_ShouldAllowOnlyModeratorsAndAdministrators()
    {
        MethodInfo method = typeof(CommentsController).GetMethod(nameof(CommentsController.CreateAsync))
            ?? throw new InvalidOperationException("CommentsController.CreateAsync was not found.");
        AuthorizeAttribute attribute = method
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(candidate => candidate.Roles is not null);

        Assert.Equal(AuthorizationRoleGroups.ModeratorAdmin, attribute.Roles);
    }

    [Theory]
    [InlineData(nameof(CommentsController.UpdateAsync))]
    [InlineData(nameof(CommentsController.DeleteAsync))]
    public void CommentManagement_ShouldAllowOwnersAndAdministrators(string methodName)
    {
        MethodInfo method = typeof(CommentsController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"{methodName} was not found.");
        AuthorizeAttribute attribute = method
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(candidate => candidate.Roles is not null);

        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, attribute.Roles);
        Assert.NotNull(method.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.NotNull(method.GetCustomAttribute<AdminAuditAttribute>());
        Assert.NotNull(method.GetCustomAttribute<InvalidatesPublicCacheAttribute>());
    }

    [Theory]
    [InlineData(nameof(CommentsController.GetSummaryAsync))]
    [InlineData(nameof(CommentsController.GetThreadAsync))]
    public void PublicReads_ShouldRemainAnonymous(string methodName)
    {
        MethodInfo method = typeof(CommentsController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"{methodName} was not found.");

        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(CommentsController.UploadImageAsync))]
    [InlineData(nameof(CommentsController.DeleteDraftImageAsync))]
    public void CommentImageMutations_ShouldAllowOnlyModeratorsAndAdministrators(string methodName)
    {
        MethodInfo method = typeof(CommentsController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"{methodName} was not found.");
        AuthorizeAttribute attribute = method
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(candidate => candidate.Roles is not null);

        Assert.Equal(AuthorizationRoleGroups.ModeratorAdmin, attribute.Roles);
        Assert.NotNull(method.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.NotNull(method.GetCustomAttribute<AdminAuditAttribute>());
    }

    [Fact]
    public void UploadImageAsync_ShouldUseSharedImageProcessingRateLimit()
    {
        MethodInfo method = typeof(CommentsController).GetMethod(nameof(CommentsController.UploadImageAsync))
            ?? throw new InvalidOperationException("CommentsController.UploadImageAsync was not found.");
        EnableRateLimitingAttribute attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>()
            ?? throw new InvalidOperationException("The upload endpoint has no rate limiting policy.");

        Assert.Equal(RateLimitPolicyNames.ImageUploadProcessing, attribute.PolicyName);
    }
}
