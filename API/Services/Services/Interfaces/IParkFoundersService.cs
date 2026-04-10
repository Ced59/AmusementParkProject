using OneOf;
using Dtos.ParkFounders.Creating;
using Dtos.ParkFounders.ParkFounders;
using Dtos.ParkFounders.Updating;
using Entities.Model.Errors;

namespace Services.Interfaces;

public interface IParkFoundersService
{
    Task<OneOf<IEnumerable<ParkFounderDto>, ErrorCodes.ErrorDetail>> GetAllAsync();
    Task<OneOf<ParkFounderDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id);
    Task<OneOf<ParkFounderDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkFounderCreateDto dto);
    Task<OneOf<ParkFounderDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkFounderUpdateDto dto);
}