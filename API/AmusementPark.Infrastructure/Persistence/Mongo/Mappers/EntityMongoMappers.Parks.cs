using AmusementPark.Application.Features.CaptainCoaster.Results;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Images;
using System;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

/// <summary>
/// Mappers centralisés domaine/resultats applicatifs &lt;-&gt; documents Mongo.
/// </summary>
internal static partial class EntityMongoMappers
{
    public static Park ToDomain(this ParkDocument document)
    {
        Park entity = new Park
        {
            Id = document.Id,
            Name = document.Name,
            CountryCode = document.CountryCode,
            Type = document.Type,
            AudienceClassification = document.AudienceClassification,
            Status = document.Status,
            OpeningDate = document.OpeningDate,
            ClosingDate = document.ClosingDate,
            OpeningDateText = document.OpeningDateText,
            ClosingDateText = document.ClosingDateText,
            FounderId = document.FounderId,
            OperatorId = document.OperatorId,
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsVisible = document.IsVisible,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
            IsFeaturedOnHome = document.IsFeaturedOnHome,
            FeaturedHomeOrder = document.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = document.IsFeaturedOnHomeSponsored,
            WebsiteUrl = document.WebsiteUrl,
            Street = document.Street,
            City = document.City,
            PostalCode = document.PostalCode,
            CurrentLogoImageId = document.CurrentLogoImageId,
        };

        CommonMongoMappers.ApplyPosition(entity, document.Latitude, document.Longitude);
        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkDocument ToDocument(this Park entity)
    {
        ParkDocument document = new ParkDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            CountryCode = entity.CountryCode,
            Type = entity.Type,
            AudienceClassification = entity.AudienceClassification,
            Status = entity.Status,
            OpeningDate = entity.OpeningDate,
            ClosingDate = entity.ClosingDate,
            OpeningDateText = entity.OpeningDateText,
            ClosingDateText = entity.ClosingDateText,
            FounderId = entity.FounderId,
            OperatorId = entity.OperatorId,
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsVisible = entity.IsVisible,
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            IsFeaturedOnHome = entity.IsFeaturedOnHome,
            FeaturedHomeOrder = entity.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = entity.IsFeaturedOnHomeSponsored,
            WebsiteUrl = entity.WebsiteUrl,
            Street = entity.Street,
            City = entity.City,
            PostalCode = entity.PostalCode,
            CurrentLogoImageId = entity.CurrentLogoImageId,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };

        CommonMongoMappers.ApplyPosition(document, entity.Position);
        return document;
    }

    public static ParkZone ToDomain(this ParkZoneDocument document)
    {
        ParkZone entity = new ParkZone
        {
            Id = document.Id,
            ParkId = document.ParkId,
            Name = document.Name,
            Names = CommonMongoMappers.ToDomain(document.Names),
            Slug = document.Slug,
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsVisible = document.IsVisible,
            SortOrder = document.SortOrder,
        };

        CommonMongoMappers.ApplyPosition(entity, document.Latitude, document.Longitude);
        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkZoneDocument ToDocument(this ParkZone entity)
    {
        ParkZoneDocument document = new ParkZoneDocument
        {
            Id = entity.Id,
            ParkId = entity.ParkId,
            Name = entity.Name,
            Names = CommonMongoMappers.ToDocuments(entity.Names),
            Slug = entity.Slug,
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsVisible = entity.IsVisible,
            SortOrder = entity.SortOrder,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };

        CommonMongoMappers.ApplyPosition(document, entity.Position);
        return document;
    }

    public static ParkItem ToDomain(this ParkItemDocument document)
    {
        ParkItem entity = new ParkItem
        {
            Id = document.Id,
            ParkId = document.ParkId,
            ZoneId = document.ZoneId,
            Name = document.Name,
            Category = document.Category,
            Type = document.Type,
            Subtype = document.Subtype,
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            AttractionDetails = document.AttractionDetails?.ToDomain(),
            AttractionLocations = document.AttractionLocations?.ToDomain(),
            IsVisible = document.IsVisible,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
        };

        CommonMongoMappers.ApplyPosition(entity, document.Latitude, document.Longitude);
        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkItemDocument ToDocument(this ParkItem entity)
    {
        ParkItemDocument document = new ParkItemDocument
        {
            Id = entity.Id,
            ParkId = entity.ParkId,
            ZoneId = entity.ZoneId,
            Name = entity.Name,
            Category = entity.Category,
            Type = entity.Type,
            Subtype = entity.Subtype,
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            AttractionDetails = entity.AttractionDetails?.ToDocument(),
            AttractionLocations = entity.AttractionLocations?.ToDocument(),
            IsVisible = entity.IsVisible,
            AdminReviewStatus = entity.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = entity.AdminReviewStatus.ToAdminReviewPriority(),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };

        CommonMongoMappers.ApplyPosition(document, entity.Position);
        return document;
    }

    public static AttractionDetails ToDomain(this AttractionDetailsDocument document)
    {
        AttractionDetails entity = new AttractionDetails
        {
            ManufacturerId = document.ManufacturerId,
            Model = document.Model,
            ExternalSource = document.ExternalSource,
            ExternalId = document.ExternalId,
            SourceUrl = document.SourceUrl,
            Status = ParkItemStatusNormalizer.Normalize(document.Status),
            MaterialType = document.MaterialType,
            SeatingType = document.SeatingType,
            LaunchType = document.LaunchType,
            RestraintType = document.RestraintType,
            IsLaunched = document.IsLaunched,
            OpeningDate = document.OpeningDate,
            ClosingDate = document.ClosingDate,
            OpeningDateText = document.OpeningDateText,
            ClosingDateText = document.ClosingDateText,
            DurationInSeconds = document.DurationInSeconds,
            CapacityPerHour = document.CapacityPerHour,
            HeightInFeet = document.HeightInFeet,
            HeightInMeters = document.HeightInMeters,
            LengthInFeet = document.LengthInFeet,
            LengthInMeters = document.LengthInMeters,
            SpeedInMph = document.SpeedInMph,
            SpeedInKmH = document.SpeedInKmH,
            DropInFeet = document.DropInFeet,
            DropInMeters = document.DropInMeters,
            InversionCount = document.InversionCount,
            TrainCount = document.TrainCount,
            CarsPerTrain = document.CarsPerTrain,
            RidersPerVehicle = document.RidersPerVehicle,
            HasSingleRider = document.HasSingleRider,
            HasFastPass = document.HasFastPass,
            IsAccessibleForReducedMobility = document.IsAccessibleForReducedMobility,
            IsIndoor = document.IsIndoor,
            WaterExposureLevel = document.WaterExposureLevel,
            AccessConditions = document.AccessConditions.Select(ToDomain).ToList(),
        };

        return entity;
    }

    public static AttractionDetailsDocument ToDocument(this AttractionDetails entity)
    {
        return new AttractionDetailsDocument
        {
            ManufacturerId = entity.ManufacturerId,
            Model = entity.Model,
            ExternalSource = entity.ExternalSource,
            ExternalId = entity.ExternalId,
            SourceUrl = entity.SourceUrl,
            Status = ParkItemStatusNormalizer.Normalize(entity.Status),
            MaterialType = entity.MaterialType,
            SeatingType = entity.SeatingType,
            LaunchType = entity.LaunchType,
            RestraintType = entity.RestraintType,
            IsLaunched = entity.IsLaunched,
            OpeningDate = entity.OpeningDate,
            ClosingDate = entity.ClosingDate,
            OpeningDateText = entity.OpeningDateText,
            ClosingDateText = entity.ClosingDateText,
            DurationInSeconds = entity.DurationInSeconds,
            CapacityPerHour = entity.CapacityPerHour,
            HeightInFeet = entity.HeightInFeet,
            HeightInMeters = entity.HeightInMeters,
            LengthInFeet = entity.LengthInFeet,
            LengthInMeters = entity.LengthInMeters,
            SpeedInMph = entity.SpeedInMph,
            SpeedInKmH = entity.SpeedInKmH,
            DropInFeet = entity.DropInFeet,
            DropInMeters = entity.DropInMeters,
            InversionCount = entity.InversionCount,
            TrainCount = entity.TrainCount,
            CarsPerTrain = entity.CarsPerTrain,
            RidersPerVehicle = entity.RidersPerVehicle,
            HasSingleRider = entity.HasSingleRider,
            HasFastPass = entity.HasFastPass,
            IsAccessibleForReducedMobility = entity.IsAccessibleForReducedMobility,
            IsIndoor = entity.IsIndoor,
            WaterExposureLevel = entity.WaterExposureLevel,
            AccessConditions = entity.AccessConditions.Select(ToDocument).ToList(),
        };
    }

    public static AttractionAccessCondition ToDomain(this AttractionAccessConditionDocument document)
    {
        return new AttractionAccessCondition
        {
            Type = document.Type,
            TypeKey = document.TypeKey,
            IsCustom = document.IsCustom,
            CustomTypeKey = document.CustomTypeKey,
            CustomTypeLabel = CommonMongoMappers.ToDomain(document.CustomTypeLabel),
            Value = document.Value,
            Unit = document.Unit,
            RequiresAccompaniment = document.RequiresAccompaniment,
            MinimumCompanionAge = document.MinimumCompanionAge,
            Label = CommonMongoMappers.ToDomain(document.Label),
            Description = CommonMongoMappers.ToDomain(document.Description),
            DisplayOrder = document.DisplayOrder,
        };
    }

    public static AttractionAccessConditionDocument ToDocument(this AttractionAccessCondition entity)
    {
        return new AttractionAccessConditionDocument
        {
            Type = entity.Type,
            TypeKey = entity.TypeKey,
            IsCustom = entity.IsCustom,
            CustomTypeKey = entity.CustomTypeKey,
            CustomTypeLabel = CommonMongoMappers.ToDocuments(entity.CustomTypeLabel),
            Value = entity.Value,
            Unit = entity.Unit,
            RequiresAccompaniment = entity.RequiresAccompaniment,
            MinimumCompanionAge = entity.MinimumCompanionAge,
            Label = CommonMongoMappers.ToDocuments(entity.Label),
            Description = CommonMongoMappers.ToDocuments(entity.Description),
            DisplayOrder = entity.DisplayOrder,
        };
    }

    public static AttractionLocations ToDomain(this AttractionLocationsDocument document)
    {
        return new AttractionLocations
        {
            Entrance = CommonMongoMappers.ToDomain(document.Entrance),
            Exit = CommonMongoMappers.ToDomain(document.Exit),
            FastPassEntrance = CommonMongoMappers.ToDomain(document.FastPassEntrance),
            ReducedMobilityEntrance = CommonMongoMappers.ToDomain(document.ReducedMobilityEntrance),
        };
    }

    public static AttractionLocationsDocument ToDocument(this AttractionLocations entity)
    {
        return new AttractionLocationsDocument
        {
            Entrance = CommonMongoMappers.ToDocument(entity.Entrance),
            Exit = CommonMongoMappers.ToDocument(entity.Exit),
            FastPassEntrance = CommonMongoMappers.ToDocument(entity.FastPassEntrance),
            ReducedMobilityEntrance = CommonMongoMappers.ToDocument(entity.ReducedMobilityEntrance),
        };
    }
    public static AttractionAccessConditionTypeDefinition ToDomain(this AttractionAccessConditionTypeDefinitionDocument document)
    {
        return new AttractionAccessConditionTypeDefinition
        {
            Id = document.Id,
            Key = document.Key,
            LegacyType = document.LegacyType,
            IsSystem = document.IsSystem,
            IsActive = document.IsActive,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            SortOrder = document.SortOrder,
        };
    }

    public static AttractionAccessConditionTypeDefinitionDocument ToDocument(this AttractionAccessConditionTypeDefinition entity)
    {
        return new AttractionAccessConditionTypeDefinitionDocument
        {
            Id = string.IsNullOrWhiteSpace(entity.Id) ? Guid.NewGuid().ToString("N") : entity.Id,
            Key = entity.Key,
            LegacyType = entity.LegacyType,
            IsSystem = entity.IsSystem,
            IsActive = entity.IsActive,
            Labels = CommonMongoMappers.ToDocuments(entity.Labels),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            SortOrder = entity.SortOrder,
        };
    }

}
