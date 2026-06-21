using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Ports;

/// <summary>
/// Port applicatif de persistance des zones de parc.
/// </summary>
public interface IParkZoneRepository
{
    Task<IReadOnlyCollection<ParkZone>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ParkZone>> GetByParkIdAsync(string parkId, CancellationToken cancellationToken);
    Task<ParkZone?> GetByIdAsync(string zoneId, CancellationToken cancellationToken);
    Task<ParkZone> CreateAsync(ParkZone zone, CancellationToken cancellationToken);
    Task<ParkZone?> UpdateAsync(string zoneId, ParkZone zone, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string zoneId, CancellationToken cancellationToken);
    Task<ParkExplorerResult> GetExplorerAsync(string parkId, bool includeHidden, CancellationToken cancellationToken);
    Task<ParkExplorerResult> GetExplorerAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);
}
