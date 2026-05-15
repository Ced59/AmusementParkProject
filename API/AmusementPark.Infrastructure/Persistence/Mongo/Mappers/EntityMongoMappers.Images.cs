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
internal static partial class EntityMongoMappers
{
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
}
