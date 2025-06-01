using Dtos.Pagination;
using Dtos.Parks.Creating;
using Dtos.Parks.ParkGet;
using Dtos.Parks.Parks;
using Entities.Model.Parks;
using Entities.Model.Searching;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations;

public class ParksService : IParksService
{
    private readonly IParksQueryHandler _parksQueryHandler;
    private readonly ISearchIndexService _searchIndexService;
    private readonly IMongoDbSettings _mongoDbSettings;

    public ParksService(IParksQueryHandler parksQueryHandler, ISearchIndexService searchIndexService ,IMongoDbSettings mongoDbSettings)
    {
        _parksQueryHandler = parksQueryHandler;
        _searchIndexService = searchIndexService;
        _mongoDbSettings = mongoDbSettings;
    }

    public async Task<OneOf<ParkCreatedDto, ErrorDetail>>? CreateParkAsync(ParkCreateDto parkDto)
    {
        Park park = new()
        {
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Name = parkDto.Name,
            CountryCode = parkDto.CountryCode,
            Latitude = parkDto.Latitude,
            Longitude = parkDto.Longitude
        };

        Park? createdPark = await _parksQueryHandler.CreateParkAsync(park);

        if (createdPark == null)
        {
            return ErrorCreatingPark;
        }

        SearchItem searchItem = _searchIndexService.ConvertParkToSearchItem(createdPark);

        await _searchIndexService.UpsertSearchItemAsync(searchItem, _mongoDbSettings.SearchItemCollectionName);

        ParkCreatedDto parkCreatedDto = new()
        {
            Id = createdPark.Id,
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

        Park? park = await _parksQueryHandler.GetParkByIdAsync(id.Id);
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
        long totalItems = await _parksQueryHandler.GetTotalParksCountAsync();

        PaginationDto paginationInfo = PaginationDto.Create(Convert.ToInt32(totalItems), page, pageSize);

        IEnumerable<Park> parks = await _parksQueryHandler.GetParksPaginatedAsync(page, pageSize);

        List<ParkDto> parkDtos = parks.Select(park => new ParkDto
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
        IEnumerable<Park>? parks = await _parksQueryHandler.GetParksByLocationAsync(latitude, longitude, radius);

        if (parks == null || !parks.Any())
        {
            return NoParkInThisLocation;
        }

        List<ParkDto> parksDtos = parks.Select(park => new ParkDto
        {
            CountryCode = park.CountryCode,
            Latitude = park.Latitude,
            Longitude = park.Longitude,
            Name = park.Name
        }).ToList();

        return parksDtos;
    }

}