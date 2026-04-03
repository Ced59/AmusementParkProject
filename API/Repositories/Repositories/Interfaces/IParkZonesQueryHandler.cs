using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Parks;

namespace Repositories.Interfaces
{
    public interface IParkZonesQueryHandler
    {
        Task<IEnumerable<ParkZone>> GetByParkIdAsync(string parkId);
        Task<ParkZone?> GetByIdAsync(string id);
        Task<ParkZone?> CreateAsync(ParkZone zone);
        Task<ParkZone?> UpdateAsync(ParkZone zone);
        Task<bool> DeleteAsync(string id);
    }
}
