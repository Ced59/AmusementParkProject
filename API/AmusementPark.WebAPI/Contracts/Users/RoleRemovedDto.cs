using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après retrait de rôle.
/// </summary>
public sealed class RoleRemovedDto
{
    public string UserId { get; set; } = string.Empty;

    public IEnumerable<UserRoleDto> Roles { get; set; } = Array.Empty<UserRoleDto>();
}
