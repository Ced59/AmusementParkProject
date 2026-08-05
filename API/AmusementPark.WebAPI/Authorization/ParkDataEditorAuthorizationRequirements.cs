using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Authorization;

public sealed class RestrictedParkDataEditorTokenRequirement : IAuthorizationRequirement
{
    public static RestrictedParkDataEditorTokenRequirement Instance { get; } = new RestrictedParkDataEditorTokenRequirement();

    private RestrictedParkDataEditorTokenRequirement()
    {
    }
}

public sealed class AdminOrParkDataEditorTokenRequirement : IAuthorizationRequirement
{
    public static AdminOrParkDataEditorTokenRequirement Instance { get; } = new AdminOrParkDataEditorTokenRequirement();

    private AdminOrParkDataEditorTokenRequirement()
    {
    }
}

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
