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
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedText> Biography { get; set; } = new();

    /// <summary>
    /// Statut de revue interne pour le pilotage back-office.
    /// </summary>
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;
}
