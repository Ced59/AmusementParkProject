using Entities.Model.Parks;

namespace Repositories.Interfaces;

public interface IParksQueryHandler
{
    Task<Park?> GetParkByIdAsync(string id);
    Task<IEnumerable<Park>> GetParksPaginatedAsync(int page, int pageSize);
    Task<long> GetTotalParksCountAsync();
    Task<Park?> CreateParkAsync(Park park);
    Task<IEnumerable<Park>> GetParksByLocationAsync(double latitude, double longitude, double radius);
}