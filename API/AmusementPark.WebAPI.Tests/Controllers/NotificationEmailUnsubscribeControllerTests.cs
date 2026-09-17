using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class NotificationEmailUnsubscribeControllerTests
{
    [Fact]
    public void Unsubscribe_ShouldBeAnonymousPostAndRateLimited()
    {
        Type controllerType = typeof(NotificationEmailUnsubscribeController);
        Assert.Single(controllerType.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
        System.Reflection.MethodInfo action = controllerType.GetMethod("UnsubscribeAsync")!;
        Assert.Single(action.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true));
        EnableRateLimitingAttribute rateLimit = Assert.Single(
            action.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
                .Cast<EnableRateLimitingAttribute>());

        Assert.Equal(RateLimitPolicyNames.AuthEmailChallenge, rateLimit.PolicyName);
    }
}
