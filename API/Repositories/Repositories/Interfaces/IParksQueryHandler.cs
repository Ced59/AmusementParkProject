using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Parks;

namespace Repositories.Interfaces
{
    public interface IParksQueryHandler
    {
        Task<Park?> GetParkByIdAsync(string id);
        Task<IEnumerable<Park>> GetParksPaginatedAsync(
            int page,
            int pageSize,
            bool includeNonVisible = false);
        Task<IEnumerable<Park>> GetParksByIdsAsync(IEnumerable<string> ids);
        Task<long> GetTotalParksCountAsync(
            bool includeNonVisible = false);
        Task<Park?> CreateParkAsync(Park park);
        Task<IEnumerable<Park>> GetParksByLocationAsync(double latitude, double longitude, double radius);
        Task<long> GetTotalParksCountByNameAsync(
            string name,
            bool includeNonVisible = false);
        Task<IEnumerable<Park>> GetParksByNamePaginatedAsync(
            string name,
            int page,
            int pageSize,
            bool includeNonVisible = false);
        Task<Park?> UpdateParkVisibilityAsync(string id, bool isVisible);
        Task<Park?> UpdateParkAsync(Park park);
        Task<bool> UpdateCurrentLogoAsync(string parkId, string? logoImageId);
    }
}
