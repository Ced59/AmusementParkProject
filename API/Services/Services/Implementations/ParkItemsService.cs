using Common.General.Localization;
using Dtos.Pagination;
using Dtos.ParkItems;
using Dtos.ParkItems.Creating;
using Dtos.ParkItems.ParkItems;
using Dtos.ParkItems.Updating;
using Entities.Model.Errors;
using Entities.Model.Parks;
using Entities.Model.Searching;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Searching;

namespace Services.Implementations
{
    public class ParkItemsService : IParkItemsService
    {
        private readonly IParkItemsQueryHandler itemsQueryHandler;
        private readonly IParksQueryHandler parksQueryHandler;
        private readonly IParkZonesQueryHandler zonesQueryHandler;
        private readonly IAttractionManufacturersQueryHandler attractionManufacturersQueryHandler;
        private readonly ISearchIndexService searchIndexService;
        private readonly IMongoDbSettings mongoDbSettings;

        public ParkItemsService(
            IParkItemsQueryHandler itemsQueryHandler,
            IParksQueryHandler parksQueryHandler,
            IParkZonesQueryHandler zonesQueryHandler,
            IAttractionManufacturersQueryHandler attractionManufacturersQueryHandler,
            ISearchIndexService searchIndexService,
            IMongoDbSettings mongoDbSettings)
        {
            this.itemsQueryHandler = itemsQueryHandler;
            this.parksQueryHandler = parksQueryHandler;
            this.zonesQueryHandler = zonesQueryHandler;
            this.attractionManufacturersQueryHandler = attractionManufacturersQueryHandler;
            this.searchIndexService = searchIndexService;
            this.mongoDbSettings = mongoDbSettings;
        }

        public async Task<OneOf<IEnumerable<ParkItemDto>, ErrorCodes.ErrorDetail>> GetByParkIdAsync(string parkId, bool includeNonVisible = true)
        {
            Park? park = await parksQueryHandler.GetParkByIdAsync(parkId);
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            IEnumerable<ParkItem> items = await itemsQueryHandler.GetByParkIdAsync(parkId, includeNonVisible);
            return items.Select(MapToDto).ToList();
        }

        public async Task<(IEnumerable<ParkItemAdminListDto> Data, PaginationDto Pagination)> GetPaginatedAsync(
            int page,
            int pageSize,
            string? parkId,
            string? search,
            bool includeNonVisible = true)
        {
            (IEnumerable<ParkItem> items, long totalCount) = await itemsQueryHandler.GetPaginatedAsync(
                page,
                pageSize,
                parkId,
                search,
                includeNonVisible);

            List<ParkItem> itemsList = items.ToList();
            List<string> parkIds = itemsList
                .Where(item => !string.IsNullOrWhiteSpace(item.ParkId))
                .Select(item => item.ParkId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            IEnumerable<Park> parks = await parksQueryHandler.GetParksByIdsAsync(parkIds);
            Dictionary<string, string> parkNamesById = parks
                .Where(park => !string.IsNullOrWhiteSpace(park.Id))
                .ToDictionary(
                    park => park.Id!,
                    park => park.Name ?? string.Empty,
                    StringComparer.Ordinal);

            List<ParkItemAdminListDto> data = itemsList.Select(item => new ParkItemAdminListDto
            {
                Id = item.Id ?? string.Empty,
                ParkId = item.ParkId,
                ParkName = parkNamesById.TryGetValue(item.ParkId, out string? parkName) ? parkName : string.Empty,
                Name = item.Name,
                Category = MapCategory(item.Category),
                Type = MapType(item.Type),
                IsVisible = item.IsVisible
            }).ToList();

            PaginationDto pagination = PaginationDto.Create(Convert.ToInt32(totalCount), page, pageSize);
            return (data, pagination);
        }

        public async Task<OneOf<ParkItemDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id)
        {
            ParkItem? item = await itemsQueryHandler.GetByIdAsync(id);
            return item == null ? ErrorCodes.ParkItemNotExists : MapToDto(item);
        }

        public async Task<OneOf<ParkItemDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkItemCreateDto dto)
        {
            Park? park = await parksQueryHandler.GetParkByIdAsync(dto.ParkId);
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            ErrorCodes.ErrorDetail? validationError = await ValidateReferencesAsync(dto.ParkId, dto.ZoneId, dto.Category, dto.AttractionDetails);
            if (validationError.HasValue)
            {
                return validationError.Value;
            }

            ParkItem item = new()
            {
                ParkId = dto.ParkId.Trim(),
                ZoneId = NormalizeOptionalText(dto.ZoneId),
                Name = dto.Name.Trim(),
                Category = MapCategory(dto.Category),
                Type = MapType(dto.Type),
                Subtype = NormalizeOptionalText(dto.Subtype),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Descriptions = NormalizeLocalizedTextItems(dto.Descriptions),
                AttractionDetails = NormalizeAttractionDetails(dto.Category, dto.AttractionDetails),
                AttractionLocations = NormalizeAttractionLocations(dto.Category, dto.AttractionLocations),
                IsVisible = dto.IsVisible,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            ParkItem? created = await itemsQueryHandler.CreateAsync(item);
            if (created == null)
            {
                return ErrorCodes.ErrorCreatingParkItem;
            }

            SearchItem searchItem = searchIndexService.ConvertParkItemToSearchItem(created, park.Name ?? string.Empty);
            searchItem.IsVisible = park.IsVisible && created.IsVisible;
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);

            return MapToDto(created);
        }

        public async Task<OneOf<ParkItemDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkItemUpdateDto dto)
        {
            ParkItem? existing = await itemsQueryHandler.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorCodes.ParkItemNotExists;
            }

            ErrorCodes.ErrorDetail? validationError = await ValidateReferencesAsync(dto.ParkId, dto.ZoneId, dto.Category, dto.AttractionDetails);
            if (validationError.HasValue)
            {
                return validationError.Value;
            }

            existing.ParkId = dto.ParkId.Trim();
            existing.ZoneId = NormalizeOptionalText(dto.ZoneId);
            existing.Name = dto.Name.Trim();
            existing.Category = MapCategory(dto.Category);
            existing.Type = MapType(dto.Type);
            existing.Subtype = NormalizeOptionalText(dto.Subtype);
            existing.Latitude = dto.Latitude;
            existing.Longitude = dto.Longitude;
            existing.Descriptions = NormalizeLocalizedTextItems(dto.Descriptions);
            existing.AttractionDetails = NormalizeAttractionDetails(dto.Category, dto.AttractionDetails);
            existing.AttractionLocations = NormalizeAttractionLocations(dto.Category, dto.AttractionLocations);
            existing.IsVisible = dto.IsVisible;
            existing.UpdatedAt = DateTime.UtcNow;

            ParkItem? updated = await itemsQueryHandler.UpdateAsync(existing);
            if (updated == null)
            {
                return ErrorCodes.ErrorUpdatingParkItem;
            }

            Park? park = await parksQueryHandler.GetParkByIdAsync(updated.ParkId);
            SearchItem searchItem = searchIndexService.ConvertParkItemToSearchItem(updated, park?.Name ?? string.Empty);
            searchItem.IsVisible = (park?.IsVisible ?? true) && updated.IsVisible;
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);

            return MapToDto(updated);
        }

        public async Task<OneOf<bool, ErrorCodes.ErrorDetail>> DeleteAsync(string id)
        {
            ParkItem? existing = await itemsQueryHandler.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorCodes.ParkItemNotExists;
            }

            bool deleted = await itemsQueryHandler.DeleteAsync(id);
            if (!deleted)
            {
                return ErrorCodes.ErrorDeletingParkItem;
            }

            await searchIndexService.DeleteSearchItemAsync($"parkItem_{id}", mongoDbSettings.SearchItemCollectionName);
            return true;
        }

        private async Task<ErrorCodes.ErrorDetail?> ValidateReferencesAsync(
            string parkId,
            string? zoneId,
            ParkItemCategoryDto category,
            AttractionDetailsDto? attractionDetails)
        {
            if (!string.IsNullOrWhiteSpace(zoneId))
            {
                ParkZone? zone = await zonesQueryHandler.GetByIdAsync(zoneId);
                if (zone == null || zone.ParkId != parkId)
                {
                    return ErrorCodes.ParkZoneNotExists;
                }
            }

            if (category == ParkItemCategoryDto.Attraction)
            {
                string? manufacturerId = NormalizeOptionalText(attractionDetails?.ManufacturerId);
                if (!string.IsNullOrWhiteSpace(manufacturerId))
                {
                    AttractionManufacturer? manufacturer = await attractionManufacturersQueryHandler.GetByIdAsync(manufacturerId);
                    if (manufacturer == null)
                    {
                        return ErrorCodes.AttractionManufacturerNotExists;
                    }
                }
            }

            return null;
        }

        private static ParkItemDto MapToDto(ParkItem item)
        {
            return new ParkItemDto
            {
                Id = item.Id,
                ParkId = item.ParkId,
                ZoneId = item.ZoneId,
                Name = item.Name,
                Category = MapCategory(item.Category),
                Type = MapType(item.Type),
                Subtype = item.Subtype,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                Descriptions = item.Descriptions,
                AttractionDetails = MapAttractionDetails(item.AttractionDetails),
                AttractionLocations = MapAttractionLocations(item.AttractionLocations),
                IsVisible = item.IsVisible
            };
        }

        private static List<LocalizedItem<string>> NormalizeLocalizedTextItems(IEnumerable<LocalizedItem<string>>? items)
        {
            if (items == null)
            {
                return new List<LocalizedItem<string>>();
            }

            return items
                .Where(item => item != null)
                .Where(item => !string.IsNullOrWhiteSpace(item.LanguageCode))
                .Select(item => new LocalizedItem<string>
                {
                    LanguageCode = item.LanguageCode.Trim().ToLowerInvariant(),
                    Value = item.Value?.Trim() ?? string.Empty
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .GroupBy(item => item.LanguageCode)
                .Select(group => group.Last())
                .ToList();
        }

        private static List<LocalizedItem<string>>? NormalizeLocalizedTextList(IEnumerable<LocalizedItem<string>>? items)
        {
            List<LocalizedItem<string>> normalized = NormalizeLocalizedTextItems(items);
            return normalized.Count > 0 ? normalized : null;
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static AttractionDetails? NormalizeAttractionDetails(ParkItemCategoryDto category, AttractionDetailsDto? details)
        {
            if (category != ParkItemCategoryDto.Attraction || details == null)
            {
                return null;
            }

            AttractionDetails normalized = new()
            {
                ManufacturerId = NormalizeOptionalText(details.ManufacturerId),
                Model = NormalizeOptionalText(details.Model),
                ExternalSource = NormalizeOptionalText(details.ExternalSource),
                ExternalId = NormalizeOptionalText(details.ExternalId),
                SourceUrl = NormalizeOptionalText(details.SourceUrl),
                Status = NormalizeOptionalText(details.Status),
                MaterialType = NormalizeOptionalText(details.MaterialType),
                SeatingType = NormalizeOptionalText(details.SeatingType),
                LaunchType = NormalizeOptionalText(details.LaunchType),
                RestraintType = NormalizeOptionalText(details.RestraintType),
                IsLaunched = details.IsLaunched,
                OpeningDate = details.OpeningDate?.Date,
                ClosingDate = details.ClosingDate?.Date,
                OpeningDateText = NormalizeOptionalText(details.OpeningDateText),
                ClosingDateText = NormalizeOptionalText(details.ClosingDateText),
                DurationInSeconds = NormalizeNullableInt(details.DurationInSeconds),
                CapacityPerHour = NormalizeNullableInt(details.CapacityPerHour),
                HeightInFeet = NormalizeNullableDouble(details.HeightInFeet),
                HeightInMeters = NormalizeNullableDouble(details.HeightInMeters),
                LengthInFeet = NormalizeNullableDouble(details.LengthInFeet),
                LengthInMeters = NormalizeNullableDouble(details.LengthInMeters),
                SpeedInMph = NormalizeNullableDouble(details.SpeedInMph),
                SpeedInKmH = NormalizeNullableDouble(details.SpeedInKmH),
                DropInMeters = NormalizeNullableDouble(details.DropInMeters),
                InversionCount = NormalizeNullableInt(details.InversionCount),
                TrainCount = NormalizeNullableInt(details.TrainCount),
                CarsPerTrain = NormalizeNullableInt(details.CarsPerTrain),
                RidersPerVehicle = NormalizeNullableInt(details.RidersPerVehicle),
                HasSingleRider = details.HasSingleRider,
                HasFastPass = details.HasFastPass,
                IsAccessibleForReducedMobility = details.IsAccessibleForReducedMobility,
                IsIndoor = details.IsIndoor,
                WaterExposureLevel = details.WaterExposureLevel.HasValue
                    ? MapWaterExposureLevel(details.WaterExposureLevel.Value)
                    : null,
                AccessConditions = NormalizeAttractionAccessConditions(details.AccessConditions)
            };

            if (!HasAtLeastOneAttractionDetail(normalized))
            {
                return null;
            }

            return normalized;
        }

        private static List<AttractionAccessCondition>? NormalizeAttractionAccessConditions(IEnumerable<AttractionAccessConditionDto>? conditions)
        {
            if (conditions == null)
            {
                return null;
            }

            List<AttractionAccessCondition> normalized = conditions
                .Where(condition => condition != null)
                .Select(NormalizeAttractionAccessCondition)
                .Where(condition => condition != null)
                .Cast<AttractionAccessCondition>()
                .OrderBy(condition => condition.DisplayOrder ?? int.MaxValue)
                .ToList();

            return normalized.Count > 0 ? normalized : null;
        }

        private static AttractionAccessCondition? NormalizeAttractionAccessCondition(AttractionAccessConditionDto dto)
        {
            AttractionAccessConditionType type = MapAccessConditionType(dto.Type);
            List<LocalizedItem<string>>? label = NormalizeLocalizedTextList(dto.Label);
            List<LocalizedItem<string>>? description = NormalizeLocalizedTextList(dto.Description);

            AttractionAccessCondition normalized = new()
            {
                Type = type,
                IsCustom = dto.IsCustom == true || type == AttractionAccessConditionType.Custom ? true : null,
                Value = NormalizeNullableDouble(dto.Value),
                Unit = dto.Unit.HasValue ? MapAccessConditionUnit(dto.Unit.Value) : null,
                RequiresAccompaniment = dto.RequiresAccompaniment,
                MinimumCompanionAge = NormalizeNullableInt(dto.MinimumCompanionAge),
                Label = label,
                Description = description,
                DisplayOrder = NormalizeNullableInt(dto.DisplayOrder)
            };

            if (!HasAtLeastOneAccessConditionValue(normalized))
            {
                return null;
            }

            return normalized;
        }

        private static bool HasAtLeastOneAccessConditionValue(AttractionAccessCondition condition)
        {
            if (condition.Type != AttractionAccessConditionType.Custom)
            {
                return true;
            }

            return condition.Value != null ||
                   condition.Unit != null ||
                   condition.RequiresAccompaniment == true ||
                   condition.MinimumCompanionAge != null ||
                   (condition.Label != null && condition.Label.Count > 0) ||
                   (condition.Description != null && condition.Description.Count > 0);
        }

        private static AttractionLocations? NormalizeAttractionLocations(ParkItemCategoryDto category, AttractionLocationsDto? locations)
        {
            if (category != ParkItemCategoryDto.Attraction || locations == null)
            {
                return null;
            }

            AttractionLocations normalized = new()
            {
                Entrance = NormalizeAttractionLocationPoint(locations.Entrance),
                Exit = NormalizeAttractionLocationPoint(locations.Exit),
                FastPassEntrance = NormalizeAttractionLocationPoint(locations.FastPassEntrance),
                ReducedMobilityEntrance = NormalizeAttractionLocationPoint(locations.ReducedMobilityEntrance)
            };

            if (normalized.Entrance == null &&
                normalized.Exit == null &&
                normalized.FastPassEntrance == null &&
                normalized.ReducedMobilityEntrance == null)
            {
                return null;
            }

            return normalized;
        }

        private static AttractionLocationPoint? NormalizeAttractionLocationPoint(AttractionLocationPointDto? point)
        {
            if (point?.Latitude == null || point.Longitude == null)
            {
                return null;
            }

            if (!IsValidLatitude(point.Latitude.Value) || !IsValidLongitude(point.Longitude.Value))
            {
                return null;
            }

            return new AttractionLocationPoint
            {
                Latitude = point.Latitude.Value,
                Longitude = point.Longitude.Value
            };
        }

        private static AttractionDetailsDto? MapAttractionDetails(AttractionDetails? details)
        {
            if (details == null)
            {
                return null;
            }

            return new AttractionDetailsDto
            {
                ManufacturerId = details.ManufacturerId,
                Model = details.Model,
                ExternalSource = details.ExternalSource,
                ExternalId = details.ExternalId,
                SourceUrl = details.SourceUrl,
                Status = details.Status,
                MaterialType = details.MaterialType,
                SeatingType = details.SeatingType,
                LaunchType = details.LaunchType,
                RestraintType = details.RestraintType,
                IsLaunched = details.IsLaunched,
                OpeningDate = details.OpeningDate,
                ClosingDate = details.ClosingDate,
                OpeningDateText = details.OpeningDateText,
                ClosingDateText = details.ClosingDateText,
                DurationInSeconds = details.DurationInSeconds,
                CapacityPerHour = details.CapacityPerHour,
                HeightInFeet = details.HeightInFeet,
                HeightInMeters = details.HeightInMeters,
                LengthInFeet = details.LengthInFeet,
                LengthInMeters = details.LengthInMeters,
                SpeedInMph = details.SpeedInMph,
                SpeedInKmH = details.SpeedInKmH,
                DropInMeters = details.DropInMeters,
                InversionCount = details.InversionCount,
                TrainCount = details.TrainCount,
                CarsPerTrain = details.CarsPerTrain,
                RidersPerVehicle = details.RidersPerVehicle,
                HasSingleRider = details.HasSingleRider,
                HasFastPass = details.HasFastPass,
                IsAccessibleForReducedMobility = details.IsAccessibleForReducedMobility,
                IsIndoor = details.IsIndoor,
                WaterExposureLevel = details.WaterExposureLevel.HasValue
                    ? MapWaterExposureLevel(details.WaterExposureLevel.Value)
                    : null,
                AccessConditions = MapAttractionAccessConditions(details.AccessConditions)
            };
        }

        private static List<AttractionAccessConditionDto>? MapAttractionAccessConditions(IEnumerable<AttractionAccessCondition>? conditions)
        {
            if (conditions == null)
            {
                return null;
            }

            List<AttractionAccessConditionDto> mapped = conditions
                .Where(condition => condition != null)
                .Select(MapAttractionAccessCondition)
                .Where(condition => condition != null)
                .Cast<AttractionAccessConditionDto>()
                .ToList();

            return mapped.Count > 0 ? mapped : null;
        }

        private static AttractionAccessConditionDto? MapAttractionAccessCondition(AttractionAccessCondition? condition)
        {
            if (condition == null)
            {
                return null;
            }

            return new AttractionAccessConditionDto
            {
                Type = MapAccessConditionType(condition.Type),
                IsCustom = condition.IsCustom,
                Value = condition.Value,
                Unit = condition.Unit.HasValue ? MapAccessConditionUnit(condition.Unit.Value) : null,
                RequiresAccompaniment = condition.RequiresAccompaniment,
                MinimumCompanionAge = condition.MinimumCompanionAge,
                Label = condition.Label,
                Description = condition.Description,
                DisplayOrder = condition.DisplayOrder
            };
        }

        private static AttractionLocationsDto? MapAttractionLocations(AttractionLocations? locations)
        {
            if (locations == null)
            {
                return null;
            }

            return new AttractionLocationsDto
            {
                Entrance = MapAttractionLocationPoint(locations.Entrance),
                Exit = MapAttractionLocationPoint(locations.Exit),
                FastPassEntrance = MapAttractionLocationPoint(locations.FastPassEntrance),
                ReducedMobilityEntrance = MapAttractionLocationPoint(locations.ReducedMobilityEntrance)
            };
        }

        private static AttractionLocationPointDto? MapAttractionLocationPoint(AttractionLocationPoint? point)
        {
            if (point == null)
            {
                return null;
            }

            return new AttractionLocationPointDto
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude
            };
        }

        private static int? NormalizeNullableInt(int? value)
        {
            return value.HasValue && value.Value >= 0 ? value.Value : null;
        }

        private static double? NormalizeNullableDouble(double? value)
        {
            return value.HasValue && value.Value >= 0 ? value.Value : null;
        }

        private static bool HasAtLeastOneAttractionDetail(AttractionDetails details)
        {
            return !string.IsNullOrWhiteSpace(details.ManufacturerId) ||
                   !string.IsNullOrWhiteSpace(details.Model) ||
                   !string.IsNullOrWhiteSpace(details.ExternalSource) ||
                   !string.IsNullOrWhiteSpace(details.ExternalId) ||
                   !string.IsNullOrWhiteSpace(details.SourceUrl) ||
                   !string.IsNullOrWhiteSpace(details.Status) ||
                   !string.IsNullOrWhiteSpace(details.MaterialType) ||
                   !string.IsNullOrWhiteSpace(details.SeatingType) ||
                   !string.IsNullOrWhiteSpace(details.LaunchType) ||
                   !string.IsNullOrWhiteSpace(details.RestraintType) ||
                   details.IsLaunched == true ||
                   details.OpeningDate != null ||
                   details.ClosingDate != null ||
                   !string.IsNullOrWhiteSpace(details.OpeningDateText) ||
                   !string.IsNullOrWhiteSpace(details.ClosingDateText) ||
                   details.DurationInSeconds != null ||
                   details.CapacityPerHour != null ||
                   details.HeightInFeet != null ||
                   details.HeightInMeters != null ||
                   details.LengthInFeet != null ||
                   details.LengthInMeters != null ||
                   details.SpeedInMph != null ||
                   details.SpeedInKmH != null ||
                   details.DropInMeters != null ||
                   details.InversionCount != null ||
                   details.TrainCount != null ||
                   details.CarsPerTrain != null ||
                   details.RidersPerVehicle != null ||
                   details.HasSingleRider == true ||
                   details.HasFastPass == true ||
                   details.IsAccessibleForReducedMobility == true ||
                   details.IsIndoor == true ||
                   details.WaterExposureLevel != null ||
                   (details.AccessConditions != null && details.AccessConditions.Count > 0);
        }

        private static bool IsValidLatitude(double latitude)
        {
            return latitude >= -90 && latitude <= 90;
        }

        private static bool IsValidLongitude(double longitude)
        {
            return longitude >= -180 && longitude <= 180;
        }

        private static ParkItemCategory MapCategory(ParkItemCategoryDto category)
        {
            return Enum.TryParse(category.ToString(), out ParkItemCategory parsed)
                ? parsed
                : ParkItemCategory.Other;
        }

        private static ParkItemCategoryDto MapCategory(ParkItemCategory category)
        {
            return Enum.TryParse(category.ToString(), out ParkItemCategoryDto parsed)
                ? parsed
                : ParkItemCategoryDto.Other;
        }

        private static ParkItemType MapType(ParkItemTypeDto type)
        {
            return Enum.TryParse(type.ToString(), out ParkItemType parsed)
                ? parsed
                : ParkItemType.Other;
        }

        private static ParkItemTypeDto MapType(ParkItemType type)
        {
            return Enum.TryParse(type.ToString(), out ParkItemTypeDto parsed)
                ? parsed
                : ParkItemTypeDto.Other;
        }

        private static AttractionAccessConditionType MapAccessConditionType(AttractionAccessConditionTypeDto type)
        {
            return Enum.TryParse(type.ToString(), out AttractionAccessConditionType parsed)
                ? parsed
                : AttractionAccessConditionType.Custom;
        }

        private static AttractionAccessConditionTypeDto MapAccessConditionType(AttractionAccessConditionType type)
        {
            return Enum.TryParse(type.ToString(), out AttractionAccessConditionTypeDto parsed)
                ? parsed
                : AttractionAccessConditionTypeDto.Custom;
        }

        private static AttractionAccessConditionUnit MapAccessConditionUnit(AttractionAccessConditionUnitDto unit)
        {
            return Enum.TryParse(unit.ToString(), out AttractionAccessConditionUnit parsed)
                ? parsed
                : AttractionAccessConditionUnit.Centimeter;
        }

        private static AttractionAccessConditionUnitDto MapAccessConditionUnit(AttractionAccessConditionUnit unit)
        {
            return Enum.TryParse(unit.ToString(), out AttractionAccessConditionUnitDto parsed)
                ? parsed
                : AttractionAccessConditionUnitDto.Centimeter;
        }

        private static AttractionWaterExposureLevel MapWaterExposureLevel(AttractionWaterExposureLevelDto waterExposureLevel)
        {
            return Enum.TryParse(waterExposureLevel.ToString(), out AttractionWaterExposureLevel parsed)
                ? parsed
                : AttractionWaterExposureLevel.None;
        }

        private static AttractionWaterExposureLevelDto MapWaterExposureLevel(AttractionWaterExposureLevel waterExposureLevel)
        {
            return Enum.TryParse(waterExposureLevel.ToString(), out AttractionWaterExposureLevelDto parsed)
                ? parsed
                : AttractionWaterExposureLevelDto.None;
        }
    }
}
