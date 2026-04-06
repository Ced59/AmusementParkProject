using AmusementPark.Application.Features.CaptainCoaster.Results;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Images;
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
internal static class EntityMongoMappers
{
    public static Country ToDomain(this CountryDocument document)
    {
        Country entity = new Country
        {
            Id = document.Id,
            IsoCode = document.IsoCode,
            Names = CommonMongoMappers.ToDomain(document.Names),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static CountryDocument ToDocument(this Country entity)
    {
        return new CountryDocument
        {
            Id = entity.Id,
            IsoCode = entity.IsoCode,
            Names = CommonMongoMappers.ToDocuments(entity.Names),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ParkFounder ToDomain(this ParkFounderDocument document)
    {
        ParkFounder entity = new ParkFounder
        {
            Id = document.Id,
            Name = document.Name,
            Biography = CommonMongoMappers.ToDomain(document.Biography),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkFounderDocument ToDocument(this ParkFounder entity)
    {
        return new ParkFounderDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            Biography = CommonMongoMappers.ToDocuments(entity.Biography),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ParkOperator ToDomain(this ParkOperatorDocument document)
    {
        ParkOperator entity = new ParkOperator
        {
            Id = document.Id,
            Name = document.Name,
            Description = CommonMongoMappers.ToDomain(document.Description),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ParkOperatorDocument ToDocument(this ParkOperator entity)
    {
        return new ParkOperatorDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = CommonMongoMappers.ToDocuments(entity.Description),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static AttractionManufacturer ToDomain(this AttractionManufacturerDocument document)
    {
        AttractionManufacturer entity = new AttractionManufacturer
        {
            Id = document.Id,
            Name = document.Name,
            Biography = CommonMongoMappers.ToDomain(document.Biography),
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static AttractionManufacturerDocument ToDocument(this AttractionManufacturer entity)
    {
        return new AttractionManufacturerDocument
        {
            Id = entity.Id,
            Name = entity.Name,
            Biography = CommonMongoMappers.ToDocuments(entity.Biography),
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static Park ToDomain(this ParkDocument document)
    {
        Park entity = new Park
        {
            Id = document.Id,
            Name = document.Name,
            CountryCode = document.CountryCode,
            Type = document.Type,
            FounderId = document.FounderId,
            OperatorId = document.OperatorId,
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsVisible = document.IsVisible,
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
            FounderId = entity.FounderId,
            OperatorId = entity.OperatorId,
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsVisible = entity.IsVisible,
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
            Status = document.Status,
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
            Status = entity.Status,
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
            IsCustom = document.IsCustom,
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
            IsCustom = entity.IsCustom,
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

    public static Image ToDomain(this ImageDocument document)
    {
        Image entity = new Image
        {
            Id = document.Id,
            Category = document.Category,
            Path = document.Path,
            Description = document.Description,
            AltTexts = CommonMongoMappers.ToDomain(document.AltTexts),
            Captions = CommonMongoMappers.ToDomain(document.Captions),
            Credits = CommonMongoMappers.ToDomain(document.Credits),
            TagIds = document.TagIds,
            GeoLocation = CommonMongoMappers.ToDomain(document.GeoLocation),
            ExifMetadata = document.ExifMetadata?.ToDomain(),
            Width = document.Width,
            Height = document.Height,
            SizeInBytes = document.SizeInBytes,
            OwnerType = document.OwnerType,
            OwnerId = document.OwnerId,
            IsCurrent = document.IsCurrent,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            IsPublished = document.IsPublished,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ImageDocument ToDocument(this Image entity)
    {
        return new ImageDocument
        {
            Id = entity.Id,
            Category = entity.Category,
            Path = entity.Path,
            Description = entity.Description,
            AltTexts = CommonMongoMappers.ToDocuments(entity.AltTexts),
            Captions = CommonMongoMappers.ToDocuments(entity.Captions),
            Credits = CommonMongoMappers.ToDocuments(entity.Credits),
            TagIds = entity.TagIds,
            GeoLocation = CommonMongoMappers.ToDocument(entity.GeoLocation),
            ExifMetadata = entity.ExifMetadata?.ToDocument(),
            Width = entity.Width,
            Height = entity.Height,
            SizeInBytes = entity.SizeInBytes,
            OwnerType = entity.OwnerType,
            OwnerId = entity.OwnerId,
            IsCurrent = entity.IsCurrent,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            IsPublished = entity.IsPublished,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ImageExifMetadata ToDomain(this ImageExifMetadataDocument document)
    {
        return new ImageExifMetadata
        {
            CameraMaker = document.CameraMaker,
            CameraModel = document.CameraModel,
            TakenOnUtc = document.TakenOnUtc,
            Orientation = document.Orientation,
            FocalLength = document.FocalLength,
            Aperture = document.Aperture,
            ExposureTime = document.ExposureTime,
            Iso = document.Iso,
            RawGpsLatitude = document.RawGpsLatitude,
            RawGpsLongitude = document.RawGpsLongitude,
        };
    }

    public static ImageExifMetadataDocument ToDocument(this ImageExifMetadata entity)
    {
        return new ImageExifMetadataDocument
        {
            CameraMaker = entity.CameraMaker,
            CameraModel = entity.CameraModel,
            TakenOnUtc = entity.TakenOnUtc,
            Orientation = entity.Orientation,
            FocalLength = entity.FocalLength,
            Aperture = entity.Aperture,
            ExposureTime = entity.ExposureTime,
            Iso = entity.Iso,
            RawGpsLatitude = entity.RawGpsLatitude,
            RawGpsLongitude = entity.RawGpsLongitude,
        };
    }

    public static ImageTag ToDomain(this ImageTagDocument document)
    {
        ImageTag entity = new ImageTag
        {
            Id = document.Id,
            Slug = document.Slug,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsActive = document.IsActive,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static ImageTagDocument ToDocument(this ImageTag entity)
    {
        return new ImageTagDocument
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Labels = CommonMongoMappers.ToDocuments(entity.Labels),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static User ToDomain(this UserDocument document)
    {
        User entity = new User
        {
            Id = document.Id,
            FirstName = document.FirstName,
            LastName = document.LastName,
            Email = document.Email,
            HashedPassword = document.HashedPassword,
            IsActivated = document.IsActivated,
            IsBlocked = document.IsBlocked,
            PreferredLanguage = document.PreferredLanguage,
            AvatarUrl = document.AvatarUrl,
            Roles = document.Roles.ToList(),
            ExternalLogins = document.ExternalLogins.Select(ToDomain).ToList(),
            LastLoginUtc = document.LastLoginUtc,
            LastActivityUtc = document.LastActivityUtc,
            EmailConfirmationTokenHash = document.EmailConfirmationTokenHash,
            EmailConfirmationTokenExpiresAtUtc = document.EmailConfirmationTokenExpiresAtUtc,
            EmailConfirmationSentAtUtc = document.EmailConfirmationSentAtUtc,
            PasswordResetTokenHash = document.PasswordResetTokenHash,
            PasswordResetTokenExpiresAtUtc = document.PasswordResetTokenExpiresAtUtc,
            PasswordResetSentAtUtc = document.PasswordResetSentAtUtc,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static UserDocument ToDocument(this User entity)
    {
        return new UserDocument
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            HashedPassword = entity.HashedPassword,
            IsActivated = entity.IsActivated,
            IsBlocked = entity.IsBlocked,
            PreferredLanguage = entity.PreferredLanguage,
            AvatarUrl = entity.AvatarUrl,
            Roles = entity.Roles.ToList(),
            ExternalLogins = entity.ExternalLogins.Select(ToDocument).ToList(),
            LastLoginUtc = entity.LastLoginUtc,
            LastActivityUtc = entity.LastActivityUtc,
            EmailConfirmationTokenHash = entity.EmailConfirmationTokenHash,
            EmailConfirmationTokenExpiresAtUtc = entity.EmailConfirmationTokenExpiresAtUtc,
            EmailConfirmationSentAtUtc = entity.EmailConfirmationSentAtUtc,
            PasswordResetTokenHash = entity.PasswordResetTokenHash,
            PasswordResetTokenExpiresAtUtc = entity.PasswordResetTokenExpiresAtUtc,
            PasswordResetSentAtUtc = entity.PasswordResetSentAtUtc,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static ExternalLogin ToDomain(this ExternalLoginDocument document)
    {
        return new ExternalLogin
        {
            Provider = document.Provider,
            ProviderUserId = document.ProviderUserId,
            Email = document.Email,
            IsEmailVerified = document.IsEmailVerified,
            DisplayName = document.DisplayName,
            GivenName = document.GivenName,
            FamilyName = document.FamilyName,
            PictureUrl = document.PictureUrl,
            HostedDomain = document.HostedDomain,
            LinkedAtUtc = document.LinkedAtUtc,
            LastLoginAtUtc = document.LastLoginAtUtc,
        };
    }

    public static ExternalLoginDocument ToDocument(this ExternalLogin entity)
    {
        return new ExternalLoginDocument
        {
            Provider = entity.Provider,
            ProviderUserId = entity.ProviderUserId,
            Email = entity.Email,
            IsEmailVerified = entity.IsEmailVerified,
            DisplayName = entity.DisplayName,
            GivenName = entity.GivenName,
            FamilyName = entity.FamilyName,
            PictureUrl = entity.PictureUrl,
            HostedDomain = entity.HostedDomain,
            LinkedAtUtc = entity.LinkedAtUtc,
            LastLoginAtUtc = entity.LastLoginAtUtc,
        };
    }

    public static CaptainCoasterSettingsResult ToResult(this CaptainCoasterSettingsDocument document)
    {
        return new CaptainCoasterSettingsResult
        {
            IsEnabled = document.IsEnabled,
            DataDirectoryPath = document.DataDirectoryPath,
            HtmlDirectoryPath = document.HtmlDirectoryPath,
            UseOfflineMode = document.UseOfflineMode,
        };
    }

    public static CaptainCoasterSettingsDocument ToDocument(this CaptainCoasterSettingsResult result)
    {
        return new CaptainCoasterSettingsDocument
        {
            IsEnabled = result.IsEnabled,
            DataDirectoryPath = result.DataDirectoryPath,
            HtmlDirectoryPath = result.HtmlDirectoryPath,
            UseOfflineMode = result.UseOfflineMode,
        };
    }

    public static CaptainCoasterSessionResult ToResult(this CaptainCoasterSyncSessionDocument document)
    {
        return new CaptainCoasterSessionResult
        {
            SessionId = document.Id,
            Status = document.Status,
            ProgressPercentage = document.ProgressPercentage,
            Message = document.Message,
        };
    }

    public static AmusementPark.Application.Features.Search.Results.SearchHitResult ToSearchHit(this SearchItemDocument document)
    {
        return new AmusementPark.Application.Features.Search.Results.SearchHitResult
        {
            Id = string.IsNullOrWhiteSpace(document.OriginalId) ? document.Id : document.OriginalId,
            ResourceType = string.IsNullOrWhiteSpace(document.ResourceType) ? document.Category : document.ResourceType,
            Title = document.Title,
            Subtitle = document.Subtitle,
            Category = document.Category,
            Description = document.Description,
            Score = document.CompositeScore,
        };
    }
}
