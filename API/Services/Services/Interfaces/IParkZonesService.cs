using System.Collections.Generic;
using System.Threading.Tasks;
using Dtos.ParkZones;
using Dtos.ParkZones.Creating;
using Dtos.ParkZones.ParkZones;
using Dtos.ParkZones.Updating;
using Entities.Model.Errors;
using OneOf;

namespace Services.Interfaces
{
    public interface IParkZonesService
    {
        Task<OneOf<IEnumerable<ParkZoneDto>, ErrorCodes.ErrorDetail>> GetByParkIdAsync(string parkId);
        Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id);
        Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkZoneCreateDto dto);
        Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkZoneUpdateDto dto);
        Task<OneOf<bool, ErrorCodes.ErrorDetail>> DeleteAsync(string id);
        Task<OneOf<ParkExplorerDto, ErrorCodes.ErrorDetail>> GetExplorerAsync(string parkId, bool includeHidden = false);
    }
}
