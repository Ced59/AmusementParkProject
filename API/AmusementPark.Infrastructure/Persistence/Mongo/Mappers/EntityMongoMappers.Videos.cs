using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static Video ToDomain(this VideoDocument document)
    {
        Video entity = new Video
        {
            Id = document.Id,
            HostingProvider = document.HostingProvider,
            OwnerType = document.OwnerType,
            OwnerId = document.OwnerId,
            Type = document.Type,
            OriginalUrl = document.OriginalUrl,
            CanonicalUrl = document.CanonicalUrl,
            EmbedUrl = document.EmbedUrl,
            ExternalId = document.ExternalId,
            Title = document.Title,
            Description = document.Description,
            CreatorName = document.CreatorName,
            CreatorUrl = document.CreatorUrl,
            ThumbnailUrl = document.ThumbnailUrl,
            ThumbnailImageId = document.ThumbnailImageId,
            Duration = document.DurationSeconds.HasValue ? TimeSpan.FromSeconds(document.DurationSeconds.Value) : null,
            PublishedAtUtc = document.PublishedAtUtc,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            TagIds = document.TagIds,
            ExternalMetadata = document.ExternalMetadata.ToDomain(),
            IsPublished = document.IsPublished,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static VideoDocument ToDocument(this Video entity)
    {
        return new VideoDocument
        {
            Id = entity.Id,
            HostingProvider = entity.HostingProvider,
            OwnerType = entity.OwnerType,
            OwnerId = entity.OwnerId,
            Type = entity.Type,
            OriginalUrl = entity.OriginalUrl,
            CanonicalUrl = entity.CanonicalUrl,
            EmbedUrl = entity.EmbedUrl,
            ExternalId = entity.ExternalId,
            Title = entity.Title,
            Description = entity.Description,
            CreatorName = entity.CreatorName,
            CreatorUrl = entity.CreatorUrl,
            ThumbnailUrl = entity.ThumbnailUrl,
            ThumbnailImageId = entity.ThumbnailImageId,
            DurationSeconds = entity.Duration.HasValue ? checked((long)entity.Duration.Value.TotalSeconds) : null,
            PublishedAtUtc = entity.PublishedAtUtc,
            Titles = CommonMongoMappers.ToDocuments(entity.Titles),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            TagIds = entity.TagIds,
            ExternalMetadata = entity.ExternalMetadata.ToDocument(),
            IsPublished = entity.IsPublished,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static VideoExternalMetadata ToDomain(this VideoExternalMetadataDocument document)
    {
        return new VideoExternalMetadata
        {
            Source = document.Source,
            FetchedAtUtc = document.FetchedAtUtc,
            ProviderTitle = document.ProviderTitle,
            ProviderDescription = document.ProviderDescription,
            ProviderChannelId = document.ProviderChannelId,
            ProviderChannelUrl = document.ProviderChannelUrl,
        };
    }

    public static VideoExternalMetadataDocument ToDocument(this VideoExternalMetadata entity)
    {
        return new VideoExternalMetadataDocument
        {
            Source = entity.Source,
            FetchedAtUtc = entity.FetchedAtUtc,
            ProviderTitle = entity.ProviderTitle,
            ProviderDescription = entity.ProviderDescription,
            ProviderChannelId = entity.ProviderChannelId,
            ProviderChannelUrl = entity.ProviderChannelUrl,
        };
    }

    public static VideoTag ToDomain(this VideoTagDocument document)
    {
        VideoTag entity = new VideoTag
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

    public static VideoTagDocument ToDocument(this VideoTag entity)
    {
        return new VideoTagDocument
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
