using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.AttractionManufacturers;

/// <summary>
/// Contrat HTTP de création d'un constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturerCreateDto
{
    /// <summary>
    /// Nom du constructeur.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nom légal ou complet, si différent du nom d'affichage.
    /// </summary>
    [MaxLength(240)]
    public string? LegalName { get; set; }

    /// <summary>
    /// Année de création.
    /// </summary>
    public int? FoundedYear { get; set; }

    /// <summary>
    /// Année de fin d'activité éventuelle.
    /// </summary>
    public int? ClosedYear { get; set; }

    /// <summary>
    /// Coordonnées facultatives.
    /// </summary>
    public ParkReferenceContactDetailsDto? ContactDetails { get; set; }

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedTextDto> Biography { get; set; } = new();

    /// <summary>
    /// Statut de revue interne back-office.
    /// </summary>
    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;
}
