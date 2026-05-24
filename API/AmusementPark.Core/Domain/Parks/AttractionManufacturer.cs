using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturer : AuditableEntity
{
    /// <summary>
    /// Nom métier du constructeur.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nom légal ou complet du constructeur, si différent du nom d'affichage.
    /// </summary>
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
    public ParkReferenceContactDetails? ContactDetails { get; set; }

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedText> Biography { get; set; } = new();

    /// <summary>
    /// Statut de revue interne pour le pilotage back-office.
    /// </summary>
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;
}
