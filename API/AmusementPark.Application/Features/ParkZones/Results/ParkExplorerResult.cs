using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Results;

/// <summary>
/// Résultat applicatif de l'explorateur d'un parc.
/// </summary>
public sealed class ParkExplorerResult
{
    /// <summary>
    /// Identifiant du parc exploré.
    /// </summary>
    public string ParkId { get; init; } = string.Empty;

    /// <summary>
    /// Zones du parc.
    /// </summary>
    public IReadOnlyCollection<ParkZone> Zones { get; init; } = Array.Empty<ParkZone>();

    /// <summary>
    /// Éléments du parc visibles dans l'explorateur.
    /// </summary>
    public IReadOnlyCollection<ParkItem> Items { get; init; } = Array.Empty<ParkItem>();
}
