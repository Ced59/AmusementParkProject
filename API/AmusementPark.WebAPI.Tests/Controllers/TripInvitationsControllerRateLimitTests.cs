using System.Reflection;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class TripInvitationsControllerRateLimitTests
{
    [Theory]
    [InlineData(nameof(TripInvitationsController.CreateAsync))]
    [InlineData(nameof(TripInvitationsController.RevokeAsync))]
    public void MutationEndpoints_ShouldShareThePerUserInvitationBudget(string methodName)
    {
        MethodInfo method = Assert.IsAssignableFrom<MethodInfo>(
            typeof(TripInvitationsController).GetMethod(methodName));

        EnableRateLimitingAttribute attribute = Assert.IsType<EnableRateLimitingAttribute>(
            method.GetCustomAttribute<EnableRateLimitingAttribute>());

        Assert.Equal(RateLimitPolicyNames.TripInvitationMutations, attribute.PolicyName);
    }
}
