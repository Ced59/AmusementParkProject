using Entities.Model.Parks;

namespace Repositories.Interfaces
{
    public interface IParkItemsQueryHandler
    {
        Task<IEnumerable<ParkItem>> GetByParkIdAsync(string parkId, bool includeNonVisible = true);
        Task<ParkItem?> GetByIdAsync(string id);
        Task<ParkItem?> CreateAsync(ParkItem item);
        Task<ParkItem?> UpdateAsync(ParkItem item);
        Task<bool> DeleteAsync(string id);
        Task<long> ClearZoneAsync(string zoneId);
    }
}
