using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Results;

/// <summary>
/// Résultat applicatif utilisé pour les listes paginées d'administration des park items.
/// </summary>
public sealed class ParkItemAdminListResult
{
    /// <summary>
    /// Identifiant du park item.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Identifiant du parc parent.
    /// </summary>
    public string ParkId { get; init; } = string.Empty;

    /// <summary>
    /// Nom du parc parent.
    /// </summary>
    public string ParkName { get; init; } = string.Empty;

    /// <summary>
    /// Nom du park item.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Catégorie du park item.
    /// </summary>
    public ParkItemCategory Category { get; init; }

    /// <summary>
    /// Type détaillé du park item.
    /// </summary>
    public ParkItemType Type { get; init; }

    /// <summary>
    /// Indique si l'élément est visible.
    /// </summary>
    public bool IsVisible { get; init; }

    /// <summary>
    /// Statut de traitement interne.
    /// </summary>
    public AdminReviewStatus AdminReviewStatus { get; init; }
}
