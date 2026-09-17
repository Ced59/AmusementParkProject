using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminNotificationEmailDeliveriesControllerTests
{
    [Fact]
    public void Controller_ShouldRequireAdminAndActivatedAccount()
    {
        Type controllerType = typeof(AdminNotificationEmailDeliveriesController);
        AuthorizeAttribute authorize = Assert.Single(
            controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));

        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.Single(controllerType.GetCustomAttributes(
            typeof(RequireActivatedUnblockedUserAttribute),
            inherit: true));
    }
}
