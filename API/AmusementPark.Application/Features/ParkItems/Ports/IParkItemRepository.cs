using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Ports;

/// <summary>
/// Port applicatif de persistance des park items.
/// </summary>
public interface IParkItemRepository
{
    Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken);
    Task<PagedResult<ParkItem>> GetPageAsync(int page, int pageSize, string? parkId, string? search, bool includeHidden, CancellationToken cancellationToken);
    Task<ParkItem?> GetByIdAsync(string parkItemId, CancellationToken cancellationToken);
    Task<ParkItem> CreateAsync(ParkItem parkItem, CancellationToken cancellationToken);
    Task<ParkItem?> UpdateAsync(string parkItemId, ParkItem parkItem, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string parkItemId, CancellationToken cancellationToken);
}
