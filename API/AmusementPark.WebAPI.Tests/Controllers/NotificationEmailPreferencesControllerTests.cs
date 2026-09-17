using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class NotificationEmailPreferencesControllerTests
{
    [Fact]
    public void Controller_ShouldRequireAnAuthenticatedActivatedAccount()
    {
        Type controllerType = typeof(NotificationEmailPreferencesController);
        AuthorizeAttribute authorize = Assert.Single(
            controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));

        Assert.Equal(AuthorizationRoleGroups.UserModeratorAdmin, authorize.Roles);
        Assert.Single(controllerType.GetCustomAttributes(
            typeof(RequireActivatedUnblockedUserAttribute),
            inherit: true));
    }
}
