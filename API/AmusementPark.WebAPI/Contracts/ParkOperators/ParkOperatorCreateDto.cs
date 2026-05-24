using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOperators;

/// <summary>
/// Contrat HTTP de création d'un exploitant de parc.
/// </summary>
public sealed class ParkOperatorCreateDto
{
    /// <summary>
    /// Nom de l'exploitant.
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
    /// Description localisée.
    /// </summary>
    public List<LocalizedTextDto> Description { get; set; } = new();

    /// <summary>
    /// Statut de revue interne back-office.
    /// </summary>
    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;
}
