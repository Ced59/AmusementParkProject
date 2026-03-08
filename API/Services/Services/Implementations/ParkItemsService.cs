using Common.General.Localization;
using Dtos.ParkItems;
using Dtos.ParkItems.Creating;
using Dtos.ParkItems.ParkItems;
using Dtos.ParkItems.Updating;
using Entities.Model.Errors;
using Entities.Model.Parks;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ParkItemsService : IParkItemsService
    {
        private readonly IParkItemsQueryHandler itemsQueryHandler;
        private readonly IParksQueryHandler parksQueryHandler;
        private readonly IParkZonesQueryHandler zonesQueryHandler;

        public ParkItemsService(
            IParkItemsQueryHandler itemsQueryHandler,
            IParksQueryHandler parksQueryHandler,
            IParkZonesQueryHandler zonesQueryHandler)
        {
            this.itemsQueryHandler = itemsQueryHandler;
            this.parksQueryHandler = parksQueryHandler;
            this.zonesQueryHandler = zonesQueryHandler;
        }

        public async Task<OneOf<IEnumerable<ParkItemDto>, ErrorCodes.ErrorDetail>> GetByParkIdAsync(string parkId, bool includeNonVisible = true)
        {
            Park? park = await parksQueryHandler.GetParkByIdAsync(parkId);
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            return (await itemsQueryHandler.GetByParkIdAsync(parkId, includeNonVisible)).Select(MapToDto).ToList();
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

            if (!string.IsNullOrWhiteSpace(dto.ZoneId))
            {
                ParkZone? zone = await zonesQueryHandler.GetByIdAsync(dto.ZoneId);
                if (zone == null || zone.ParkId != dto.ParkId)
                {
                    return ErrorCodes.ParkZoneNotExists;
                }
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
            return created == null ? ErrorCodes.ErrorCreatingParkItem : MapToDto(created);
        }

        public async Task<OneOf<ParkItemDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkItemUpdateDto dto)
        {
            ParkItem? existing = await itemsQueryHandler.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorCodes.ParkItemNotExists;
            }

            if (!string.IsNullOrWhiteSpace(dto.ZoneId))
            {
                ParkZone? zone = await zonesQueryHandler.GetByIdAsync(dto.ZoneId);
                if (zone == null || zone.ParkId != dto.ParkId)
                {
                    return ErrorCodes.ParkZoneNotExists;
                }
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
            return updated == null ? ErrorCodes.ErrorUpdatingParkItem : MapToDto(updated);
        }

        public async Task<OneOf<bool, ErrorCodes.ErrorDetail>> DeleteAsync(string id)
        {
            ParkItem? existing = await itemsQueryHandler.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorCodes.ParkItemNotExists;
            }

            return await itemsQueryHandler.DeleteAsync(id) ? true : ErrorCodes.ErrorDeletingParkItem;
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
                Manufacturer = NormalizeOptionalText(details.Manufacturer),
                Model = NormalizeOptionalText(details.Model),
                OpeningDate = details.OpeningDate?.Date,
                ClosingDate = details.ClosingDate?.Date,
                DurationInSeconds = NormalizeNullableInt(details.DurationInSeconds),
                CapacityPerHour = NormalizeNullableInt(details.CapacityPerHour),
                HeightInMeters = NormalizeNullableDouble(details.HeightInMeters),
                LengthInMeters = NormalizeNullableDouble(details.LengthInMeters),
                SpeedInKmH = NormalizeNullableDouble(details.SpeedInKmH),
                DropInMeters = NormalizeNullableDouble(details.DropInMeters),
                InversionCount = NormalizeNullableInt(details.InversionCount),
                MinimumHeightInCm = NormalizeNullableInt(details.MinimumHeightInCm),
                MaximumHeightInCm = NormalizeNullableInt(details.MaximumHeightInCm),
                MinimumAge = NormalizeNullableInt(details.MinimumAge),
                TrainCount = NormalizeNullableInt(details.TrainCount),
                CarsPerTrain = NormalizeNullableInt(details.CarsPerTrain),
                RidersPerVehicle = NormalizeNullableInt(details.RidersPerVehicle),
                HasSingleRider = details.HasSingleRider,
                HasFastPass = details.HasFastPass,
                IsAccessibleForReducedMobility = details.IsAccessibleForReducedMobility,
                IsIndoor = details.IsIndoor,
                IsWaterAttraction = details.IsWaterAttraction
            };

            if (!HasAtLeastOneAttractionDetail(normalized))
            {
                return null;
            }

            return normalized;
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
                Manufacturer = details.Manufacturer,
                Model = details.Model,
                OpeningDate = details.OpeningDate,
                ClosingDate = details.ClosingDate,
                DurationInSeconds = details.DurationInSeconds,
                CapacityPerHour = details.CapacityPerHour,
                HeightInMeters = details.HeightInMeters,
                LengthInMeters = details.LengthInMeters,
                SpeedInKmH = details.SpeedInKmH,
                DropInMeters = details.DropInMeters,
                InversionCount = details.InversionCount,
                MinimumHeightInCm = details.MinimumHeightInCm,
                MaximumHeightInCm = details.MaximumHeightInCm,
                MinimumAge = details.MinimumAge,
                TrainCount = details.TrainCount,
                CarsPerTrain = details.CarsPerTrain,
                RidersPerVehicle = details.RidersPerVehicle,
                HasSingleRider = details.HasSingleRider,
                HasFastPass = details.HasFastPass,
                IsAccessibleForReducedMobility = details.IsAccessibleForReducedMobility,
                IsIndoor = details.IsIndoor,
                IsWaterAttraction = details.IsWaterAttraction
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
            return !string.IsNullOrWhiteSpace(details.Manufacturer) ||
                   !string.IsNullOrWhiteSpace(details.Model) ||
                   details.OpeningDate != null ||
                   details.ClosingDate != null ||
                   details.DurationInSeconds != null ||
                   details.CapacityPerHour != null ||
                   details.HeightInMeters != null ||
                   details.LengthInMeters != null ||
                   details.SpeedInKmH != null ||
                   details.DropInMeters != null ||
                   details.InversionCount != null ||
                   details.MinimumHeightInCm != null ||
                   details.MaximumHeightInCm != null ||
                   details.MinimumAge != null ||
                   details.TrainCount != null ||
                   details.CarsPerTrain != null ||
                   details.RidersPerVehicle != null ||
                   details.HasSingleRider == true ||
                   details.HasFastPass == true ||
                   details.IsAccessibleForReducedMobility == true ||
                   details.IsIndoor == true ||
                   details.IsWaterAttraction == true;
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
    }
}
