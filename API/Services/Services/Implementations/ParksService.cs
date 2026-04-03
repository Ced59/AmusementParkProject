using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IParkItemsQueryHandler parkItemsQueryHandler;

        public ParksService(
            IParksQueryHandler parksQueryHandler,
            ISearchIndexService searchIndexService,
            IMongoDbSettings mongoDbSettings,
            IParkItemsQueryHandler parkItemsQueryHandler)
        {
            this.parksQueryHandler = parksQueryHandler;
            this.searchIndexService = searchIndexService;
            this.mongoDbSettings = mongoDbSettings;
            this.parkItemsQueryHandler = parkItemsQueryHandler;
        }

        public async Task<OneOf<ParkCreatedDto, ErrorCodes.ErrorDetail>>? CreateParkAsync(ParkCreateDto parkDto)
        {
            Park park = new()
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Name = parkDto.Name,
                CountryCode = parkDto.CountryCode,
                Type = MapParkType(parkDto.Type),
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
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);

            return new ParkCreatedDto
            {
                Id = createdPark.Id,
                Name = createdPark.Name,
                CountryCode = createdPark.CountryCode,
                Type = MapParkType(createdPark.Type),
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
        }

        public async Task<OneOf<ParkGettedDto, ErrorCodes.ErrorDetail>>? GetParkByIdAsync(ParkGetByIdDto id)
        {
            if (string.IsNullOrWhiteSpace(id.Id))
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
                Type = MapParkType(park.Type),
                FounderId = park.FounderId,
                OperatorId = park.OperatorId,
                Latitude = park.Latitude,
                Longitude = park.Longitude,
                Descriptions = park.Descriptions,
                IsVisible = park.IsVisible,
                WebSiteUrl = park.WebSiteUrl,
                Street = park.Street,
                City = park.City,
                PostalCode = park.PostalCode,
                CurrentLogoImageId = park.CurrentLogoImageId
            };
        }

        public async Task<(IEnumerable<ParkDto>, PaginationDto)>? GetListParkPaginatedAsync(int page, int pageSize, bool includeNonVisible = false)
        {
            long totalItems = await parksQueryHandler.GetTotalParksCountAsync(includeNonVisible);
            PaginationDto paginationInfo = PaginationDto.Create(Convert.ToInt32(totalItems), page, pageSize);
            IEnumerable<Park> parks = await parksQueryHandler.GetParksPaginatedAsync(page, pageSize, includeNonVisible);
            return (parks.Select(MapToDto).ToList(), paginationInfo);
        }

        public async Task<(IEnumerable<ParkDto>, PaginationDto)>? SearchParksByNamePaginatedAsync(string name, int page, int pageSize, bool includeNonVisible = false)
        {
            string trimmed = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return await GetListParkPaginatedAsync(page, pageSize, includeNonVisible);
            }

            long totalItems = await parksQueryHandler.GetTotalParksCountByNameAsync(trimmed, includeNonVisible);
            PaginationDto paginationInfo = PaginationDto.Create(Convert.ToInt32(totalItems), page, pageSize);
            IEnumerable<Park> parks = await parksQueryHandler.GetParksByNamePaginatedAsync(trimmed, page, pageSize, includeNonVisible);
            return (parks.Select(MapToDto).ToList(), paginationInfo);
        }

        public async Task<OneOf<IEnumerable<ParkDto>, ErrorCodes.ErrorDetail>> SearchParksByLocationAsync(double latitude, double longitude, double radius)
        {
            IEnumerable<Park>? parks = await parksQueryHandler.GetParksByLocationAsync(latitude, longitude, radius);
            if (parks == null || !parks.Any())
            {
                return ErrorCodes.NoParkInThisLocation;
            }

            return parks.Select(MapToDto).ToList();
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
            existing.Type = MapParkType(dto.Type);
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
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);
            await RefreshParkItemSearchItemsAsync(updatedPark);
            return MapToDto(updatedPark);
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
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);
            await RefreshParkItemSearchItemsAsync(updatedPark);
            return MapToDto(updatedPark);
        }

        private async Task RefreshParkItemSearchItemsAsync(Park park)
        {
            IEnumerable<ParkItem> parkItems = await parkItemsQueryHandler.GetByParkIdAsync(park.Id, true);

            foreach (ParkItem parkItem in parkItems)
            {
                SearchItem searchItem = searchIndexService.ConvertParkItemToSearchItem(parkItem, park.Name ?? string.Empty);
                searchItem.IsVisible = park.IsVisible && parkItem.IsVisible;
                await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);
            }
        }

        private static ParkDto MapToDto(Park park)
        {
            return new ParkDto
            {
                Id = park.Id,
                Name = park.Name,
                CountryCode = park.CountryCode,
                Type = MapParkType(park.Type),
                FounderId = park.FounderId,
                OperatorId = park.OperatorId,
                Latitude = park.Latitude,
                Longitude = park.Longitude,
                Descriptions = park.Descriptions,
                IsVisible = park.IsVisible,
                WebSiteUrl = park.WebSiteUrl,
                Street = park.Street,
                City = park.City,
                PostalCode = park.PostalCode,
                CurrentLogoImageId = park.CurrentLogoImageId
            };
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
            return string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        }

        private static ParkType? MapParkType(ParkTypeDto? type)
        {
            if (type == null)
            {
                return null;
            }

            return Enum.TryParse(type.ToString(), out ParkType parsed) ? parsed : null;
        }

        private static ParkTypeDto? MapParkType(ParkType? type)
        {
            if (type == null)
            {
                return null;
            }

            return Enum.TryParse(type.ToString(), out ParkTypeDto parsed) ? parsed : null;
        }
    }
}
