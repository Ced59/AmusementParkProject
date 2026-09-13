using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Authorization;

public sealed class AdminOrParkDataEditorTokenAuthorizationHandler :
    AuthorizationHandler<AdminOrParkDataEditorTokenRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOrParkDataEditorTokenRequirement requirement)
    {
        if (context.User.IsInRole(AuthorizationRoleGroups.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        string? authenticationMethod = context.User.FindFirst(
            ParkDataEditorAuthenticationDefaults.AuthenticationMethodClaim)?.Value;
        if (context.User.IsInRole(AuthorizationRoleGroups.ParkDataEditor)
            && string.Equals(
                authenticationMethod,
                ParkDataEditorAuthenticationDefaults.AuthenticationMethod,
                StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
