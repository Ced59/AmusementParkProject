using System.Security.Claims;
using Common.Users;
using Entities.Model.Users;

namespace WebAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool IsInRoles(this ClaimsPrincipal user, params Role[] roles)
    {
        var userRoles = user.FindAll(ClaimTypes.Role)
            .Select(r => Enum.Parse<Role>(r.Value, true))
            .ToList();
        return userRoles.Any(roles.Contains);
    }

    public static string? GetUserId(this ClaimsPrincipal user)
    {

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}