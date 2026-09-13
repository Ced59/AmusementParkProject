using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après assignation de rôle.
/// </summary>
public sealed class RoleAssignedDto
{
    public string UserId { get; set; } = string.Empty;

    public IEnumerable<UserRoleDto> Roles { get; set; } = Array.Empty<UserRoleDto>();
}
