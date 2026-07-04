using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Application.Features.Parks.Results;
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
            AudienceClassification = dto.AudienceClassification.ToDomain(),
            Status = dto.Status.ToDomain(),
            OpeningDate = dto.OpeningDate?.Date,
            ClosingDate = dto.ClosingDate?.Date,
            OpeningDateText = NormalizeOptionalString(dto.OpeningDateText),
            ClosingDateText = NormalizeOptionalString(dto.ClosingDateText),
            FounderId = NormalizeOptionalString(dto.FounderId),
            OperatorId = NormalizeOptionalString(dto.OperatorId),
            Descriptions = dto.Descriptions.ToDomain(),
            IsVisible = dto.IsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
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
            AudienceClassification = dto.AudienceClassification.ToDomain(),
            Status = dto.Status.HasValue ? dto.Status.Value.ToDomain() : ParkStatus.Operating,
            OpeningDate = dto.OpeningDate?.Date,
            ClosingDate = dto.ClosingDate?.Date,
            OpeningDateText = NormalizeOptionalString(dto.OpeningDateText),
            ClosingDateText = NormalizeOptionalString(dto.ClosingDateText),
            FounderId = NormalizeOptionalString(dto.FounderId),
            OperatorId = NormalizeOptionalString(dto.OperatorId),
            Descriptions = dto.Descriptions.ToDomain(),
            IsVisible = dto.IsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
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
            AudienceClassification = value.AudienceClassification.ToHttp(),
            Status = value.Status.ToHttp(),
            OpeningDate = value.OpeningDate,
            ClosingDate = value.ClosingDate,
            OpeningDateText = value.OpeningDateText,
            ClosingDateText = value.ClosingDateText,
            FounderId = value.FounderId,
            OperatorId = value.OperatorId,
            Latitude = value.Position?.Latitude ?? 0.0,
            Longitude = value.Position?.Longitude ?? 0.0,
            Descriptions = value.Descriptions.ToHttp(),
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
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
            AudienceClassification = value.AudienceClassification.ToHttp(),
            Status = value.Status.ToHttp(),
            OpeningDate = value.OpeningDate,
            ClosingDate = value.ClosingDate,
            OpeningDateText = value.OpeningDateText,
            ClosingDateText = value.ClosingDateText,
            FounderId = value.FounderId,
            OperatorId = value.OperatorId,
            Latitude = value.Position?.Latitude ?? 0.0,
            Longitude = value.Position?.Longitude ?? 0.0,
            Descriptions = value.Descriptions.ToHttp(),
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
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

    public static ParkDto ToHttp(this ParkListResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        ParkDto dto = value.Park.ToHttp();
        dto.ParkItemsTotalCount = value.ParkItemsTotalCount;
        dto.ParkItemsVisibleCount = value.ParkItemsVisibleCount;
        dto.OpeningHours = value.OpeningHours?.ToHttp();
        dto.DataCompleteness = value.DataCompleteness?.ToHttp();
        return dto;
    }

    public static ParkDetailSummaryDto ToDetailSummaryHttp(this ParkDetailSummaryResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkDetailSummaryDto
        {
            Park = value.Park.ToHttp(),
            MainImage = value.MainImage?.ToHttp(),
            References = new ParkDetailReferenceSummaryDto
            {
                FounderName = value.FounderName,
                OperatorName = value.OperatorName,
            },
            Rating = value.Rating?.ToHttp(),
            Stats = new ParkDetailSummaryStatsDto
            {
                TotalItems = value.Stats.TotalItems,
                ZoneCount = value.Stats.ZoneCount,
                AttractionCount = value.Stats.AttractionCount,
                RestaurantCount = value.Stats.RestaurantCount,
                ShowCount = value.Stats.ShowCount,
                ShopCount = value.Stats.ShopCount,
                HotelCount = value.Stats.HotelCount,
                CountsByCategory = value.Stats.CountsByCategory.ToDictionary(
                    pair => pair.Key.ToString(),
                    pair => pair.Value,
                    StringComparer.Ordinal),
            },
        };
    }


    public static ParkMapItemsDto ToMapItemsHttp(this ParkMapItemsResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkMapItemsDto
        {
            Park = value.Park.ToHttp(),
            Items = value.Items.Select(static item => new ParkMapItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category.ToString(),
                Type = item.Type.ToString(),
                Subtype = item.Subtype,
                ZoneId = item.ZoneId,
                Descriptions = item.Descriptions.ToHttp(),
                AttractionDetails = item.AttractionDetails.ToMapHttp(),
                Latitude = item.Latitude,
                Longitude = item.Longitude,
            }).ToList(),
            UnlocatedItems = value.UnlocatedItems.Select(static item => new ParkMapUnlocatedItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Category = item.Category.ToString(),
                Type = item.Type.ToString(),
                Subtype = item.Subtype,
                ZoneId = item.ZoneId,
                Descriptions = item.Descriptions.ToHttp(),
                AttractionDetails = item.AttractionDetails.ToMapHttp(),
            }).ToList(),
            Zones = value.Zones.Select(static zone => new ParkMapZoneDto
            {
                Id = zone.Id,
                Name = zone.Name,
                SortOrder = zone.SortOrder,
            }).ToList(),
        };
    }

    public static ParkMapPointDto ToMapPointHttp(this Park value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkMapPointDto
        {
            Id = value.Id ?? string.Empty,
            Name = value.Name ?? string.Empty,
            CountryCode = value.CountryCode,
            AudienceClassification = value.AudienceClassification.ToHttp(),
            City = value.City,
            Street = value.Street,
            PostalCode = value.PostalCode,
            Latitude = value.Position?.Latitude ?? 0.0,
            Longitude = value.Position?.Longitude ?? 0.0,
            CurrentLogoImageId = value.CurrentLogoImageId,
        };
    }

    public static ParkDistanceResponseDto ToDistanceHttp(this ParkDistanceResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkDistanceResponseDto
        {
            Source = value.SourcePark.ToMapPointHttp(),
            DistanceUnit = value.DistanceUnit,
            CalculationKind = value.CalculationKind,
            Targets = value.Targets.Select(target => target.ToDistanceHttp(value.DistanceUnit)).ToList(),
            MissingTargetParkIds = value.MissingTargetParkIds.ToList(),
            UnavailableTargetParkIds = value.UnavailableTargetParkIds.ToList(),
        };
    }

    public static ParkDistanceTargetDto ToDistanceHttp(this ParkDistanceTargetResult value, string distanceUnit)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkDistanceTargetDto
        {
            ProximityRank = value.ProximityRank,
            DistanceKilometers = value.DistanceKilometers,
            DistanceMeters = Math.Round(value.DistanceKilometers * 1000d, 0, MidpointRounding.AwayFromZero),
            DistanceUnit = distanceUnit,
            EstimatedTravelDurationMinutes = value.EstimatedTravelDurationMinutes,
            Park = value.Park.ToHttp(),
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
        ParkItemsByZoneGrouping groupedItems = GroupItemsByZoneId(items);

        ParkExplorerBucketDto overview = BuildBucket(
            id: null,
            name: "overview",
            names: null,
            slug: null,
            isVirtual: true,
            items: items);

        List<ParkExplorerBucketDto> zoneBuckets = zones
            .Select(zone =>
            {
                IReadOnlyCollection<ParkItem> zoneItems = !string.IsNullOrWhiteSpace(zone.Id)
                    && groupedItems.ItemsByZoneId.TryGetValue(zone.Id, out List<ParkItem>? zoneGroup)
                        ? zoneGroup
                        : Array.Empty<ParkItem>();

                return BuildBucket(
                    zone.Id,
                    ResolveDisplayName(zone.Names, zone.Name),
                    zone.Names,
                    zone.Slug,
                    false,
                    zoneItems);
            })
            .ToList();

        ParkExplorerBucketDto? unassignedBucket = null;
        if (groupedItems.UnassignedItems.Count > 0)
        {
            unassignedBucket = BuildBucket(null, "unassigned", null, null, true, groupedItems.UnassignedItems);
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

    private static ParkItemsByZoneGrouping GroupItemsByZoneId(IEnumerable<ParkItem> items)
    {
        Dictionary<string, List<ParkItem>> itemsByZoneId = new(StringComparer.Ordinal);
        List<ParkItem> unassignedItems = new();
        foreach (ParkItem item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ZoneId))
            {
                unassignedItems.Add(item);
                continue;
            }

            if (!itemsByZoneId.TryGetValue(item.ZoneId, out List<ParkItem>? zoneItems))
            {
                zoneItems = new List<ParkItem>();
                itemsByZoneId.Add(item.ZoneId, zoneItems);
            }

            zoneItems.Add(item);
        }

        return new ParkItemsByZoneGrouping(itemsByZoneId, unassignedItems);
    }

    private static ParkExplorerBucketDto BuildBucket(string? id, string name, IEnumerable<AmusementPark.Core.Localization.LocalizedText>? names, string? slug, bool isVirtual, IReadOnlyCollection<ParkItem> items)
    {
        ParkExplorerBucketCounts counts = CountBucketItems(items);

        return new ParkExplorerBucketDto
        {
            Id = id,
            Name = name,
            Names = names?.ToHttp() ?? new List<LocalizedTextDto>(),
            Slug = slug,
            IsVirtual = isVirtual,
            TotalItems = counts.TotalItems,
            CountsByCategory = ToOrderedCountDtos(counts.CountsByCategory),
            CountsByType = ToOrderedCountDtos(counts.CountsByType),
        };
    }

    private static ParkExplorerBucketCounts CountBucketItems(IEnumerable<ParkItem> items)
    {
        Dictionary<string, int> countsByCategory = new(StringComparer.Ordinal);
        Dictionary<string, int> countsByType = new(StringComparer.Ordinal);
        int totalItems = 0;
        foreach (ParkItem item in items)
        {
            totalItems += 1;
            string categoryKey = item.Category.ToString();
            countsByCategory[categoryKey] = countsByCategory.GetValueOrDefault(categoryKey) + 1;

            string typeKey = item.Type.ToString();
            countsByType[typeKey] = countsByType.GetValueOrDefault(typeKey) + 1;
        }

        return new ParkExplorerBucketCounts(totalItems, countsByCategory, countsByType);
    }

    private sealed class ParkItemsByZoneGrouping
    {
        public ParkItemsByZoneGrouping(Dictionary<string, List<ParkItem>> itemsByZoneId, List<ParkItem> unassignedItems)
        {
            this.ItemsByZoneId = itemsByZoneId;
            this.UnassignedItems = unassignedItems;
        }

        public Dictionary<string, List<ParkItem>> ItemsByZoneId { get; }

        public List<ParkItem> UnassignedItems { get; }
    }

    private sealed class ParkExplorerBucketCounts
    {
        public ParkExplorerBucketCounts(int totalItems, Dictionary<string, int> countsByCategory, Dictionary<string, int> countsByType)
        {
            this.TotalItems = totalItems;
            this.CountsByCategory = countsByCategory;
            this.CountsByType = countsByType;
        }

        public int TotalItems { get; }

        public Dictionary<string, int> CountsByCategory { get; }

        public Dictionary<string, int> CountsByType { get; }
    }

    private static List<ParkZoneSummaryCountDto> ToOrderedCountDtos(Dictionary<string, int> counts)
    {
        return counts
            .OrderBy(static count => count.Key, StringComparer.Ordinal)
            .Select(static count => new ParkZoneSummaryCountDto
            {
                Key = count.Key,
                Count = count.Value,
            })
            .ToList();
    }

    private static ParkMapAttractionDetailsDto? ToMapHttp(this ParkMapAttractionDetailsResult? value)
    {
        if (value is null)
        {
            return null;
        }

        return new ParkMapAttractionDetailsDto
        {
            ManufacturerId = value.ManufacturerId,
            Model = value.Model,
            Status = value.Status,
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

    private static ParkOpeningHoursAdminSummaryDto ToHttp(this ParkOpeningHoursAdminSummaryResult value)
    {
        return new ParkOpeningHoursAdminSummaryDto
        {
            HasOpeningHours = value.HasOpeningHours,
            Status = value.Status.ToString(),
            TimeZoneId = value.TimeZoneId,
            FirstDate = value.FirstDate.HasValue ? FormatDate(value.FirstDate.Value) : null,
            LastDate = value.LastDate.HasValue ? FormatDate(value.LastDate.Value) : null,
            CompleteUntilDate = value.CompleteUntilDate.HasValue ? FormatDate(value.CompleteUntilDate.Value) : null,
            CompleteForDays = value.CompleteForDays,
            WarningThresholdDays = value.WarningThresholdDays,
            LastVerifiedAtUtc = value.LastVerifiedAtUtc,
            UpdatedAtUtc = value.UpdatedAtUtc,
        };
    }

    public static DataCompletenessScoreDto ToHttp(this DataCompletenessScore value)
    {
        return new DataCompletenessScoreDto
        {
            CompletenessScore = value.CompletenessScore,
            DataQualityLevel = value.DataQualityLevel.ToString(),
            ApplicableMaxPoints = value.ApplicableMaxPoints,
            EarnedPoints = value.EarnedPoints,
        };
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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

    private static ParkAudienceClassification? ToDomain(this ParkAudienceClassificationDto? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Enum.TryParse(value.Value.ToString(), out ParkAudienceClassification parsed) ? parsed : null;
    }

    private static ParkAudienceClassificationDto? ToHttp(this ParkAudienceClassification? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return Enum.TryParse(value.Value.ToString(), out ParkAudienceClassificationDto parsed) ? parsed : null;
    }

    private static ParkStatus ToDomain(this ParkStatusDto value)
    {
        return Enum.TryParse(value.ToString(), out ParkStatus parsed) ? parsed : ParkStatus.Operating;
    }

    private static ParkStatusDto ToHttp(this ParkStatus value)
    {
        return Enum.TryParse(value.ToString(), out ParkStatusDto parsed) ? parsed : ParkStatusDto.Operating;
    }
}
