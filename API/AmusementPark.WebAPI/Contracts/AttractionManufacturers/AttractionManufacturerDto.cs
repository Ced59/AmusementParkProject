using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.AttractionManufacturers;

/// <summary>
/// Contrat HTTP retourné pour un constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturerDto
{
    /// <summary>
    /// Identifiant du constructeur.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Nom du constructeur.
    /// </summary>
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
    /// Identifiant de l'image courante utilisee comme logo.
    /// </summary>
    public string? CurrentLogoImageId { get; set; }

    /// <summary>
    /// Identifiant de l'image principale publique a utiliser sur les cartes.
    /// </summary>
    public string? MainImageId { get; set; }

    /// <summary>
    /// Indique si le constructeur est visible publiquement.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Statut de revue interne back-office.
    /// </summary>
    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;

    /// <summary>
    /// Nombre d'attractions rattachées.
    /// </summary>
    public int AttractionCount { get; set; }
}
