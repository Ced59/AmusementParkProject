using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Ports;

/// <summary>
/// Port de lecture spécialisé pour la page publique ParkDetailLight.
/// </summary>
public interface IParkDetailSummaryReadRepository
{
    Task<ParkDetailSummaryResult?> GetAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);
}
