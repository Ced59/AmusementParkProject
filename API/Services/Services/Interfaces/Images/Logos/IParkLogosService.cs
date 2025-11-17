using Dtos.Parks.Logos;
using OneOf;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Interfaces.Images.Logos;

public interface IParkLogosService
{
    Task<OneOf<ParkLogoDto, ErrorDetail>> AddLogoAsync(string parkId, ParkLogoCreateDto request);
    Task<OneOf<ParkLogoDto, ErrorDetail>> GetCurrentLogoAsync(string parkId);
    Task<OneOf<IEnumerable<ParkLogoDto>, ErrorDetail>> GetLogosHistoryAsync(string parkId);
    Task<OneOf<ParkLogoDto, ErrorDetail>> SetCurrentLogoAsync(string logoId);
    Task<OneOf<bool, ErrorDetail>> DeleteLogoAsync(string logoId);
}