using System.Collections.Generic;
using System.Threading.Tasks;
using Dtos.AttractionManufacturers.AttractionManufacturers;
using Dtos.AttractionManufacturers.Creating;
using Dtos.AttractionManufacturers.Updating;
using Entities.Model.Errors;
using OneOf;

namespace Services.Interfaces
{
    public interface IAttractionManufacturersService
    {
        Task<OneOf<IEnumerable<AttractionManufacturerDto>, ErrorCodes.ErrorDetail>> GetAllAsync();
        Task<OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id);
        Task<OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail>> CreateAsync(AttractionManufacturerCreateDto dto);
        Task<OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, AttractionManufacturerUpdateDto dto);
    }
}
