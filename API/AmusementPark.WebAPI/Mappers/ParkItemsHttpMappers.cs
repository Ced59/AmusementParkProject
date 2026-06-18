using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de mapping HTTP pour la feature ParkItems migrée en phase 8.
/// </summary>
internal static class ParkItemsHttpMappers
{
    public static ParkItem ToDomain(this ParkItemCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        ParkItem parkItem = new ParkItem
        {
            ParkId = dto.ParkId,
            ZoneId = dto.ZoneId,
            Name = dto.Name,
            Category = dto.Category.ToDomain(),
            Type = dto.Type.ToDomain(),
            Subtype = dto.Subtype,
            Descriptions = dto.Descriptions.ToDomain(),
            AttractionDetails = dto.AttractionDetails?.ToDomain(),
            AttractionLocations = dto.AttractionLocations?.ToDomain(),
            IsVisible = dto.IsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
        };

        ApplyOptionalPosition(parkItem, dto.Latitude, dto.Longitude);
        return parkItem;
    }

    public static ParkItem ToDomain(this ParkItemQuickCreateDto dto, GeoPoint? fallbackPosition = null)
    {
        ArgumentNullException.ThrowIfNull(dto);

        ParkItemCategory category = dto.Category.HasValue
            ? dto.Category.Value.ToDomain()
            : ParkItemAdministrationDefaults.QuickCreateCategory;
        ParkItemType type = dto.Type.HasValue
            ? dto.Type.Value.ToDomain()
            : ParkItemAdministrationDefaults.GetDefaultType(category);

        ParkItem parkItem = new ParkItem
        {
            ParkId = dto.ParkId,
            ZoneId = dto.ZoneId,
            Name = dto.Name,
            Category = category,
            Type = type,
            Descriptions = new List<LocalizedText>(),
            AttractionDetails = BuildQuickCreateAttractionDetails(category, dto.ManufacturerId),
            AttractionLocations = null,
            IsVisible = dto.IsVisible ?? ParkItemAdministrationDefaults.QuickCreateIsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToOptionalDomain() ?? ParkItemAdministrationDefaults.QuickCreateAdminReviewStatus,
        };

        ApplyOptionalPosition(parkItem, dto.Latitude, dto.Longitude);
        ParkItemAdministrationDefaults.ApplyQuickCreateDefaults(parkItem, fallbackPosition);
        return parkItem;
    }

    public static ParkItem ToDomain(this ParkItemUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        ParkItem parkItem = new ParkItem
        {
            ParkId = dto.ParkId,
            ZoneId = dto.ZoneId,
            Name = dto.Name,
            Category = dto.Category.ToDomain(),
            Type = dto.Type.ToDomain(),
            Subtype = dto.Subtype,
            Descriptions = dto.Descriptions.ToDomain(),
            AttractionDetails = dto.AttractionDetails?.ToDomain(),
            AttractionLocations = dto.AttractionLocations?.ToDomain(),
            IsVisible = dto.IsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
        };

        ApplyOptionalPosition(parkItem, dto.Latitude, dto.Longitude);
        return parkItem;
    }

    public static ParkItemDto ToHttp(this ParkItem value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkItemDto
        {
            Id = value.Id,
            ParkId = value.ParkId,
            ZoneId = value.ZoneId,
            Name = value.Name,
            Category = value.Category.ToHttp(),
            Type = value.Type.ToHttp(),
            Subtype = value.Subtype,
            Latitude = value.Position?.Latitude,
            Longitude = value.Position?.Longitude,
            Descriptions = value.Descriptions.ToHttp(),
            AttractionDetails = value.AttractionDetails?.ToHttp(),
            AttractionLocations = value.AttractionLocations?.ToHttp(),
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
        };
    }

    public static UpdateParkItemsBulkFieldsCommand ToApplication(this ParkItemBulkFieldsUpdateDto value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new UpdateParkItemsBulkFieldsCommand(
            value.Ids,
            value.UpdateZone,
            value.ZoneId,
            value.Category.HasValue ? value.Category.Value.ToDomain() : null,
            value.Type.HasValue ? value.Type.Value.ToDomain() : null,
            value.UpdateManufacturer,
            value.ManufacturerId,
            value.IsVisible,
            value.AdminReviewStatus.ToOptionalDomain());
    }

    public static PreviewParkItemsBulkCreateCommand ToPreviewApplication(this ParkItemsBulkCreateRequestDto value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PreviewParkItemsBulkCreateCommand(
            value.ParkId,
            value.Rows.Select(static row => row.ToApplication()).ToList());
    }

    public static ApplyParkItemsBulkCreateCommand ToApplyApplication(this ParkItemsBulkCreateRequestDto value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ApplyParkItemsBulkCreateCommand(
            value.ParkId,
            value.Rows.Select(static row => row.ToApplication()).ToList());
    }

    public static ParkItemsBulkCreatePreviewResultDto ToHttp(this ParkItemsBulkCreatePreviewResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkItemsBulkCreatePreviewResultDto
        {
            Rows = value.Rows.Select(static row => row.ToHttp()).ToList(),
            ReadyCount = value.ReadyCount,
            WarningCount = value.WarningCount,
            ErrorCount = value.ErrorCount,
        };
    }

    public static ParkItemsBulkCreateApplyResultDto ToHttp(this ParkItemsBulkCreateApplyResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkItemsBulkCreateApplyResultDto
        {
            Rows = value.Rows.Select(static row => row.ToHttp()).ToList(),
            CreatedIds = value.CreatedIds,
            RequestedCount = value.RequestedCount,
            CreatedCount = value.CreatedCount,
            IgnoredCount = value.IgnoredCount,
        };
    }

    public static ParkItemAdminListDto ToHttp(this ParkItemAdminListResult value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkItemAdminListDto
        {
            Id = value.Id,
            ParkId = value.ParkId,
            ParkName = value.ParkName,
            ZoneId = value.ZoneId,
            Name = value.Name,
            Category = value.Category.ToHttp(),
            Type = value.Type.ToHttp(),
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
            ContentQuality = value.ContentQuality.ToHttp(),
            PublicationSignals = value.PublicationSignals.ToHttp(),
        };
    }

    private static ParkItemContentQualityDto ToHttp(this ParkItemContentQualityResult value)
    {
        return new ParkItemContentQualityDto
        {
            StructureComplete = value.StructureComplete,
            HasAnyDescription = value.HasAnyDescription,
            HasFrenchDescription = value.HasFrenchDescription,
            HasEnglishDescription = value.HasEnglishDescription,
            HasZone = value.HasZone,
            HasPreciseType = value.HasPreciseType,
            HasLocation = value.HasLocation,
            HasAccessConditions = value.HasAccessConditions,
            IsPublishable = value.IsPublishable,
            AvailableLanguageCodes = value.AvailableLanguageCodes,
            MissingRequirementKeys = value.MissingRequirementKeys,
        };
    }

    private static ParkItemAdminPublicationSignalsDto ToHttp(this ParkItemAdminPublicationSignalsResult value)
    {
        return new ParkItemAdminPublicationSignalsDto
        {
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
            LastUpdatedAtUtc = value.LastUpdatedAtUtc,
            AvailableLanguageCodes = value.AvailableLanguageCodes,
            IsPublishable = value.IsPublishable,
        };
    }

    private static ParkItemBulkCreateDraft ToApplication(this ParkItemBulkCreateDraftDto value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkItemBulkCreateDraft
        {
            RowNumber = value.RowNumber,
            Name = value.Name,
            Category = value.Category.HasValue ? value.Category.Value.ToDomain() : null,
            Type = value.Type.HasValue ? value.Type.Value.ToDomain() : null,
            ZoneId = value.ZoneId,
            ZoneName = value.ZoneName,
            ManufacturerId = value.ManufacturerId,
            ManufacturerName = value.ManufacturerName,
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus.ToOptionalDomain(),
            DescriptionFr = value.DescriptionFr,
        };
    }

    private static ParkItemBulkCreatePreviewRowDto ToHttp(this ParkItemBulkCreatePreviewRow value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ParkItemBulkCreatePreviewRowDto
        {
            RowNumber = value.RowNumber,
            Name = value.Name,
            Category = value.Category.ToHttp(),
            Type = value.Type.ToHttp(),
            ZoneId = value.ZoneId,
            ZoneName = value.ZoneName,
            ManufacturerId = value.ManufacturerId,
            ManufacturerName = value.ManufacturerName,
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
            DescriptionFr = value.DescriptionFr,
            CanApply = value.CanApply,
            Errors = value.Errors,
            Warnings = value.Warnings,
        };
    }

    public static PaginationDto ToHttp(this PagedResult<ParkItemAdminListResult> value)
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

    private static AttractionDetails ToDomain(this AttractionDetailsDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        AttractionDetails details = new AttractionDetails
        {
            ManufacturerId = dto.ManufacturerId,
            Model = dto.Model,
            ExternalSource = dto.ExternalSource,
            ExternalId = dto.ExternalId,
            SourceUrl = dto.SourceUrl,
            Status = dto.Status,
            MaterialType = dto.MaterialType,
            SeatingType = dto.SeatingType,
            LaunchType = dto.LaunchType,
            RestraintType = dto.RestraintType,
            IsLaunched = dto.IsLaunched,
            OpeningDate = dto.OpeningDate,
            ClosingDate = dto.ClosingDate,
            OpeningDateText = dto.OpeningDateText,
            ClosingDateText = dto.ClosingDateText,
            DurationInSeconds = dto.DurationInSeconds,
            CapacityPerHour = dto.CapacityPerHour,
            HeightInFeet = dto.HeightInFeet,
            HeightInMeters = dto.HeightInMeters,
            LengthInFeet = dto.LengthInFeet,
            LengthInMeters = dto.LengthInMeters,
            SpeedInMph = dto.SpeedInMph,
            SpeedInKmH = dto.SpeedInKmH,
            DropInFeet = dto.DropInFeet,
            DropInMeters = dto.DropInMeters,
            InversionCount = dto.InversionCount,
            TrainCount = dto.TrainCount,
            CarsPerTrain = dto.CarsPerTrain,
            RidersPerVehicle = dto.RidersPerVehicle,
            HasSingleRider = dto.HasSingleRider,
            HasFastPass = dto.HasFastPass,
            IsAccessibleForReducedMobility = dto.IsAccessibleForReducedMobility,
            IsIndoor = dto.IsIndoor,
            WaterExposureLevel = dto.WaterExposureLevel?.ToDomain(),
            AccessConditions = dto.AccessConditions?.Select(static value => value.ToDomain()).ToList() ?? new List<AttractionAccessCondition>(),
        };

        MeasurementConversionService.Instance.NormalizeAttractionDetails(details);
        return details;
    }

    private static AttractionDetailsDto ToHttp(this AttractionDetails value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new AttractionDetailsDto
        {
            ManufacturerId = value.ManufacturerId,
            Model = value.Model,
            ExternalSource = value.ExternalSource,
            ExternalId = value.ExternalId,
            SourceUrl = value.SourceUrl,
            Status = value.Status,
            MaterialType = value.MaterialType,
            SeatingType = value.SeatingType,
            LaunchType = value.LaunchType,
            RestraintType = value.RestraintType,
            IsLaunched = value.IsLaunched,
            OpeningDate = value.OpeningDate,
            ClosingDate = value.ClosingDate,
            OpeningDateText = value.OpeningDateText,
            ClosingDateText = value.ClosingDateText,
            DurationInSeconds = value.DurationInSeconds,
            CapacityPerHour = value.CapacityPerHour,
            HeightInFeet = value.HeightInFeet,
            HeightInMeters = value.HeightInMeters,
            LengthInFeet = value.LengthInFeet,
            LengthInMeters = value.LengthInMeters,
            SpeedInMph = value.SpeedInMph,
            SpeedInKmH = value.SpeedInKmH,
            DropInFeet = value.DropInFeet,
            DropInMeters = value.DropInMeters,
            InversionCount = value.InversionCount,
            TrainCount = value.TrainCount,
            CarsPerTrain = value.CarsPerTrain,
            RidersPerVehicle = value.RidersPerVehicle,
            HasSingleRider = value.HasSingleRider,
            HasFastPass = value.HasFastPass,
            IsAccessibleForReducedMobility = value.IsAccessibleForReducedMobility,
            IsIndoor = value.IsIndoor,
            WaterExposureLevel = value.WaterExposureLevel?.ToHttp(),
            AccessConditions = value.AccessConditions.Count > 0
                ? value.AccessConditions.Select(static condition => condition.ToHttp()).ToList()
                : null,
        };
    }

    private static AttractionDetails? BuildQuickCreateAttractionDetails(ParkItemCategory category, string? manufacturerId)
    {
        if (category != ParkItemCategory.Attraction || string.IsNullOrWhiteSpace(manufacturerId))
        {
            return null;
        }

        return new AttractionDetails
        {
            ManufacturerId = manufacturerId.Trim(),
        };
    }

    private static void ApplyOptionalPosition(ParkItem parkItem, double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            parkItem.ClearPosition();
            return;
        }

        parkItem.SetPosition(latitude.Value, longitude.Value);
    }

    private static AttractionAccessCondition ToDomain(this AttractionAccessConditionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new AttractionAccessCondition
        {
            Type = dto.Type.ToDomain(),
            TypeKey = dto.TypeKey,
            IsCustom = dto.IsCustom,
            CustomTypeKey = dto.CustomTypeKey,
            CustomTypeLabel = dto.CustomTypeLabel.ToDomain(),
            Value = dto.Value,
            Unit = dto.Unit?.ToDomain(),
            RequiresAccompaniment = dto.RequiresAccompaniment,
            MinimumCompanionAge = dto.MinimumCompanionAge,
            Label = dto.Label.ToDomain(),
            Description = dto.Description.ToDomain(),
            DisplayOrder = dto.DisplayOrder,
        };
    }

    private static AttractionAccessConditionDto ToHttp(this AttractionAccessCondition value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new AttractionAccessConditionDto
        {
            Type = value.Type.ToHttp(),
            TypeKey = value.TypeKey,
            IsCustom = value.IsCustom,
            CustomTypeKey = value.CustomTypeKey,
            CustomTypeLabel = value.CustomTypeLabel.Count > 0 ? value.CustomTypeLabel.ToHttp() : null,
            Value = value.Value,
            Unit = value.Unit?.ToHttp(),
            RequiresAccompaniment = value.RequiresAccompaniment,
            MinimumCompanionAge = value.MinimumCompanionAge,
            Label = value.Label.Count > 0 ? value.Label.ToHttp() : null,
            Description = value.Description.Count > 0 ? value.Description.ToHttp() : null,
            DisplayOrder = value.DisplayOrder,
        };
    }

    private static AttractionLocations ToDomain(this AttractionLocationsDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new AttractionLocations
        {
            Entrance = dto.Entrance.ToDomain(),
            Exit = dto.Exit.ToDomain(),
            FastPassEntrance = dto.FastPassEntrance.ToDomain(),
            ReducedMobilityEntrance = dto.ReducedMobilityEntrance.ToDomain(),
        };
    }

    private static AttractionLocationsDto ToHttp(this AttractionLocations value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new AttractionLocationsDto
        {
            Entrance = value.Entrance.ToHttp(),
            Exit = value.Exit.ToHttp(),
            FastPassEntrance = value.FastPassEntrance.ToHttp(),
            ReducedMobilityEntrance = value.ReducedMobilityEntrance.ToHttp(),
        };
    }

    private static GeoPoint? ToDomain(this AttractionLocationPointDto? dto)
    {
        if (dto is null || dto.Latitude is null || dto.Longitude is null)
        {
            return null;
        }

        if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
        {
            return null;
        }

        return new GeoPoint(dto.Latitude.Value, dto.Longitude.Value);
    }

    private static AttractionLocationPointDto? ToHttp(this GeoPoint? value)
    {
        if (value is null)
        {
            return null;
        }

        return new AttractionLocationPointDto
        {
            Latitude = value.Latitude,
            Longitude = value.Longitude,
        };
    }

    private static ParkItemCategory ToDomain(this ParkItemCategoryDto value)
    {
        return Enum.TryParse(value.ToString(), out ParkItemCategory parsed)
            ? parsed
            : ParkItemCategory.Other;
    }

    private static ParkItemCategoryDto ToHttp(this ParkItemCategory value)
    {
        return Enum.TryParse(value.ToString(), out ParkItemCategoryDto parsed)
            ? parsed
            : ParkItemCategoryDto.Other;
    }

    private static ParkItemType ToDomain(this ParkItemTypeDto value)
    {
        return Enum.TryParse(value.ToString(), out ParkItemType parsed)
            ? parsed
            : ParkItemType.Other;
    }

    private static ParkItemTypeDto ToHttp(this ParkItemType value)
    {
        return Enum.TryParse(value.ToString(), out ParkItemTypeDto parsed)
            ? parsed
            : ParkItemTypeDto.Other;
    }

    private static AttractionWaterExposureLevel ToDomain(this AttractionWaterExposureLevelDto value)
    {
        return Enum.TryParse(value.ToString(), out AttractionWaterExposureLevel parsed)
            ? parsed
            : AttractionWaterExposureLevel.None;
    }

    private static AttractionWaterExposureLevelDto ToHttp(this AttractionWaterExposureLevel value)
    {
        return Enum.TryParse(value.ToString(), out AttractionWaterExposureLevelDto parsed)
            ? parsed
            : AttractionWaterExposureLevelDto.None;
    }

    private static AttractionAccessConditionType ToDomain(this AttractionAccessConditionTypeDto value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionType parsed)
            ? parsed
            : AttractionAccessConditionType.Custom;
    }

    private static AttractionAccessConditionTypeDto ToHttp(this AttractionAccessConditionType value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionTypeDto parsed)
            ? parsed
            : AttractionAccessConditionTypeDto.Custom;
    }

    private static AttractionAccessConditionUnit ToDomain(this AttractionAccessConditionUnitDto value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionUnit parsed)
            ? parsed
            : AttractionAccessConditionUnit.Centimeter;
    }

    private static AttractionAccessConditionUnitDto ToHttp(this AttractionAccessConditionUnit value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionUnitDto parsed)
            ? parsed
            : AttractionAccessConditionUnitDto.Centimeter;
    }
}
