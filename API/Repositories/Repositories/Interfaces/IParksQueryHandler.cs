using Entities.Model.Parks;

namespace Repositories.Interfaces;

public interface IParksQueryHandler
{
    Task<Park?> GetParkByIdAsync(string id);
    Task<IEnumerable<Park>> GetParksPaginatedAsync(int page, int pageSize);
    Task<long> GetTotalParksCountAsync();
    Task<Park?> CreateParkAsync(Park park);
    Task<IEnumerable<Park>> GetParksByLocationAsync(double latitude, double longitude, double radius);
    Task<long> GetTotalParksCountByNameAsync(string name);
    Task<IEnumerable<Park>> GetParksByNamePaginatedAsync(string name, int page, int pageSize);

    /// <summary>
    /// Met à jour la visibilité d’un parc et renvoie la version mise à jour.
    /// </summary>
    Task<Park?> UpdateParkVisibilityAsync(string id, bool isVisible);
}