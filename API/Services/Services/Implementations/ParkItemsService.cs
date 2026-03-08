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
                ZoneId = NormalizeOptionalId(dto.ZoneId),
                Name = dto.Name.Trim(),
                Category = MapCategory(dto.Category),
                Type = MapType(dto.Type),
                Subtype = NormalizeOptionalId(dto.Subtype),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Descriptions = NormalizeLocalizedTextItems(dto.Descriptions),
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
            existing.ZoneId = NormalizeOptionalId(dto.ZoneId);
            existing.Name = dto.Name.Trim();
            existing.Category = MapCategory(dto.Category);
            existing.Type = MapType(dto.Type);
            existing.Subtype = NormalizeOptionalId(dto.Subtype);
            existing.Latitude = dto.Latitude;
            existing.Longitude = dto.Longitude;
            existing.Descriptions = NormalizeLocalizedTextItems(dto.Descriptions);
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

        private static string? NormalizeOptionalId(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
