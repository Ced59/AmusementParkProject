using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Contracts.ParkZones;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de mapping HTTP pour les features Parks et ParkZones migrées en phase 7.
/// </summary>
internal static class ParksHttpMappers
{
    public static Park ToDomain(this ParkCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        Park park = new Park
        {
            Name = dto.Name?.Trim(),
            CountryCode = NormalizeOptionalString(dto.CountryCode),
            Type = dto.Type.ToDomain(),
            FounderId = NormalizeOptionalString(dto.FounderId),
            OperatorId = NormalizeOptionalString(dto.OperatorId),
            Descriptions = dto.Descriptions.ToDomain(),
            IsVisible = dto.IsVisible,
            IsFeaturedOnHome = dto.IsFeaturedOnHome,
            FeaturedHomeOrder = NormalizeOptionalOrder(dto.FeaturedHomeOrder),
            IsFeaturedOnHomeSponsored = dto.IsFeaturedOnHomeSponsored && dto.IsFeaturedOnHome,
            WebsiteUrl = NormalizeOptionalString(dto.WebsiteUrl),
            Street = NormalizeOptionalString(dto.Street),
            City = NormalizeOptionalString(dto.City),
            PostalCode = NormalizeOptionalString(dto.PostalCode),
        };

        park.SetPosition(dto.Latitude, dto.Longitude);
        return park;
    }

    public static Park ToDomain(this ParkUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        Park park = new Park
        {
            Name = dto.Name.Trim(),
            CountryCode = NormalizeOptionalString(dto.CountryCode),
            Type = dto.Type.ToDomain(),
            FounderId = NormalizeOptionalString(dto.FounderId),
            OperatorId = NormalizeOptionalString(dto.OperatorId),
            Descriptions = dto.Descriptions.ToDomain(),
            IsVisible = dto.IsVisible,
            IsFeaturedOnHome = dto.IsFeaturedOnHome,
            FeaturedHomeOrder = NormalizeOptionalOrder(dto.FeaturedHomeOrder),
            IsFeaturedOnHomeSponsored = dto.IsFeaturedOnHomeSponsored && dto.IsFeaturedOnHome,
            WebsiteUrl = NormalizeOptionalString(dto.WebsiteUrl),
            Street = NormalizeOptionalString(dto.Street),
            City = NormalizeOptionalString(dto.City),
            PostalCode = NormalizeOptionalString(dto.PostalCode),
        };

        park.SetPosition(dto.Latitude, dto.Longitude);
        return park;
    }

    public static ParkCreatedDto ToCreatedHttp(this Park value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkCreatedDto
        {
            Id = value.Id,
            Name = value.Name,
            CountryCode = value.CountryCode,
            Type = value.Type.ToHttp(),
            FounderId = value.FounderId,
            OperatorId = value.OperatorId,
            Latitude = value.Position?.Latitude ?? 0.0,
            Longitude = value.Position?.Longitude ?? 0.0,
            Descriptions = value.Descriptions.ToHttp(),
            IsVisible = value.IsVisible,
            IsFeaturedOnHome = value.IsFeaturedOnHome,
            FeaturedHomeOrder = value.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = value.IsFeaturedOnHomeSponsored,
            WebSiteUrl = value.WebsiteUrl,
            Street = value.Street,
            City = value.City,
            PostalCode = value.PostalCode,
        };
    }

    public static ParkDto ToHttp(this Park value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkDto
        {
            Id = value.Id,
            Name = value.Name,
            CountryCode = value.CountryCode,
            Type = value.Type.ToHttp(),
            FounderId = value.FounderId,
            OperatorId = value.OperatorId,
            Latitude = value.Position?.Latitude ?? 0.0,
            Longitude = value.Position?.Longitude ?? 0.0,
            Descriptions = value.Descriptions.ToHttp(),
            IsVisible = value.IsVisible,
            IsFeaturedOnHome = value.IsFeaturedOnHome,
            FeaturedHomeOrder = value.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = value.IsFeaturedOnHomeSponsored,
            WebSiteUrl = value.WebsiteUrl,
            Street = value.Street,
            City = value.City,
            PostalCode = value.PostalCode,
            CurrentLogoImageId = value.CurrentLogoImageId,
        };
    }

    public static PaginationDto ToHttp(this PagedResult<Park> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PaginationDto
        {
            TotalItems = checked((int)value.TotalItems),
            TotalPages = value.TotalPages,
            CurrentPage = value.Page,
            ItemsPerPage = value.PageSize,
        };
    }

    public static ParkZone ToDomain(this ParkZoneCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        ParkZone zone = new ParkZone
        {
            ParkId = dto.ParkId.Trim(),
            Name = dto.Name?.Trim() ?? string.Empty,
            Names = dto.Names.ToDomain(),
            Descriptions = dto.Descriptions.ToDomain(),
            IsVisible = dto.IsVisible,
            SortOrder = dto.SortOrder,
        };

        ApplyOptionalPosition(zone, dto.Latitude, dto.Longitude);
        return zone;
    }

    public static ParkZone ToDomain(this ParkZoneUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        ParkZone zone = new ParkZone
        {
            ParkId = dto.ParkId.Trim(),
            Name = dto.Name?.Trim() ?? string.Empty,
            Names = dto.Names.ToDomain(),
            Descriptions = dto.Descriptions.ToDomain(),
            IsVisible = dto.IsVisible,
            SortOrder = dto.SortOrder,
        };

        ApplyOptionalPosition(zone, dto.Latitude, dto.Longitude);
        return zone;
    }

    public static ParkZoneDto ToHttp(this ParkZone value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkZoneDto
        {
            Id = value.Id,
            ParkId = value.ParkId,
            Name = ResolveDisplayName(value.Names, value.Name),
            Names = value.Names.ToHttp(),
            Slug = value.Slug,
            Descriptions = value.Descriptions.ToHttp(),
            Latitude = value.Position?.Latitude,
            Longitude = value.Position?.Longitude,
            IsVisible = value.IsVisible,
            SortOrder = value.SortOrder,
        };
    }

    public static ParkExplorerDto ToHttp(this ParkExplorerResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        List<ParkZone> zones = value.Zones
            .OrderBy(static zone => zone.SortOrder)
            .ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<ParkItem> items = value.Items.ToList();

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
            .Where(static item => string.IsNullOrWhiteSpace(item.ZoneId))
            .ToList();

        ParkExplorerBucketDto? unassignedBucket = null;
        if (unassignedItems.Count > 0)
        {
            unassignedBucket = BuildBucket(null, "unassigned", null, null, true, unassignedItems);
        }
        else if (zoneBuckets.Count == 0)
        {
            unassignedBucket = overview;
        }

        return new ParkExplorerDto
        {
            ParkId = value.ParkId,
            HasZones = zoneBuckets.Count > 0,
            Overview = overview,
            Zones = zoneBuckets,
            Unassigned = unassignedBucket,
        };
    }

    private static ParkExplorerBucketDto BuildBucket(string? id, string name, IEnumerable<AmusementPark.Core.Localization.LocalizedText>? names, string? slug, bool isVirtual, IEnumerable<ParkItem> items)
    {
        List<ParkItem> materializedItems = items.ToList();

        return new ParkExplorerBucketDto
        {
            Id = id,
            Name = name,
            Names = names?.ToHttp() ?? new List<LocalizedTextDto>(),
            Slug = slug,
            IsVirtual = isVirtual,
            TotalItems = materializedItems.Count,
            CountsByCategory = materializedItems
                .GroupBy(static item => item.Category.ToString())
                .OrderBy(static group => group.Key, StringComparer.Ordinal)
                .Select(static group => new ParkZoneSummaryCountDto
                {
                    Key = group.Key,
                    Count = group.Count(),
                })
                .ToList(),
            CountsByType = materializedItems
                .GroupBy(static item => item.Type.ToString())
                .OrderBy(static group => group.Key, StringComparer.Ordinal)
                .Select(static group => new ParkZoneSummaryCountDto
                {
                    Key = group.Key,
                    Count = group.Count(),
                })
                .ToList(),
        };
    }

    private static string ResolveDisplayName(IEnumerable<AmusementPark.Core.Localization.LocalizedText>? names, string? fallback)
    {
        string resolved = names.Resolve("en", "en");
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return resolved.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
    }

    private static void ApplyOptionalPosition(ParkZone zone, double? latitude, double? longitude)
    {
        ArgumentNullException.ThrowIfNull(zone);

        if (latitude.HasValue && longitude.HasValue)
        {
            zone.SetPosition(latitude.Value, longitude.Value);
        }
        else
        {
            zone.ClearPosition();
        }
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? NormalizeOptionalOrder(int? value)
    {
        return value.HasValue && value.Value > 0 ? value.Value : null;
    }

    private static ParkType? ToDomain(this ParkTypeDto? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Enum.TryParse(value.Value.ToString(), out ParkType parsed) ? parsed : null;
    }

    private static ParkTypeDto? ToHttp(this ParkType? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Enum.TryParse(value.Value.ToString(), out ParkTypeDto parsed) ? parsed : null;
    }
}
