using System;
using System.Linq;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.Parks.Logos;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de mapping HTTP pour la feature Images.
/// </summary>
internal static class ImagesHttpMappers
{
    public static ImageCategory ToDomain(this ImageCategoryDto value)
    {
        return value switch
        {
            ImageCategoryDto.AVATAR => ImageCategory.Avatar,
            ImageCategoryDto.PARK_LOGO => ImageCategory.ParkLogo,
            ImageCategoryDto.PARK => ImageCategory.Park,
            ImageCategoryDto.PARK_ITEM => ImageCategory.ParkItem,
            ImageCategoryDto.OPERATOR => ImageCategory.Operator,
            ImageCategoryDto.MANUFACTURER => ImageCategory.Manufacturer,
            ImageCategoryDto.FOUNDER => ImageCategory.Founder,
            ImageCategoryDto.VIDEO_THUMBNAIL => ImageCategory.VideoThumbnail,
            _ => ImageCategory.Park,
        };
    }

    public static ImageOwnerType ToDomain(this ImageOwnerTypeDto value)
    {
        return value switch
        {
            ImageOwnerTypeDto.PARK => ImageOwnerType.Park,
            ImageOwnerTypeDto.USER => ImageOwnerType.User,
            ImageOwnerTypeDto.PARK_ITEM => ImageOwnerType.ParkItem,
            ImageOwnerTypeDto.PARK_OPERATOR => ImageOwnerType.ParkOperator,
            ImageOwnerTypeDto.ATTRACTION_MANUFACTURER => ImageOwnerType.AttractionManufacturer,
            ImageOwnerTypeDto.PARK_FOUNDER => ImageOwnerType.ParkFounder,
            ImageOwnerTypeDto.VIDEO => ImageOwnerType.Video,
            _ => ImageOwnerType.None,
        };
    }

    public static ImageCategory? ToOptionalDomain(this ImageCategoryDto? value)
    {
        return value.HasValue ? value.Value.ToDomain() : null;
    }

    public static ImageOwnerType? ToOptionalDomain(this ImageOwnerTypeDto? value)
    {
        return value.HasValue ? value.Value.ToDomain() : null;
    }

    public static bool TryParseImageCategoryDto(string value, out ImageCategoryDto category)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            category = ImageCategoryDto.PARK;
            return false;
        }

        if (string.Equals(value.Trim(), "ATTRACTION", StringComparison.OrdinalIgnoreCase))
        {
            category = ImageCategoryDto.PARK_ITEM;
            return true;
        }

        return Enum.TryParse(value, true, out category);
    }

    public static bool TryParseImageOwnerTypeDto(string value, out ImageOwnerTypeDto ownerType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ownerType = ImageOwnerTypeDto.NONE;
            return false;
        }

        if (string.Equals(value.Trim(), "ATTRACTION", StringComparison.OrdinalIgnoreCase))
        {
            ownerType = ImageOwnerTypeDto.PARK_ITEM;
            return true;
        }

        return Enum.TryParse(value, true, out ownerType);
    }

    public static ImageBulkMetadataUpdate ToApplication(this BulkImageMetadataUpdateDto value)
    {
        return new ImageBulkMetadataUpdate(
            value.IsPublished,
            value.Category.ToOptionalDomain(),
            value.AddTagIds,
            value.RemoveTagIds);
    }

    public static ImageCategoryDto ToHttp(this ImageCategory value)
    {
        return value switch
        {
            ImageCategory.Avatar => ImageCategoryDto.AVATAR,
            ImageCategory.ParkLogo => ImageCategoryDto.PARK_LOGO,
            ImageCategory.Park => ImageCategoryDto.PARK,
            ImageCategory.ParkItem => ImageCategoryDto.PARK_ITEM,
            ImageCategory.Operator => ImageCategoryDto.OPERATOR,
            ImageCategory.Manufacturer => ImageCategoryDto.MANUFACTURER,
            ImageCategory.Founder => ImageCategoryDto.FOUNDER,
            ImageCategory.VideoThumbnail => ImageCategoryDto.VIDEO_THUMBNAIL,
            _ => ImageCategoryDto.PARK,
        };
    }

    public static ImageOwnerTypeDto ToHttp(this ImageOwnerType value)
    {
        return value switch
        {
            ImageOwnerType.Park => ImageOwnerTypeDto.PARK,
            ImageOwnerType.User => ImageOwnerTypeDto.USER,
            ImageOwnerType.ParkItem => ImageOwnerTypeDto.PARK_ITEM,
            ImageOwnerType.ParkOperator => ImageOwnerTypeDto.PARK_OPERATOR,
            ImageOwnerType.AttractionManufacturer => ImageOwnerTypeDto.ATTRACTION_MANUFACTURER,
            ImageOwnerType.ParkFounder => ImageOwnerTypeDto.PARK_FOUNDER,
            ImageOwnerType.Video => ImageOwnerTypeDto.VIDEO,
            _ => ImageOwnerTypeDto.NONE,
        };
    }

    public static ImageUploadRequest ToApplication(this ImageCreateDto value, FilePayload file)
    {
        return new ImageUploadRequest
        {
            Category = value.Category.ToDomain(),
            File = file,
            Description = value.Description,
            WithWatermark = value.WithWatermark,
        };
    }

    public static ImageMetadataUpdate ToApplication(this UpdateImageAssetRequest value, ImageCategory category)
    {
        return new ImageMetadataUpdate
        {
            Description = value.Description,
            GeoLocation = value.GeoLocation == null ? null : new GeoPointValue(value.GeoLocation.Latitude, value.GeoLocation.Longitude),
            AltTexts = value.AltTexts.Select(static item => new LocalizedTextValue(item.LanguageCode, item.Value)).ToList(),
            Captions = value.Captions.Select(static item => new LocalizedTextValue(item.LanguageCode, item.Value)).ToList(),
            Credits = value.Credits.Select(static item => new LocalizedTextValue(item.LanguageCode, item.Value)).ToList(),
            TagIds = value.TagIds.Distinct(StringComparer.Ordinal).ToList(),
            Category = category,
            IsPublished = value.IsPublished,
        };
    }

    public static ImageTagWriteModel ToApplication(this CreateImageTagRequest value)
    {
        return new ImageTagWriteModel
        {
            Slug = value.Slug,
            Labels = value.Labels.Select(static item => new LocalizedTextValue(item.LanguageCode, item.Value)).ToList(),
            Descriptions = value.Descriptions.Select(static item => new LocalizedTextValue(item.LanguageCode, item.Value)).ToList(),
            IsActive = true,
        };
    }

    public static ImageTagWriteModel ToApplication(this UpdateImageTagRequest value)
    {
        return new ImageTagWriteModel
        {
            Slug = value.Slug,
            Labels = value.Labels.Select(static item => new LocalizedTextValue(item.LanguageCode, item.Value)).ToList(),
            Descriptions = value.Descriptions.Select(static item => new LocalizedTextValue(item.LanguageCode, item.Value)).ToList(),
            IsActive = value.IsActive,
        };
    }

    public static ImageCreatedDto ToHttp(this UploadedImageResult value)
    {
        return new ImageCreatedDto
        {
            Id = value.Image.Id,
            SavedListFile = value.SavedFiles,
            Category = value.Image.Category.ToHttp(),
            Latitude = value.Image.GeoLocation?.Latitude,
            Longitude = value.Image.GeoLocation?.Longitude,
            Width = value.Image.Width,
            Height = value.Image.Height,
            SizeInBytes = value.Image.SizeInBytes,
        };
    }

    public static ImageDto ToHttp(this Image value)
    {
        return new ImageDto
        {
            Id = value.Id,
            Category = value.Category.ToHttp(),
            OwnerType = value.OwnerType.ToHttp(),
            OwnerId = value.OwnerId,
            Path = value.Path,
            Description = value.Description,
            IsCurrent = value.IsCurrent,
            IsPublished = value.IsPublished,
            Width = value.Width,
            Height = value.Height,
            SizeInBytes = value.SizeInBytes,
            OriginalFileName = value.OriginalFileName,
            ContentType = value.ContentType,
            GeoLocation = value.GeoLocation == null
                ? null
                : new ImageGeoLocationDto
                {
                    Latitude = value.GeoLocation.Latitude,
                    Longitude = value.GeoLocation.Longitude,
                },
            ExifMetadata = value.ExifMetadata == null
                ? null
                : new ImageExifMetadataDto
                {
                    CameraMaker = value.ExifMetadata.CameraMaker,
                    CameraModel = value.ExifMetadata.CameraModel,
                    TakenOnUtc = value.ExifMetadata.TakenOnUtc,
                    Orientation = value.ExifMetadata.Orientation,
                    FocalLength = value.ExifMetadata.FocalLength,
                    Aperture = value.ExifMetadata.Aperture,
                    ExposureTime = value.ExifMetadata.ExposureTime,
                    Iso = value.ExifMetadata.Iso,
                },
            AltTexts = value.AltTexts.ToHttp(),
            Captions = value.Captions.ToHttp(),
            Credits = value.Credits.ToHttp(),
            TagIds = value.TagIds.ToList(),
            CreatedAt = value.CreatedAtUtc,
            UpdatedAt = value.UpdatedAtUtc,
        };
    }

    public static ImageTagDto ToHttp(this ImageTag value)
    {
        return new ImageTagDto
        {
            Id = value.Id,
            Slug = value.Slug,
            Labels = value.Labels.ToHttp(),
            Descriptions = value.Descriptions.ToHttp(),
            IsActive = value.IsActive,
            CreatedAt = value.CreatedAtUtc,
            UpdatedAt = value.UpdatedAtUtc,
        };
    }

    public static ParkLogoDto ToParkLogoHttp(this Image value)
    {
        return new ParkLogoDto
        {
            Id = value.Id,
            ParkId = value.OwnerId ?? string.Empty,
            ImageId = value.Id,
            Description = value.Description,
            IsCurrent = value.IsCurrent,
            CreatedAt = value.CreatedAtUtc,
        };
    }
}
