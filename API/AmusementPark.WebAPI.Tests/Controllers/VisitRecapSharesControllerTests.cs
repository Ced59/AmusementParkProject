using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class VisitRecapSharesControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldUseTheAuthenticatedOwnerAndRequestedVisit()
    {
        Mock<IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>>> queryHandler =
            new Mock<IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict);
        queryHandler.Setup(value => value.HandleAsync(
                It.Is<GetSharePublicationSettingsQuery>(query =>
                    query.UserId == "owner-1"
                    && query.PublicationType == SharePublicationType.VisitRecap
                    && query.SourceId == "visit-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationSettingsResult>.Success(
                new SharePublicationSettingsResult(
                    false,
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<ShareContentField>())));
        VisitRecapSharesController controller = CreateController(queryHandler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.GetAsync("visit-1", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SharePublicationSettingsDto response = Assert.IsType<SharePublicationSettingsDto>(ok.Value);
        Assert.False(response.IsPublic);
        queryHandler.VerifyAll();
    }

    [Fact]
    public async Task RevokeAsync_ShouldRevokeOnlyTheAuthenticatedOwnersVisitShare()
    {
        Mock<ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>> commandHandler =
            new Mock<ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict);
        commandHandler.Setup(value => value.HandleAsync(
                It.Is<SetSharePublicationVisibilityCommand>(command =>
                    command.UserId == "owner-1"
                    && command.PublicationType == SharePublicationType.VisitRecap
                    && command.SourceId == "visit-1"
                    && !command.IsPublic),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharePublicationSettingsResult>.Success(
                new SharePublicationSettingsResult(
                    false,
                    null,
                    null,
                    1,
                    ShareDatePrecision.Month,
                    new[] { ShareContentField.RideCount })));
        VisitRecapSharesController controller = CreateController(commandHandler: commandHandler.Object);
        controller.ControllerContext = CreateControllerContext("owner-1");

        IActionResult result = await controller.RevokeAsync("visit-1", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SharePublicationSettingsDto response = Assert.IsType<SharePublicationSettingsDto>(ok.Value);
        Assert.False(response.IsPublic);
        commandHandler.VerifyAll();
    }

    [Fact]
    public void Endpoints_ShouldRemainAuthenticatedNoStoreAndInvalidateOnRevoke()
    {
        Type controller = typeof(VisitRecapSharesController);
        AuthorizeAttribute authorize = Assert.Single(
            controller.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));
        Assert.False(string.IsNullOrWhiteSpace(authorize.Roles));
        Assert.NotNull(controller.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());

        MethodInfo get = GetAction(nameof(VisitRecapSharesController.GetAsync));
        ResponseCacheAttribute getCache = Assert.IsType<ResponseCacheAttribute>(
            get.GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(getCache.NoStore);

        MethodInfo revoke = GetAction(nameof(VisitRecapSharesController.RevokeAsync));
        Assert.NotNull(revoke.GetCustomAttribute<InvalidatesPublicCacheAttribute>());
        ResponseCacheAttribute revokeCache = Assert.IsType<ResponseCacheAttribute>(
            revoke.GetCustomAttribute<ResponseCacheAttribute>());
        Assert.True(revokeCache.NoStore);
    }

    private static MethodInfo GetAction(string name)
    {
        return typeof(VisitRecapSharesController).GetMethod(name)
            ?? throw new InvalidOperationException($"Action {name} was not found.");
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

    private static VisitRecapSharesController CreateController(
        IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>>? queryHandler = null,
        ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>? commandHandler = null)
    {
        return new VisitRecapSharesController(
            queryHandler
                ?? new Mock<IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict).Object,
            commandHandler
                ?? new Mock<ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>>(MockBehavior.Strict).Object);
    }
}
