using Common.General.Localization;
using Dtos.Pagination;
using Dtos.Parks;
using Dtos.Parks.Creating;
using Dtos.Parks.ParkGet;
using Dtos.Parks.Parks;
using Dtos.Parks.Updating;
using Entities.Model.Errors;
using Entities.Model.Parks;
using Entities.Model.Searching;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Searching;

namespace Services.Implementations
{
    public class ParksService : IParksService
    {
        private readonly IParksQueryHandler parksQueryHandler;
        private readonly ISearchIndexService searchIndexService;
        private readonly IMongoDbSettings mongoDbSettings;

        public ParksService(
            IParksQueryHandler parksQueryHandler,
            ISearchIndexService searchIndexService,
            IMongoDbSettings mongoDbSettings)
        {
            this.parksQueryHandler = parksQueryHandler;
            this.searchIndexService = searchIndexService;
            this.mongoDbSettings = mongoDbSettings;
        }

        public async Task<OneOf<ParkCreatedDto, ErrorCodes.ErrorDetail>>? CreateParkAsync(ParkCreateDto parkDto)
        {
            Park park = new()
            {
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Name = parkDto.Name,
                CountryCode = parkDto.CountryCode,
                Type = MapToEntityParkType(parkDto.Type),
                FounderId = NormalizeOptionalId(parkDto.FounderId),
                OperatorId = NormalizeOptionalId(parkDto.OperatorId),
                Latitude = parkDto.Latitude,
                Longitude = parkDto.Longitude,
                Descriptions = NormalizeDescriptions(parkDto.Descriptions),
                IsVisible = parkDto.IsVisible,
                WebSiteUrl = parkDto.WebsiteUrl,
                Street = parkDto.Street,
                City = parkDto.City,
                PostalCode = parkDto.PostalCode
            };

            Park? createdPark = await parksQueryHandler.CreateParkAsync(park);

            if (createdPark == null)
            {
                return ErrorCodes.ErrorCreatingPark;
            }

            SearchItem searchItem = searchIndexService.ConvertParkToSearchItem(createdPark);

            await searchIndexService.UpsertSearchItemAsync(
                searchItem,
                mongoDbSettings.SearchItemCollectionName);

            ParkCreatedDto parkCreatedDto = new()
            {
                Id = createdPark.Id,
                Name = createdPark.Name,
                CountryCode = createdPark.CountryCode,
                Type = MapToDtoParkType(createdPark.Type),
                FounderId = createdPark.FounderId,
                OperatorId = createdPark.OperatorId,
                Latitude = createdPark.Latitude,
                Longitude = createdPark.Longitude,
                Descriptions = createdPark.Descriptions,
                IsVisible = createdPark.IsVisible,
                WebSiteUrl = createdPark.WebSiteUrl,
                Street = createdPark.Street,
                City = createdPark.City,
                PostalCode = createdPark.PostalCode
            };

            return parkCreatedDto;
        }

        public async Task<OneOf<ParkGettedDto, ErrorCodes.ErrorDetail>>? GetParkByIdAsync(ParkGetByIdDto id)
        {
            if (id.Id == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            Park? park = await parksQueryHandler.GetParkByIdAsync(id.Id);

            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            return new ParkGettedDto
            {
                Id = park.Id,
                Name = park.Name,
                CountryCode = park.CountryCode,
                Type = MapToDtoParkType(park.Type),
                FounderId = park.FounderId,
                OperatorId = park.OperatorId,
                Latitude = park.Latitude,
                Longitude = park.Longitude,
                Descriptions = park.Descriptions,
                IsVisible = park.IsVisible,
                WebSiteUrl = park.WebSiteUrl,
                Street = park.Street,
                City = park.City,
                PostalCode = park.PostalCode
            };
        }

        public async Task<(IEnumerable<ParkDto>, PaginationDto)>? GetListParkPaginatedAsync(
            int page,
            int pageSize,
            bool includeNonVisible = false)
        {
            long totalItems = await parksQueryHandler.GetTotalParksCountAsync(includeNonVisible);

            PaginationDto paginationInfo = PaginationDto.Create(
                Convert.ToInt32(totalItems),
                page,
                pageSize);

            IEnumerable<Park> parks = await parksQueryHandler.GetParksPaginatedAsync(
                page,
                pageSize,
                includeNonVisible);

            List<ParkDto> parkDtos = parks.Select(MapToDto).ToList();

            return (parkDtos, paginationInfo);
        }

        public async Task<(IEnumerable<ParkDto>, PaginationDto)>? SearchParksByNamePaginatedAsync(
            string name,
            int page,
            int pageSize,
            bool includeNonVisible = false)
        {
            string trimmed = name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return await GetListParkPaginatedAsync(page, pageSize, includeNonVisible);
            }

            long totalItems = await parksQueryHandler.GetTotalParksCountByNameAsync(trimmed, includeNonVisible);

            PaginationDto paginationInfo = PaginationDto.Create(
                Convert.ToInt32(totalItems),
                page,
                pageSize);

            IEnumerable<Park> parks = await parksQueryHandler.GetParksByNamePaginatedAsync(
                trimmed,
                page,
                pageSize,
                includeNonVisible);

            List<ParkDto> parkDtos = parks.Select(MapToDto).ToList();

            return (parkDtos, paginationInfo);
        }

        public async Task<OneOf<IEnumerable<ParkDto>, ErrorCodes.ErrorDetail>> SearchParksByLocationAsync(
            double latitude,
            double longitude,
            double radius)
        {
            IEnumerable<Park>? parks = await parksQueryHandler.GetParksByLocationAsync(latitude, longitude, radius);

            if (parks == null || !parks.Any())
            {
                return ErrorCodes.NoParkInThisLocation;
            }

            List<ParkDto> parksDtos = parks.Select(MapToDto).ToList();

            return parksDtos;
        }

        public async Task<OneOf<ParkDto, ErrorCodes.ErrorDetail>> UpdateParkAsync(string id, ParkUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkNotExists;
            }

            Park? existing = await parksQueryHandler.GetParkByIdAsync(id);

            if (existing == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            existing.Name = dto.Name;
            existing.CountryCode = dto.CountryCode;
            existing.Type = MapToEntityParkType(dto.Type);
            existing.FounderId = NormalizeOptionalId(dto.FounderId);
            existing.OperatorId = NormalizeOptionalId(dto.OperatorId);
            existing.Latitude = dto.Latitude;
            existing.Longitude = dto.Longitude;
            existing.Descriptions = NormalizeDescriptions(dto.Descriptions);
            existing.IsVisible = dto.IsVisible;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.WebSiteUrl = dto.WebsiteUrl;
            existing.Street = dto.Street;
            existing.City = dto.City;
            existing.PostalCode = dto.PostalCode;

            Park? updatedPark = await parksQueryHandler.UpdateParkAsync(existing);

            if (updatedPark == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            SearchItem searchItem = searchIndexService.ConvertParkToSearchItem(updatedPark);

            await searchIndexService.UpsertSearchItemAsync(
                searchItem,
                mongoDbSettings.SearchItemCollectionName);

            ParkDto dtoResult = MapToDto(updatedPark);

            return dtoResult;
        }

        public async Task<OneOf<ParkDto, ErrorCodes.ErrorDetail>>? UpdateParkVisibilityAsync(string id, bool isVisible)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkNotExists;
            }

            Park? updatedPark = await parksQueryHandler.UpdateParkVisibilityAsync(id, isVisible);

            if (updatedPark == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            SearchItem searchItem = searchIndexService.ConvertParkToSearchItem(updatedPark);

            await searchIndexService.UpsertSearchItemAsync(
                searchItem,
                mongoDbSettings.SearchItemCollectionName);

            ParkDto dto = MapToDto(updatedPark);

            return dto;
        }

        private static ParkDto MapToDto(Park park)
        {
            return new ParkDto
            {
                Id = park.Id,
                Name = park.Name,
                CountryCode = park.CountryCode,
                Type = MapToDtoParkType(park.Type),
                FounderId = park.FounderId,
                OperatorId = park.OperatorId,
                Latitude = park.Latitude,
                Longitude = park.Longitude,
                Descriptions = park.Descriptions,
                IsVisible = park.IsVisible,
                WebSiteUrl = park.WebSiteUrl,
                Street = park.Street,
                City = park.City,
                PostalCode = park.PostalCode
            };
        }

        private static ParkType? MapToEntityParkType(ParkTypeDto? type)
        {
            if (!type.HasValue)
            {
                return null;
            }

            return Enum.Parse<ParkType>(type.Value.ToString());
        }

        private static ParkTypeDto? MapToDtoParkType(ParkType? type)
        {
            if (!type.HasValue)
            {
                return null;
            }

            return Enum.Parse<ParkTypeDto>(type.Value.ToString());
        }

        private static List<LocalizedItem<string>> NormalizeDescriptions(IEnumerable<LocalizedItem<string>>? descriptions)
        {
            if (descriptions == null)
            {
                return new List<LocalizedItem<string>>();
            }

            return descriptions
                .Where(description => description != null)
                .Where(description => !string.IsNullOrWhiteSpace(description.LanguageCode))
                .Select(description => new LocalizedItem<string>
                {
                    LanguageCode = description.LanguageCode.Trim().ToLowerInvariant(),
                    Value = description.Value?.Trim() ?? string.Empty
                })
                .Where(description => !string.IsNullOrWhiteSpace(description.Value))
                .GroupBy(description => description.LanguageCode)
                .Select(group => group.Last())
                .ToList();
        }

        private static string? NormalizeOptionalId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return id.Trim();
        }
    }
}