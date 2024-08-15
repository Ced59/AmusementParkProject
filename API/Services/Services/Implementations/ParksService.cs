using Dtos.Pagination;
using Dtos.Parks.Creating;
using Dtos.Parks.ParkGet;
using Dtos.Parks.Parks;
using Entities.Model.Errors;
using Entities.Model.Parks;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations;

public class ParksService : IParksService
{
    private readonly IParksQueryHandler _parksQueryHandler;

    public ParksService(IParksQueryHandler parksQueryHandler)
    {
        _parksQueryHandler = parksQueryHandler;
    }

    public Task<OneOf<ParkCreatedDto, ErrorDetail>>? CreateParkAsync(ParkCreateDto park)
    {
        throw new NotImplementedException();
    }

    public async Task<OneOf<ParkGettedDto, ErrorDetail>>? GetParkByIdAsync(ParkGetByIdDto id)
    {
        if (id.Value == null)
        {
            return ParkNotExists;
        }

        var park = await _parksQueryHandler.GetParkByIdAsync(id.Value);
        if (park == null)
        {
            return ParkNotExists;
        }

        return new ParkGettedDto()
        {
            Name = park.Name,
            CountryCode = park.CountryCode,
            Latitude = park.Latitude,
            Longitude = park.Longitude
        };
    }


    public Task<(IEnumerable<ParkDto>, PaginationDto)>? GetListParkPaginatedAsync(int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    public async Task<OneOf<IEnumerable<Park>, ErrorDetail>> SearchParksByLocationAsync(double latitude, double longitude, double radius)
    {

        return await _parksQueryHandler.GetParksByLocationAsync(latitude, longitude, radius);
    }
}