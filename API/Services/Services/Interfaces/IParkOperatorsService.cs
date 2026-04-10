using Dtos.ParkOperators.Creating;
using Dtos.ParkOperators.ParkOperators;
using Dtos.ParkOperators.Updating;
using Entities.Model.Errors;
using OneOf;

namespace Services.Interfaces;

public interface IParkOperatorsService
{
    Task<OneOf<IEnumerable<ParkOperatorDto>, ErrorCodes.ErrorDetail>> GetAllAsync();
    Task<OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id);
    Task<OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkOperatorCreateDto dto);
    Task<OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkOperatorUpdateDto dto);
}