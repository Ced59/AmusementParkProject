using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Authorization;

public sealed class RestrictedParkDataEditorTokenAuthorizationHandler :
    AuthorizationHandler<RestrictedParkDataEditorTokenRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RestrictedParkDataEditorTokenRequirement requirement)
    {
        string? authenticationMethod = context.User.FindFirst(
            ParkDataEditorAuthenticationDefaults.AuthenticationMethodClaim)?.Value;
        if (!string.Equals(
                authenticationMethod,
                ParkDataEditorAuthenticationDefaults.AuthenticationMethod,
                StringComparison.Ordinal))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        Endpoint? endpoint = context.Resource switch
        {
            HttpContext httpContext => httpContext.GetEndpoint(),
            _ => null,
        };
        if (endpoint?.Metadata.GetMetadata<AllowParkDataEditorTokenAttribute>() is not null)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

}
