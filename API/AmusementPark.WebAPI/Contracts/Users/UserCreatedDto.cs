using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP retourné après inscription.
/// </summary>
public sealed class UserCreatedDto
{
    public string Id { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Email { get; set; }

    public bool? IsActivated { get; set; }

    public bool? IsBlocked { get; set; }

    public List<UserRoleDto> Roles { get; set; } = new();

    public string? PreferredLanguage { get; set; }

    public string? AvatarUrl { get; set; }
}
