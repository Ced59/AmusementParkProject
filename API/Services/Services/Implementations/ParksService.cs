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

    public async Task<OneOf<ParkCreatedDto, ErrorDetail>>? CreateParkAsync(ParkCreateDto parkDto)
    {
        var park = new Park
        {
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Name = parkDto.Name,
            CountryCode = parkDto.CountryCode,
            Latitude = parkDto.Latitude,
            Longitude = parkDto.Longitude
        };

        var createdPark = await _parksQueryHandler.CreateParkAsync(park);

        if (createdPark == null)
        {
            return ErrorCreatingPark;
        }

        var parkCreatedDto = new ParkCreatedDto()
        {
            Name = createdPark.Name,
            CountryCode = createdPark.CountryCode,
            Latitude = createdPark.Latitude,
            Longitude = createdPark.Longitude
        };

        return parkCreatedDto;
    }

    public async Task<OneOf<ParkGettedDto, ErrorDetail>>? GetParkByIdAsync(ParkGetByIdDto id)
    {
        if (id.Id == null)
        {
            return ParkNotExists;
        }

        var park = await _parksQueryHandler.GetParkByIdAsync(id.Id);
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


    public async Task<(IEnumerable<ParkDto>, PaginationDto)>? GetListParkPaginatedAsync(int page, int pageSize)
    {
        var totalItems = await _parksQueryHandler.GetTotalParksCountAsync();

        var paginationInfo = PaginationDto.Create(Convert.ToInt32(totalItems), page, pageSize);

        var parks = await _parksQueryHandler.GetParksPaginatedAsync(page, pageSize);

        var parkDtos = parks.Select(park => new ParkDto
        {
            Id = park.Id,
            Name = park.Name,
            CountryCode = park.CountryCode,
            Latitude = park.Latitude,
            Longitude = park.Longitude
        }).ToList();

        return (parkDtos, paginationInfo);
    }

    public async Task<OneOf<IEnumerable<ParkDto>, ErrorDetail>> SearchParksByLocationAsync(double latitude, double longitude, double radius)
    {
        var parks = await _parksQueryHandler.GetParksByLocationAsync(latitude, longitude, radius);

        if (parks == null || !parks.Any())
        {
            return NoParkInThisLocation;
        }

        var parksDtos = parks.Select(park => new ParkDto
        {
            CountryCode = park.CountryCode,
            Latitude = park.Latitude,
            Longitude = park.Longitude,
            Name = park.Name
        }).ToList();

        return parksDtos;
    }

}