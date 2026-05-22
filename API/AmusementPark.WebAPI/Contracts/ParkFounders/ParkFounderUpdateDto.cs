using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkFounders;

/// <summary>
/// Contrat HTTP de mise à jour d'un fondateur de parc.
/// </summary>
public sealed class ParkFounderUpdateDto
{
    /// <summary>
    /// Nom du fondateur.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Métier ou rôle principal.
    /// </summary>
    [MaxLength(240)]
    public string? Occupation { get; set; }

    /// <summary>
    /// Date de naissance, au format libre ou ISO selon la donnée disponible.
    /// </summary>
    [MaxLength(80)]
    public string? BirthDate { get; set; }

    /// <summary>
    /// Date de décès éventuelle, au format libre ou ISO selon la donnée disponible.
    /// </summary>
    [MaxLength(80)]
    public string? DeathDate { get; set; }

    /// <summary>
    /// Lieu de naissance.
    /// </summary>
    [MaxLength(240)]
    public string? BirthPlace { get; set; }

    /// <summary>
    /// Nationalité ou pays de rattachement principal.
    /// </summary>
    [MaxLength(16)]
    public string? NationalityCountryCode { get; set; }

    /// <summary>
    /// Site web de référence.
    /// </summary>
    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedTextDto> Biography { get; set; } = new();
}
