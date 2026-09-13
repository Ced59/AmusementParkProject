using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de retrait de rôle.
/// </summary>
public sealed class RoleRemoveDto
{
    public UserRoleDto Role { get; set; }
}
