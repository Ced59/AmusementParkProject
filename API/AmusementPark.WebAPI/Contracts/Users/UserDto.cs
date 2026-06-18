using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP de listing utilisateur.
/// </summary>
public sealed class UserDto
{
    public string Id { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public bool IsActivated { get; set; }

    public bool IsBlocked { get; set; }

    public string? PreferredLanguage { get; set; }

    public string? PreferredMeasurementSystem { get; set; }

    public List<UserRoleDto> Roles { get; set; } = new();


    public string? AvatarUrl { get; set; }
}
