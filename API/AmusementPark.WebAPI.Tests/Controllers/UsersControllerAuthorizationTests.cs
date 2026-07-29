using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Queries;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class UsersControllerAuthorizationTests
{
    [Fact]
    public async Task UpdateUserAsync_WhenModeratorTargetsAnotherAccount_ShouldReturnForbidden()
    {
        Mock<ICommandHandler<UpdateUserProfileCommand, ApplicationResult<User>>> updateHandler =
            new Mock<ICommandHandler<UpdateUserProfileCommand, ApplicationResult<User>>>(
                MockBehavior.Strict);
        UsersController controller = CreateController(updateHandler);
        controller.ControllerContext = CreateControllerContext("moderator-1", "MODERATOR");

        IActionResult result = await controller.UpdateUserAsync(
            "admin-1",
            new UserUpdateDto());

        ObjectResult objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        updateHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateUserAsync_WhenAdministratorTargetsAnotherAccount_ShouldDelegateUpdate()
    {
        User updatedUser = new User
        {
            Id = "user-1",
            Email = "user@example.com",
            Roles = new List<Role> { Role.User },
        };
        Mock<ICommandHandler<UpdateUserProfileCommand, ApplicationResult<User>>> updateHandler =
            new Mock<ICommandHandler<UpdateUserProfileCommand, ApplicationResult<User>>>(
                MockBehavior.Strict);
        updateHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<UpdateUserProfileCommand>(
                    command => command.UserId == "user-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<User>.Success(updatedUser));
        UsersController controller = CreateController(updateHandler);
        controller.ControllerContext = CreateControllerContext("admin-1", "ADMIN");

        IActionResult result = await controller.UpdateUserAsync(
            "user-1",
            new UserUpdateDto());

        Assert.IsType<OkObjectResult>(result);
        updateHandler.VerifyAll();
    }

    private static ControllerContext CreateControllerContext(string userId, string role)
    {
        ClaimsIdentity identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
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

    private static UsersController CreateController(
        Mock<ICommandHandler<UpdateUserProfileCommand, ApplicationResult<User>>> updateHandler)
    {
        return new UsersController(
            new Mock<ICommandHandler<RegisterLocalUserCommand, ApplicationResult<User>>>(
                MockBehavior.Strict).Object,
            new Mock<IQueryHandler<GetUserByEmailQuery, ApplicationResult<User>>>(
                MockBehavior.Strict).Object,
            new Mock<IQueryHandler<GetUserByIdQuery, ApplicationResult<User>>>(
                MockBehavior.Strict).Object,
            new Mock<IQueryHandler<GetUsersPageQuery, ApplicationResult<PagedResult<User>>>>(
                MockBehavior.Strict).Object,
            updateHandler.Object,
            new Mock<ICommandHandler<ChangePasswordCommand, ApplicationResult>>(
                MockBehavior.Strict).Object,
            new Mock<ICommandHandler<ConfirmEmailCommand, ApplicationResult<User>>>(
                MockBehavior.Strict).Object,
            new Mock<ICommandHandler<ResendConfirmationEmailCommand, ApplicationResult>>(
                MockBehavior.Strict).Object,
            new Mock<ICommandHandler<ForgotPasswordCommand, ApplicationResult>>(
                MockBehavior.Strict).Object,
            new Mock<ICommandHandler<ResetPasswordCommand, ApplicationResult>>(
                MockBehavior.Strict).Object,
            new Mock<ICommandHandler<AssignRoleCommand, ApplicationResult<User>>>(
                MockBehavior.Strict).Object,
            new Mock<ICommandHandler<RemoveRoleCommand, ApplicationResult<User>>>(
                MockBehavior.Strict).Object,
            new Mock<ICommandHandler<LockUserCommand, ApplicationResult<User>>>(
                MockBehavior.Strict).Object,
            new Mock<ICommandHandler<UnlockUserCommand, ApplicationResult<User>>>(
                MockBehavior.Strict).Object);
    }
}
