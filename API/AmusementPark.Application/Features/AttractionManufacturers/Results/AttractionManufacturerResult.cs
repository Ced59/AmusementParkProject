using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.AttractionManufacturers.Results;

/// <summary>
/// Résultat applicatif de lecture/écriture d'un constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturerResult
{
    /// <summary>
    /// Identifiant du constructeur.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Nom du constructeur.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Nom légal ou complet du constructeur, si différent du nom d'affichage.
    /// </summary>
    public string? LegalName { get; init; }

    /// <summary>
    /// Année de création.
    /// </summary>
    public int? FoundedYear { get; init; }

    /// <summary>
    /// Année de fin d'activité éventuelle.
    /// </summary>
    public int? ClosedYear { get; init; }

    /// <summary>
    /// Coordonnées facultatives.
    /// </summary>
    public ParkReferenceContactDetails? ContactDetails { get; init; }

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public IReadOnlyCollection<LocalizedText> Biography { get; init; } = Array.Empty<LocalizedText>();

    /// <summary>
    /// Identifiant de l'image courante utilisee comme logo.
    /// </summary>
    public string? CurrentLogoImageId { get; init; }

    /// <summary>
    /// Identifiant de l'image principale publique a utiliser sur les cartes.
    /// </summary>
    public string? MainImageId { get; init; }

    /// <summary>
    /// Indique si le constructeur est visible publiquement.
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Nombre d'attractions liées.
    /// </summary>
    public int AttractionCount { get; init; }

    /// <summary>
    /// Statut de revue interne back-office.
    /// </summary>
    public AdminReviewStatus AdminReviewStatus { get; init; }
}
