using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class UserLanguagePreferencesControllerTests
{
    [Fact]
    public void Controller_ShouldRequireAnActivatedAuthenticatedUser()
    {
        IEnumerable<AuthorizeAttribute> authorizeAttributes = typeof(UserLanguagePreferencesController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Where(attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        AuthorizeAttribute authorize = Assert.Single(authorizeAttributes);

        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.NotNull(typeof(UserLanguagePreferencesController)
            .GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIsAuthenticated_ShouldUpdateOwnPreference()
    {
        User updatedUser = new User
        {
            Id = "user-1",
            PreferredLanguage = "FR",
            Roles = new List<Role> { Role.User },
        };
        Mock<ICommandHandler<UpdatePreferredLanguageCommand, ApplicationResult<User>>> handler =
            new Mock<ICommandHandler<UpdatePreferredLanguageCommand, ApplicationResult<User>>>(MockBehavior.Strict);
        handler
            .Setup(commandHandler => commandHandler.HandleAsync(
                It.Is<UpdatePreferredLanguageCommand>(command =>
                    command.UserId == "user-1" && command.PreferredLanguage == "fr"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<User>.Success(updatedUser));
        UserLanguagePreferencesController controller = new UserLanguagePreferencesController(handler.Object)
        {
            ControllerContext = CreateControllerContext("user-1"),
        };

        IActionResult result = await controller.UpdateAsync(new PreferredLanguageUpdateDto
        {
            PreferredLanguage = "fr",
        });

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        UserUpdatedDto response = Assert.IsType<UserUpdatedDto>(okResult.Value);
        Assert.Equal("FR", response.PreferredLanguage);
        handler.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenUserIdentifierIsMissing_ShouldReturnUnauthorized()
    {
        Mock<ICommandHandler<UpdatePreferredLanguageCommand, ApplicationResult<User>>> handler =
            new Mock<ICommandHandler<UpdatePreferredLanguageCommand, ApplicationResult<User>>>(MockBehavior.Strict);
        UserLanguagePreferencesController controller = new UserLanguagePreferencesController(handler.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        IActionResult result = await controller.UpdateAsync(new PreferredLanguageUpdateDto
        {
            PreferredLanguage = "fr",
        });

        ObjectResult objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        handler.VerifyNoOtherCalls();
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
}
