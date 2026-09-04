namespace AmusementPark.Application.Features.ParkItems.Ports;

/// <summary>
/// Port de lecture légère des noms des éléments de parc.
/// </summary>
public interface IParkItemNameReadRepository
{
    /// <summary>
    /// Retourne les noms des éléments demandés sans hydrater leurs contenus détaillés.
    /// </summary>
    Task<IReadOnlyDictionary<string, string?>> GetNamesByIdsAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);
}
