namespace AmusementPark.Application.Features.Parks.Ports;

/// <summary>
/// Port de lecture légère des noms de parcs.
/// </summary>
public interface IParkNameReadRepository
{
    /// <summary>
    /// Retourne les noms des parcs demandés sans hydrater leurs contenus détaillés.
    /// </summary>
    Task<IReadOnlyDictionary<string, string?>> GetNamesByIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken);
}
