using Common.General.Localization;
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
    public sealed class ParkZonesService : IParkZonesService
    {
        private readonly IParkZonesQueryHandler parkZonesQueryHandler;
        private readonly IParkItemsQueryHandler parkItemsQueryHandler;
        private readonly IParksQueryHandler parksQueryHandler;

        public ParkZonesService(
            IParkZonesQueryHandler parkZonesQueryHandler,
            IParkItemsQueryHandler parkItemsQueryHandler,
            IParksQueryHandler parksQueryHandler)
        {
            this.parkZonesQueryHandler = parkZonesQueryHandler;
            this.parkItemsQueryHandler = parkItemsQueryHandler;
            this.parksQueryHandler = parksQueryHandler;
        }

        public async Task<OneOf<IEnumerable<ParkZoneDto>, ErrorCodes.ErrorDetail>> GetByParkIdAsync(string parkId)
        {
            if (string.IsNullOrWhiteSpace(parkId))
            {
                return ErrorCodes.ParkNotExists;
            }

            Park? park = await parksQueryHandler.GetParkByIdAsync(parkId.Trim());
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            IEnumerable<ParkZone> zones = await parkZonesQueryHandler.GetByParkIdAsync(parkId.Trim());

            List<ParkZoneDto> result = zones
                .OrderBy(zone => zone.SortOrder)
                .ThenBy(zone => zone.Name)
                .Select(MapToDto)
                .ToList();

            return result;
        }

        public async Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            ParkZone? zone = await parkZonesQueryHandler.GetByIdAsync(id.Trim());
            if (zone == null)
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            return MapToDto(zone);
        }

        public async Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkZoneCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ParkId))
            {
                return ErrorCodes.ParkNotExists;
            }

            Park? park = await parksQueryHandler.GetParkByIdAsync(dto.ParkId.Trim());
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            List<LocalizedItem<string>> normalizedNames = NormalizeLocalizedTextItems(dto.Names);
            string displayName = ResolveDisplayName(normalizedNames, dto.Name);

            ParkZone zone = new()
            {
                ParkId = dto.ParkId.Trim(),
                Name = displayName,
                Names = normalizedNames,
                Slug = BuildSlug(displayName),
                Descriptions = NormalizeLocalizedTextItems(dto.Descriptions),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                SortOrder = dto.SortOrder,
                IsVisible = dto.IsVisible,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            ParkZone? created = await parkZonesQueryHandler.CreateAsync(zone);
            if (created == null)
            {
                return ErrorCodes.ErrorCreatingParkZone;
            }

            return MapToDto(created);
        }

        public async Task<OneOf<ParkZoneDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkZoneUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            ParkZone? existing = await parkZonesQueryHandler.GetByIdAsync(id.Trim());
            if (existing == null)
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            List<LocalizedItem<string>> normalizedNames = NormalizeLocalizedTextItems(dto.Names);
            string displayName = ResolveDisplayName(normalizedNames, dto.Name, existing.Name);

            existing.ParkId = dto.ParkId.Trim();
            existing.Name = displayName;
            existing.Names = normalizedNames;
            existing.Slug = BuildSlug(displayName);
            existing.Descriptions = NormalizeLocalizedTextItems(dto.Descriptions);
            existing.Latitude = dto.Latitude;
            existing.Longitude = dto.Longitude;
            existing.SortOrder = dto.SortOrder;
            existing.IsVisible = dto.IsVisible;
            existing.UpdatedAt = DateTime.UtcNow;

            ParkZone? updated = await parkZonesQueryHandler.UpdateAsync(existing);
            if (updated == null)
            {
                return ErrorCodes.ErrorUpdatingParkZone;
            }

            return MapToDto(updated);
        }

        public async Task<OneOf<bool, ErrorCodes.ErrorDetail>> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            ParkZone? existing = await parkZonesQueryHandler.GetByIdAsync(id.Trim());
            if (existing == null)
            {
                return ErrorCodes.ParkZoneNotExists;
            }

            await parkItemsQueryHandler.ClearZoneAsync(id.Trim());

            bool deleted = await parkZonesQueryHandler.DeleteAsync(id.Trim());
            if (!deleted)
            {
                return ErrorCodes.ErrorDeletingParkZone;
            }

            return true;
        }

        public async Task<OneOf<ParkExplorerDto, ErrorCodes.ErrorDetail>> GetExplorerAsync(string parkId, bool includeHidden = false)
        {
            if (string.IsNullOrWhiteSpace(parkId))
            {
                return ErrorCodes.ParkNotExists;
            }

            Park? park = await parksQueryHandler.GetParkByIdAsync(parkId.Trim());
            if (park == null)
            {
                return ErrorCodes.ParkNotExists;
            }

            List<ParkZone> zones = (await parkZonesQueryHandler.GetByParkIdAsync(parkId.Trim()))
                .Where(zone => includeHidden || zone.IsVisible)
                .OrderBy(zone => zone.SortOrder)
                .ThenBy(zone => zone.Name)
                .ToList();

            List<ParkItem> items = (await parkItemsQueryHandler.GetByParkIdAsync(parkId.Trim(), includeHidden)).ToList();

            ParkExplorerBucketDto overview = BuildBucket(
                id: null,
                name: "overview",
                names: null,
                slug: null,
                isVirtual: true,
                items: items);

            List<ParkExplorerBucketDto> zoneBuckets = zones
                .Select(zone => BuildBucket(
                    zone.Id,
                    ResolveDisplayName(zone.Names, zone.Name),
                    zone.Names,
                    zone.Slug,
                    false,
                    items.Where(item => item.ZoneId == zone.Id)))
                .ToList();

            List<ParkItem> unassignedItems = items
                .Where(item => string.IsNullOrWhiteSpace(item.ZoneId))
                .ToList();

            ParkExplorerBucketDto? unassignedBucket = null;
            if (unassignedItems.Count > 0)
            {
                unassignedBucket = BuildBucket(
                    id: null,
                    name: "unassigned",
                    names: null,
                    slug: null,
                    isVirtual: true,
                    items: unassignedItems);
            }
            else if (zoneBuckets.Count == 0)
            {
                unassignedBucket = overview;
            }

            return new ParkExplorerDto
            {
                ParkId = parkId.Trim(),
                HasZones = zoneBuckets.Count > 0,
                Overview = overview,
                Zones = zoneBuckets,
                Unassigned = unassignedBucket
            };
        }

        private static ParkExplorerBucketDto BuildBucket(
            string? id,
            string name,
            IEnumerable<LocalizedItem<string>>? names,
            string? slug,
            bool isVirtual,
            IEnumerable<ParkItem> items)
        {
            List<ParkItem> list = items.ToList();

            return new ParkExplorerBucketDto
            {
                Id = id,
                Name = name,
                Names = names?.ToList() ?? new List<LocalizedItem<string>>(),
                Slug = slug,
                IsVirtual = isVirtual,
                TotalItems = list.Count,
                CountsByCategory = list
                    .GroupBy(item => item.Category.ToString())
                    .OrderBy(group => group.Key)
                    .Select(group => new ParkZoneSummaryCountDto
                    {
                        Key = group.Key,
                        Count = group.Count()
                    })
                    .ToList(),
                CountsByType = list
                    .GroupBy(item => item.Type.ToString())
                    .OrderBy(group => group.Key)
                    .Select(group => new ParkZoneSummaryCountDto
                    {
                        Key = group.Key,
                        Count = group.Count()
                    })
                    .ToList()
            };
        }

        private static ParkZoneDto MapToDto(ParkZone zone)
        {
            return new ParkZoneDto
            {
                Id = zone.Id,
                ParkId = zone.ParkId,
                Name = ResolveDisplayName(zone.Names, zone.Name),
                Names = zone.Names,
                Slug = zone.Slug,
                Descriptions = zone.Descriptions,
                Latitude = zone.Latitude,
                Longitude = zone.Longitude,
                SortOrder = zone.SortOrder,
                IsVisible = zone.IsVisible
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

        private static string ResolveDisplayName(IEnumerable<LocalizedItem<string>>? names, params string?[] fallbacks)
        {
            string? resolved = names.Resolve("en");
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved.Trim();
            }

            foreach (string? fallback in fallbacks)
            {
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    return fallback.Trim();
                }
            }

            return "zone";
        }

        private static string BuildSlug(string value)
        {
            string normalized = value.Trim().ToLowerInvariant();
            normalized = Regex.Replace(normalized, "[^a-z0-9]+", "-");
            normalized = Regex.Replace(normalized, "-+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(normalized) ? "zone" : normalized;
        }
    }
}
