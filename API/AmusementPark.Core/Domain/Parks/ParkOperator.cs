using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Exploitant de parc.
/// </summary>
public sealed class ParkOperator : AuditableEntity
{
    /// <summary>
    /// Nom de l'exploitant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nom légal ou complet de l'exploitant, si différent du nom d'affichage.
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
    /// Description localisée.
    /// </summary>
    public List<LocalizedText> Description { get; set; } = new();

    /// <summary>
    /// Statut de revue interne pour le pilotage back-office.
    /// </summary>
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;
}
