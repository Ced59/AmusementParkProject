using Entities.Model.Parks;

namespace Repositories.Interfaces
{
    public interface IParkItemsQueryHandler
    {
        Task<IEnumerable<ParkItem>> GetByParkIdAsync(string parkId, bool includeNonVisible = true);
        Task<(IEnumerable<ParkItem> Items, long TotalCount)> GetPaginatedAsync(
            int page,
            int pageSize,
            string? parkId,
            string? search,
            bool includeNonVisible = true);
        Task<ParkItem?> GetByIdAsync(string id);
        Task<ParkItem?> CreateAsync(ParkItem item);
        Task<ParkItem?> UpdateAsync(ParkItem item);
        Task<bool> DeleteAsync(string id);
        Task<long> ClearZoneAsync(string zoneId);
        Task<Dictionary<string, int>> GetAttractionCountsByManufacturerIdsAsync(IEnumerable<string> manufacturerIds);
    }
}
