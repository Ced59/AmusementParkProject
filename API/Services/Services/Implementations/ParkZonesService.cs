using Common.General.Localization;
using Dtos.ParkItems;
using Dtos.ParkZones;
using Dtos.ParkZones.Creating;
using Dtos.ParkZones.ParkZones;
using Dtos.ParkZones.Updating;
using Entities.Model.Errors;
using Entities.Model.Parks;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Text.RegularExpressions;

namespace Services.Implementations
{
    public class ParkZonesService : IParkZonesService
    {
        private readonly IParkZonesQueryHandler zonesQueryHandler;
        private readonly IParkItemsQueryHandler itemsQueryHandler;
        private readonly IParksQueryHandler parksQueryHandler;

        public ParkZonesService(
            IParkZonesQueryHandler zonesQueryHandler,
            IParkItemsQueryHandler itemsQueryHandler,
            IParksQueryHandler parksQueryHandler)
        {
            this.zonesQueryHandler = zonesQueryHandler;
            this.itemsQueryHandler = itemsQueryHandler;
            this.parksQueryHandler = parksQueryHandler;
        }

        public async Task<OneOf<IEnumerable<ParkZoneDto>, ErrorCodes.ErrorDetail>> GetByParkIdAsync(string parkId)
        {
            if (string.IsNullOrWhiteSpace(parkId))
            {
                return ErrorCodes.ParkNotExists;
            }

            Park? park = await parksQueryHandler.GetParkByIdAsync(parkId);
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            return (await zonesQueryHandler.GetByParkIdAsync(parkId)).Select(MapToDto).ToList();
        }

        public async Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            ParkZone? zone = await zonesQueryHandler.GetByIdAsync(id);
            return zone == null ? ErrorCodes.ParkZoneNotExists : MapToDto(zone);
        }

        public async Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkZoneCreateDto dto)
        {
            Park? park = await parksQueryHandler.GetParkByIdAsync(dto.ParkId);
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            ParkZone zone = new()
            {
                ParkId = dto.ParkId.Trim(),
                Name = dto.Name.Trim(),
                Slug = BuildSlug(dto.Name),
                Descriptions = NormalizeLocalizedTextItems(dto.Descriptions),
                IsVisible = dto.IsVisible,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            ParkZone? created = await zonesQueryHandler.CreateAsync(zone);
            return created == null ? ErrorCodes.ErrorCreatingParkZone : MapToDto(created);
        }

        public async Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkZoneUpdateDto dto)
        {
            ParkZone? existing = await zonesQueryHandler.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            existing.ParkId = dto.ParkId.Trim();
            existing.Name = dto.Name.Trim();
            existing.Slug = BuildSlug(dto.Name);
            existing.Descriptions = NormalizeLocalizedTextItems(dto.Descriptions);
            existing.IsVisible = dto.IsVisible;
            existing.SortOrder = dto.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            ParkZone? updated = await zonesQueryHandler.UpdateAsync(existing);
            return updated == null ? ErrorCodes.ErrorUpdatingParkZone : MapToDto(updated);
        }

        public async Task<OneOf<bool, ErrorCodes.ErrorDetail>> DeleteAsync(string id)
        {
            ParkZone? existing = await zonesQueryHandler.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            await itemsQueryHandler.ClearZoneAsync(id);
            bool deleted = await zonesQueryHandler.DeleteAsync(id);
            return deleted ? true : ErrorCodes.ErrorDeletingParkZone;
        }

        public async Task<OneOf<ParkExplorerDto, ErrorCodes.ErrorDetail>> GetExplorerAsync(string parkId, bool includeNonVisible = false)
        {
            Park? park = await parksQueryHandler.GetParkByIdAsync(parkId);
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            List<ParkZone> zones = (await zonesQueryHandler.GetByParkIdAsync(parkId))
                .Where(zone => includeNonVisible || zone.IsVisible)
                .ToList();

            List<ParkItem> items = (await itemsQueryHandler.GetByParkIdAsync(parkId, includeNonVisible)).ToList();

            ParkExplorerBucketDto overview = BuildBucket(
                id: null,
                name: "overview",
                slug: null,
                isVirtual: true,
                items: items);

            List<ParkExplorerBucketDto> zoneBuckets = zones
                .Select(zone => BuildBucket(
                    zone.Id,
                    zone.Name,
                    zone.Slug,
                    false,
                    items.Where(item => item.ZoneId == zone.Id)))
                .OrderBy(bucket => bucket.Name)
                .ToList();

            List<ParkItem> unassignedItems = items.Where(item => string.IsNullOrWhiteSpace(item.ZoneId)).ToList();
            ParkExplorerBucketDto? unassigned = unassignedItems.Count == 0 && zoneBuckets.Count == 0
                ? null
                : BuildBucket(
                    id: null,
                    name: "unassigned",
                    slug: null,
                    isVirtual: true,
                    items: unassignedItems);

            return new ParkExplorerDto
            {
                ParkId = parkId,
                HasZones = zoneBuckets.Count > 0,
                Overview = overview,
                Zones = zoneBuckets,
                Unassigned = unassignedItems.Count > 0 ? unassigned : (zoneBuckets.Count == 0 ? overview : null)
            };
        }

        private static ParkExplorerBucketDto BuildBucket(
            string? id,
            string name,
            string? slug,
            bool isVirtual,
            IEnumerable<ParkItem> items)
        {
            List<ParkItem> list = items.ToList();

            return new ParkExplorerBucketDto
            {
                Id = id,
                Name = name,
                Slug = slug,
                IsVirtual = isVirtual,
                TotalItems = list.Count,
                CountsByCategory = list
                    .GroupBy(item => item.Category.ToString())
                    .OrderBy(group => group.Key)
                    .Select(group => new ParkZoneSummaryCountDto { Key = group.Key, Count = group.Count() })
                    .ToList(),
                CountsByType = list
                    .GroupBy(item => item.Type.ToString())
                    .OrderBy(group => group.Key)
                    .Select(group => new ParkZoneSummaryCountDto { Key = group.Key, Count = group.Count() })
                    .ToList()
            };
        }

        private static ParkZoneDto MapToDto(ParkZone zone)
        {
            return new ParkZoneDto
            {
                Id = zone.Id,
                ParkId = zone.ParkId,
                Name = zone.Name,
                Slug = zone.Slug,
                Descriptions = zone.Descriptions,
                IsVisible = zone.IsVisible,
                SortOrder = zone.SortOrder
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

        private static string BuildSlug(string value)
        {
            string normalized = value.Trim().ToLowerInvariant();
            normalized = Regex.Replace(normalized, "[^a-z0-9]+", "-");
            normalized = Regex.Replace(normalized, "-+", "-").Trim('-');
            return normalized;
        }
    }
}
