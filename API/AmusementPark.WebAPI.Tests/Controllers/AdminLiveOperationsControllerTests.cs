using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.LiveData;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminLiveOperationsControllerTests
{
    [Fact]
    public void ScopeType_ShouldSerializeAsPublicContractName()
    {
        string json = JsonSerializer.Serialize(LiveOperationalScopeTypeDto.Target);

        Assert.Equal("\"Target\"", json);
    }

    [Fact]
    public void Controller_ShouldProtectAndAuditOperationalControlUpdates()
    {
        Type type = typeof(AdminLiveOperationsController);

        Assert.Equal("admin/live/operations", type.GetCustomAttribute<RouteAttribute>()?.Template);
        AuthorizeAttribute authorize = Assert.Single(
            type.GetCustomAttributes<AuthorizeAttribute>(),
            static attribute => attribute.GetType() == typeof(AuthorizeAttribute));
        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.NotNull(type.GetCustomAttribute<RequireActivatedUnblockedUserAttribute>());
        Assert.True(type.GetCustomAttribute<ResponseCacheAttribute>()?.NoStore);
        MethodInfo update = type.GetMethod(nameof(AdminLiveOperationsController.UpdateControlAsync))!;
        Assert.Equal(
            RateLimitPolicyNames.LiveDataAdministration,
            update.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.NotNull(update.GetCustomAttribute<AdminAuditAttribute>());
        InvalidatesPublicCacheAttribute cacheInvalidation = Assert.IsType<InvalidatesPublicCacheAttribute>(
            update.GetCustomAttribute<InvalidatesPublicCacheAttribute>());
        Assert.Equal(PublicCacheScope.LiveData, Assert.Single(cacheInvalidation.Scopes));
    }

    [Fact]
    public async Task UpdateControlAsync_ShouldUseAuthenticatedAdministrator()
    {
        Mock<IQueryHandler<
            GetAdminLiveOperationsQuery,
            ApplicationResult<LiveOperationsDashboardResult>>> queryHandler =
            new(MockBehavior.Strict);
        Mock<ICommandHandler<
            UpdateLiveOperationalControlCommand,
            ApplicationResult<LiveOperationalScopeResult>>> updateHandler =
            new(MockBehavior.Strict);
        LiveOperationalScopeResult updated = new LiveOperationalScopeResult(
            LiveOperationalScopeType.Source,
            "themeparks-wiki",
            null,
            null,
            null,
            null,
            "ThemeParks.wiki",
            null,
            false,
            true,
            false,
            true,
            2,
            "Maintenance fournisseur",
            "admin-1",
            new DateTime(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc));
        updateHandler
            .Setup(value => value.HandleAsync(
                It.Is<UpdateLiveOperationalControlCommand>(command =>
                    command.SourceId == "themeparks-wiki"
                    && command.ChangedByUserId == "admin-1"
                    && command.ExpectedRevision == 1
                    && !command.CollectionEnabled),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<LiveOperationalScopeResult>.Success(updated));
        AdminLiveOperationsController controller = new AdminLiveOperationsController(
            queryHandler.Object,
            updateHandler.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, "admin-1") },
                            "test")),
                },
            },
        };
        UpdateLiveOperationalControlRequestDto request = new UpdateLiveOperationalControlRequestDto
        {
            ScopeType = LiveOperationalScopeTypeDto.Source,
            SourceId = "themeparks-wiki",
            CollectionEnabled = false,
            PublicReadEnabled = true,
            ExpectedRevision = 1,
            Reason = "Maintenance fournisseur",
        };

        IActionResult action = await controller.UpdateControlAsync(request, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(action);
        LiveOperationalScopeDto response = Assert.IsType<LiveOperationalScopeDto>(ok.Value);
        Assert.Equal(2, response.Revision);
        Assert.False(response.CollectionEnabled);
        updateHandler.VerifyAll();
    }
}
