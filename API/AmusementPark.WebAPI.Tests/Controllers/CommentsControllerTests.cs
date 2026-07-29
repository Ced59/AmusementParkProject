using System.Reflection;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
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
    [InlineData(nameof(CommentsController.GetSummaryAsync))]
    [InlineData(nameof(CommentsController.GetThreadAsync))]
    public void PublicReads_ShouldRemainAnonymous(string methodName)
    {
        MethodInfo method = typeof(CommentsController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"{methodName} was not found.");

        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
