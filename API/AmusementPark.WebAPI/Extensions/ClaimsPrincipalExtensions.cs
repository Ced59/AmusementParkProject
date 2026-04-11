using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AmusementPark.WebAPI.Contracts.Users;

namespace AmusementPark.WebAPI.Extensions;

/// <summary>
/// Helpers de lecture des claims utilisateur.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public static bool IsInRoles(this ClaimsPrincipal user, params UserRoleDto[] roles)
    {
        ArgumentNullException.ThrowIfNull(user);

        HashSet<UserRoleDto> userRoles = user
            .FindAll(ClaimTypes.Role)
            .Select(static claim => Enum.TryParse<UserRoleDto>(claim.Value, true, out UserRoleDto parsedRole) ? parsedRole : (UserRoleDto?)null)
            .Where(static role => role.HasValue)
            .Select(static role => role!.Value)
            .ToHashSet();

        return roles.Any(userRoles.Contains);
    }
}
