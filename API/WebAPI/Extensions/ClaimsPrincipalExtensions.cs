using System.Security.Claims;
using Common.Users;

namespace WebAPI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsInRoles(this ClaimsPrincipal user, params Role[] roles)
    {
        List<Role> userRoles = user.FindAll(ClaimTypes.Role)
            .Select(r => Enum.Parse<Role>(r.Value, true))
            .ToList();
        return userRoles.Any(roles.Contains);
    }

        public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
    }
}