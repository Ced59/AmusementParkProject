using System.Collections.Generic;
using System.Threading.Tasks;
using Entities.Model.Parks;

namespace Repositories.Interfaces
{
    public interface IAttractionManufacturersQueryHandler
    {
        Task<IEnumerable<AttractionManufacturer>> GetAllAsync();
        Task<AttractionManufacturer?> GetByIdAsync(string id);
        Task<AttractionManufacturer?> CreateAsync(AttractionManufacturer manufacturer);
        Task<AttractionManufacturer?> UpdateAsync(AttractionManufacturer manufacturer);
    }
}
