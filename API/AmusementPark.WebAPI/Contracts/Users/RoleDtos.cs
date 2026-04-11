using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP d'assignation de rôle.
/// </summary>
public sealed class RoleAssignDto
{
    public UserRoleDto Role { get; set; }
}

/// <summary>
/// Contrat HTTP retourné après assignation de rôle.
/// </summary>
public sealed class RoleAssignedDto
{
    public string UserId { get; set; } = string.Empty;

    public IEnumerable<UserRoleDto> Roles { get; set; } = Array.Empty<UserRoleDto>();
}

/// <summary>
/// Contrat HTTP de retrait de rôle.
/// </summary>
public sealed class RoleRemoveDto
{
    public UserRoleDto Role { get; set; }
}

/// <summary>
/// Contrat HTTP retourné après retrait de rôle.
/// </summary>
public sealed class RoleRemovedDto
{
    public string UserId { get; set; } = string.Empty;

    public IEnumerable<UserRoleDto> Roles { get; set; } = Array.Empty<UserRoleDto>();
}
