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
