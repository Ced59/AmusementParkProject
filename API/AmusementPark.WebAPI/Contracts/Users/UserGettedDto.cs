using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Contrat HTTP détaillé de lecture d'utilisateur.
/// </summary>
public sealed class UserGettedDto
{
    public string Id { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Email { get; set; } = string.Empty;

    public string? FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; } = string.Empty;

    public bool? IsActivated { get; set; }

    public bool? IsBlocked { get; set; }

    public List<UserRoleDto> Roles { get; set; } = new();

    public string? PreferredLanguage { get; set; } = string.Empty;

    public string? PreferredMeasurementSystem { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }
}
