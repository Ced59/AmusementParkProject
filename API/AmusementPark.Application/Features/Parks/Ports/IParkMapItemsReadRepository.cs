using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Ports;

/// <summary>
/// Port de lecture spécialisé pour la carte publique détaillée des éléments d'un parc.
/// </summary>
public interface IParkMapItemsReadRepository
{
    Task<ParkMapItemsResult?> GetAsync(string parkId, bool includeHidden, CancellationToken cancellationToken);
}
