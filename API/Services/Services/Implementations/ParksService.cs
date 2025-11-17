using Dtos.Pagination;
using Dtos.Parks.Creating;
using Dtos.Parks.ParkGet;
using Dtos.Parks.Parks;
using Entities.Model.Parks;
using Entities.Model.Searching;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Searching;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations;

public class ParksService : IParksService
{
    private readonly IParksQueryHandler parksQueryHandler;
    private readonly ISearchIndexService searchIndexService;
    private readonly IMongoDbSettings mongoDbSettings;

    public ParksService(IParksQueryHandler parksQueryHandler, ISearchIndexService searchIndexService ,IMongoDbSettings mongoDbSettings)
    {
        this.parksQueryHandler = parksQueryHandler;
        this.searchIndexService = searchIndexService;
        this.mongoDbSettings = mongoDbSettings;
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
            Longitude = parkDto.Longitude,
            IsVisible = parkDto.IsVisible
        };

        Park? createdPark = await parksQueryHandler.CreateParkAsync(park);

        if (createdPark == null)
        {
            return ErrorCreatingPark;
        }

        SearchItem searchItem = searchIndexService.ConvertParkToSearchItem(createdPark);

        await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);

        ParkCreatedDto parkCreatedDto = new()
        {
            Id = createdPark.Id,
            Name = createdPark.Name,
            CountryCode = createdPark.CountryCode,
            Latitude = createdPark.Latitude,
            Longitude = createdPark.Longitude,
            IsVisible = createdPark.IsVisible
        };

        return parkCreatedDto;
    }

    public async Task<OneOf<ParkGettedDto, ErrorDetail>>? GetParkByIdAsync(ParkGetByIdDto id)
    {
        if (id.Id == null)
        {
            return ParkNotExists;
        }

        Park? park = await parksQueryHandler.GetParkByIdAsync(id.Id);
        if (park == null)
        {
            return ParkNotExists;
        }

        return new ParkGettedDto()
        {
            Name = park.Name,
            CountryCode = park.CountryCode,
            Latitude = park.Latitude,
            Longitude = park.Longitude,
            IsVisible = park.IsVisible,
        };
    }


    public async Task<(IEnumerable<ParkDto>, PaginationDto)>? GetListParkPaginatedAsync(int page, int pageSize)
    {
        long totalItems = await parksQueryHandler.GetTotalParksCountAsync();

        PaginationDto paginationInfo = PaginationDto.Create(Convert.ToInt32(totalItems), page, pageSize);

        IEnumerable<Park> parks = await parksQueryHandler.GetParksPaginatedAsync(page, pageSize);

        List<ParkDto> parkDtos = parks.Select(park => new ParkDto
        {
            Id = park.Id,
            Name = park.Name,
            CountryCode = park.CountryCode,
            Latitude = park.Latitude,
            Longitude = park.Longitude,
            IsVisible = park.IsVisible,
        }).ToList();

        return (parkDtos, paginationInfo);
    }

    public async Task<(IEnumerable<ParkDto>, PaginationDto)>? SearchParksByNamePaginatedAsync(string name, int page, int pageSize)
    {
        string trimmed = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            // Fallback au comportement de la liste si pas de critère
            return await GetListParkPaginatedAsync(page, pageSize);
        }

        long totalItems = await parksQueryHandler.GetTotalParksCountByNameAsync(trimmed);

        PaginationDto paginationInfo = PaginationDto.Create(Convert.ToInt32(totalItems), page, pageSize);

        IEnumerable<Park> parks = await parksQueryHandler.GetParksByNamePaginatedAsync(trimmed, page, pageSize);

        List<ParkDto> parkDtos = parks.Select(park => new ParkDto
        {
            Id = park.Id,
            Name = park.Name,
            CountryCode = park.CountryCode,
            Latitude = park.Latitude,
            Longitude = park.Longitude,
            IsVisible = park.IsVisible,
        }).ToList();

        return (parkDtos, paginationInfo);
    }

    public async Task<OneOf<IEnumerable<ParkDto>, ErrorDetail>> SearchParksByLocationAsync(double latitude, double longitude, double radius)
    {
        IEnumerable<Park>? parks = await parksQueryHandler.GetParksByLocationAsync(latitude, longitude, radius);

        if (parks == null || !parks.Any())
        {
            return NoParkInThisLocation;
        }

        List<ParkDto> parksDtos = parks.Select(park => new ParkDto
        {
            CountryCode = park.CountryCode,
            Latitude = park.Latitude,
            Longitude = park.Longitude,
            Name = park.Name,
            IsVisible = park.IsVisible
        }).ToList();

        return parksDtos;
    }

    public async Task<OneOf<ParkDto, ErrorDetail>>? UpdateParkVisibilityAsync(string id, bool isVisible)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ParkNotExists;
        }

        Park? updatedPark = await parksQueryHandler.UpdateParkVisibilityAsync(id, isVisible);

        if (updatedPark == null)
        {
            return ParkNotExists;
        }

        var searchItem = searchIndexService.ConvertParkToSearchItem(updatedPark);
        await searchIndexService.UpsertSearchItemAsync(
            searchItem,
            mongoDbSettings.SearchItemCollectionName);

        ParkDto dto = new()
        {
            Id = updatedPark.Id,
            Name = updatedPark.Name,
            CountryCode = updatedPark.CountryCode,
            Latitude = updatedPark.Latitude,
            Longitude = updatedPark.Longitude,
            IsVisible = updatedPark.IsVisible
        };

        return dto;
    }
}