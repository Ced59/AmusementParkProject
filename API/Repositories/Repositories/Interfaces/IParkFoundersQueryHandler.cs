using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Parks;

namespace Repositories.Interfaces;

public interface IParkFoundersQueryHandler
{
    Task<IEnumerable<ParkFounder>> GetAllAsync();
    Task<ParkFounder?> GetByIdAsync(string id);
    Task<ParkFounder?> CreateAsync(ParkFounder founder);
    Task<ParkFounder?> UpdateAsync(ParkFounder founder);
}