using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.StandaloneAttractions;

namespace AmusementPark.WebAPI.Mappers;

internal static class StandaloneAttractionsHttpMappers
{
    public static StandaloneAttraction ToDomain(this StandaloneAttractionCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Name = dto.Name,
            CountryCode = dto.CountryCode,
            Type = dto.Type.ToDomain(),
            Subtype = dto.Subtype,
            OperatorId = dto.OperatorId,
            WebsiteUrl = dto.WebsiteUrl,
            Street = dto.Street,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Descriptions = dto.Descriptions.ToDomain(),
            AttractionDetails = dto.AttractionDetails?.ToStandaloneDomain(),
            AttractionLocations = dto.AttractionLocations?.ToStandaloneDomain(),
            IsVisible = dto.IsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
            LegacyParkId = dto.LegacyParkId,
            LegacyParkItemId = dto.LegacyParkItemId,
        };

        ApplyOptionalPosition(attraction, dto.Latitude, dto.Longitude);
        return attraction;
    }

    public static StandaloneAttraction ToDomain(this StandaloneAttractionUpdateDto dto)
    {
        return ((StandaloneAttractionCreateDto)dto).ToDomain();
    }

    public static StandaloneAttractionDto ToHttp(this StandaloneAttraction value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new StandaloneAttractionDto
        {
            Id = value.Id,
            Name = value.Name,
            CountryCode = value.CountryCode,
            Type = value.Type.ToHttp(),
            Subtype = value.Subtype,
            OperatorId = value.OperatorId,
            WebsiteUrl = value.WebsiteUrl,
            Street = value.Street,
            City = value.City,
            PostalCode = value.PostalCode,
            Latitude = value.Position?.Latitude,
            Longitude = value.Position?.Longitude,
            Descriptions = value.Descriptions.ToHttp(),
            AttractionDetails = value.AttractionDetails?.ToStandaloneHttp(),
            AttractionLocations = value.AttractionLocations?.ToStandaloneHttp(),
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus.ToHttp(),
            LegacyParkId = value.LegacyParkId,
            LegacyParkItemId = value.LegacyParkItemId,
        };
    }

    public static StandaloneAttractionMigrationRequest ToApplication(this StandaloneAttractionMigrationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new StandaloneAttractionMigrationRequest
        {
            LegacyParkId = dto.LegacyParkId,
            LegacyParkItemId = dto.LegacyParkItemId,
            TargetStandaloneAttractionId = dto.TargetStandaloneAttractionId,
            RetireLegacyPark = dto.RetireLegacyPark,
            RetireLegacyParkItem = dto.RetireLegacyParkItem,
        };
    }

    public static PaginationDto ToHttp(this PagedResult<StandaloneAttraction> value)
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

    private static AttractionDetails ToStandaloneDomain(this AttractionDetailsDto dto)
    {
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
            AccessConditions = dto.AccessConditions?.Select(static value => value.ToStandaloneDomain()).ToList() ?? new List<AttractionAccessCondition>(),
        };

        MeasurementConversionService.Instance.NormalizeAttractionDetails(details);
        return details;
    }

    private static AttractionDetailsDto ToStandaloneHttp(this AttractionDetails value)
    {
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
            AccessConditions = value.AccessConditions.Count > 0 ? value.AccessConditions.Select(static condition => condition.ToStandaloneHttp()).ToList() : null,
        };
    }

    private static AttractionAccessCondition ToStandaloneDomain(this AttractionAccessConditionDto dto)
    {
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

    private static AttractionAccessConditionDto ToStandaloneHttp(this AttractionAccessCondition value)
    {
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

    private static AttractionLocations ToStandaloneDomain(this AttractionLocationsDto dto)
    {
        return new AttractionLocations
        {
            Entrance = dto.Entrance.ToDomain(),
            Exit = dto.Exit.ToDomain(),
            FastPassEntrance = dto.FastPassEntrance.ToDomain(),
            ReducedMobilityEntrance = dto.ReducedMobilityEntrance.ToDomain(),
        };
    }

    private static AttractionLocationsDto ToStandaloneHttp(this AttractionLocations value)
    {
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

    private static void ApplyOptionalPosition(StandaloneAttraction attraction, double? latitude, double? longitude)
    {
        if (latitude.HasValue && longitude.HasValue)
        {
            attraction.SetPosition(latitude.Value, longitude.Value);
            return;
        }

        attraction.ClearPosition();
    }

    private static ParkItemType ToDomain(this ParkItemTypeDto value)
    {
        return Enum.TryParse(value.ToString(), out ParkItemType parsed) ? parsed : ParkItemType.Attraction;
    }

    private static ParkItemTypeDto ToHttp(this ParkItemType value)
    {
        return Enum.TryParse(value.ToString(), out ParkItemTypeDto parsed) ? parsed : ParkItemTypeDto.Attraction;
    }

    private static AttractionWaterExposureLevel ToDomain(this AttractionWaterExposureLevelDto value)
    {
        return Enum.TryParse(value.ToString(), out AttractionWaterExposureLevel parsed) ? parsed : AttractionWaterExposureLevel.None;
    }

    private static AttractionWaterExposureLevelDto ToHttp(this AttractionWaterExposureLevel value)
    {
        return Enum.TryParse(value.ToString(), out AttractionWaterExposureLevelDto parsed) ? parsed : AttractionWaterExposureLevelDto.None;
    }

    private static AttractionAccessConditionType ToDomain(this AttractionAccessConditionTypeDto value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionType parsed) ? parsed : AttractionAccessConditionType.Custom;
    }

    private static AttractionAccessConditionTypeDto ToHttp(this AttractionAccessConditionType value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionTypeDto parsed) ? parsed : AttractionAccessConditionTypeDto.Custom;
    }

    private static AttractionAccessConditionUnit ToDomain(this AttractionAccessConditionUnitDto value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionUnit parsed) ? parsed : AttractionAccessConditionUnit.Centimeter;
    }

    private static AttractionAccessConditionUnitDto ToHttp(this AttractionAccessConditionUnit value)
    {
        return Enum.TryParse(value.ToString(), out AttractionAccessConditionUnitDto parsed) ? parsed : AttractionAccessConditionUnitDto.Centimeter;
    }
}

