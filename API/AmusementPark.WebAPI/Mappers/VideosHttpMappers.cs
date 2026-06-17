using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Videos;

namespace AmusementPark.WebAPI.Mappers;

internal static class VideosHttpMappers
{
    public static VideoHostingProvider ToDomain(this VideoHostingProviderDto value)
    {
        return value switch
        {
            VideoHostingProviderDto.YOUTUBE => VideoHostingProvider.YouTube,
            VideoHostingProviderDto.DAILYMOTION => VideoHostingProvider.Dailymotion,
            VideoHostingProviderDto.VIMEO => VideoHostingProvider.Vimeo,
            _ => VideoHostingProvider.Other,
        };
    }

    public static VideoOwnerType ToDomain(this VideoOwnerTypeDto value)
    {
        return value switch
        {
            VideoOwnerTypeDto.PARK => VideoOwnerType.Park,
            VideoOwnerTypeDto.PARK_ITEM => VideoOwnerType.ParkItem,
            _ => VideoOwnerType.None,
        };
    }

    public static VideoType ToDomain(this VideoTypeDto value)
    {
        return value switch
        {
            VideoTypeDto.ON_RIDE => VideoType.OnRide,
            VideoTypeDto.OFF_RIDE => VideoType.OffRide,
            VideoTypeDto.WALKTHROUGH => VideoType.Walkthrough,
            VideoTypeDto.ADVERTISEMENT => VideoType.Advertisement,
            VideoTypeDto.DOCUMENTARY => VideoType.Documentary,
            VideoTypeDto.REVIEW => VideoType.Review,
            VideoTypeDto.NEWS => VideoType.News,
            VideoTypeDto.EVENT => VideoType.Event,
            VideoTypeDto.INTERVIEW => VideoType.Interview,
            _ => VideoType.Other,
        };
    }

    public static VideoHostingProvider? ToOptionalDomain(this VideoHostingProviderDto? value)
    {
        return value.HasValue ? value.Value.ToDomain() : null;
    }

    public static VideoOwnerType? ToOptionalDomain(this VideoOwnerTypeDto? value)
    {
        return value.HasValue ? value.Value.ToDomain() : null;
    }

    public static VideoType? ToOptionalDomain(this VideoTypeDto? value)
    {
        return value.HasValue ? value.Value.ToDomain() : null;
    }

    public static VideoHostingProviderDto ToHttp(this VideoHostingProvider value)
    {
        return value switch
        {
            VideoHostingProvider.YouTube => VideoHostingProviderDto.YOUTUBE,
            VideoHostingProvider.Dailymotion => VideoHostingProviderDto.DAILYMOTION,
            VideoHostingProvider.Vimeo => VideoHostingProviderDto.VIMEO,
            _ => VideoHostingProviderDto.OTHER,
        };
    }

    public static VideoOwnerTypeDto ToHttp(this VideoOwnerType value)
    {
        return value switch
        {
            VideoOwnerType.Park => VideoOwnerTypeDto.PARK,
            VideoOwnerType.ParkItem => VideoOwnerTypeDto.PARK_ITEM,
            _ => VideoOwnerTypeDto.NONE,
        };
    }

    public static VideoTypeDto ToHttp(this VideoType value)
    {
        return value switch
        {
            VideoType.OnRide => VideoTypeDto.ON_RIDE,
            VideoType.OffRide => VideoTypeDto.OFF_RIDE,
            VideoType.Walkthrough => VideoTypeDto.WALKTHROUGH,
            VideoType.Advertisement => VideoTypeDto.ADVERTISEMENT,
            VideoType.Documentary => VideoTypeDto.DOCUMENTARY,
            VideoType.Review => VideoTypeDto.REVIEW,
            VideoType.News => VideoTypeDto.NEWS,
            VideoType.Event => VideoTypeDto.EVENT,
            VideoType.Interview => VideoTypeDto.INTERVIEW,
            _ => VideoTypeDto.OTHER,
        };
    }

    public static VideoWriteModel ToApplication(this VideoWriteDto value)
    {
        return new VideoWriteModel
        {
            OriginalUrl = value.OriginalUrl,
            OwnerType = value.OwnerType.ToDomain(),
            OwnerId = value.OwnerId,
            Type = value.Type.ToDomain(),
            Title = value.Title,
            Description = value.Description,
            CreatorName = value.CreatorName,
            CreatorUrl = value.CreatorUrl,
            ThumbnailUrl = value.ThumbnailUrl,
            Duration = value.DurationSeconds.HasValue ? TimeSpan.FromSeconds(value.DurationSeconds.Value) : null,
            PublishedAtUtc = value.PublishedAtUtc,
            LanguageCodes = value.LanguageCodes ?? new List<string>(),
            Titles = value.Titles.ToApplicationLocalizedTextValues(),
            Descriptions = value.Descriptions.ToApplicationLocalizedTextValues(),
            TagIds = value.TagIds ?? new List<string>(),
            IsPublished = value.IsPublished,
        };
    }

    public static VideoTagWriteModel ToApplication(this CreateVideoTagRequest value)
    {
        return new VideoTagWriteModel
        {
            Slug = value.Slug,
            Labels = value.Labels.ToApplicationLocalizedTextValues(),
            Descriptions = value.Descriptions.ToApplicationLocalizedTextValues(),
            IsActive = true,
        };
    }

    public static VideoTagWriteModel ToApplication(this UpdateVideoTagRequest value)
    {
        return new VideoTagWriteModel
        {
            Slug = value.Slug,
            Labels = value.Labels.ToApplicationLocalizedTextValues(),
            Descriptions = value.Descriptions.ToApplicationLocalizedTextValues(),
            IsActive = value.IsActive,
        };
    }

    public static VideoDto ToHttp(this Video value)
    {
        return new VideoDto
        {
            Id = value.Id,
            HostingProvider = value.HostingProvider.ToHttp(),
            OwnerType = value.OwnerType.ToHttp(),
            OwnerId = value.OwnerId,
            Type = value.Type.ToHttp(),
            OriginalUrl = value.OriginalUrl,
            CanonicalUrl = value.CanonicalUrl,
            EmbedUrl = value.EmbedUrl,
            ExternalId = value.ExternalId,
            Title = value.Title,
            Description = value.Description,
            CreatorName = value.CreatorName,
            CreatorUrl = value.CreatorUrl,
            ThumbnailUrl = value.ThumbnailUrl,
            ThumbnailImageId = value.ThumbnailImageId,
            DurationSeconds = value.Duration.HasValue ? checked((long)value.Duration.Value.TotalSeconds) : null,
            PublishedAtUtc = value.PublishedAtUtc,
            LanguageCodes = value.LanguageCodes.ToList(),
            Titles = value.Titles.ToHttp(),
            Descriptions = value.Descriptions.ToHttp(),
            TagIds = value.TagIds.ToList(),
            ExternalMetadata = new VideoExternalMetadataDto
            {
                Source = value.ExternalMetadata.Source,
                FetchedAtUtc = value.ExternalMetadata.FetchedAtUtc,
                ProviderTitle = value.ExternalMetadata.ProviderTitle,
                ProviderDescription = value.ExternalMetadata.ProviderDescription,
                ProviderChannelId = value.ExternalMetadata.ProviderChannelId,
                ProviderChannelUrl = value.ExternalMetadata.ProviderChannelUrl,
            },
            IsPublished = value.IsPublished,
            CreatedAt = value.CreatedAtUtc,
            UpdatedAt = value.UpdatedAtUtc,
        };
    }

    public static VideoTagDto ToHttp(this VideoTag value)
    {
        return new VideoTagDto
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

    public static ResolvedVideoMetadataDto ToHttp(this ResolvedVideoMetadata value)
    {
        return new ResolvedVideoMetadataDto
        {
            HostingProvider = value.HostingProvider.ToHttp(),
            OriginalUrl = value.OriginalUrl,
            CanonicalUrl = value.CanonicalUrl,
            EmbedUrl = value.EmbedUrl,
            ExternalId = value.ExternalId,
            Title = value.Title,
            Description = value.Description,
            CreatorName = value.CreatorName,
            CreatorUrl = value.CreatorUrl,
            ThumbnailUrl = value.ThumbnailUrl,
            DurationSeconds = value.Duration.HasValue ? checked((long)value.Duration.Value.TotalSeconds) : null,
            PublishedAtUtc = value.PublishedAtUtc,
            DetectedLanguageCode = value.DetectedLanguageCode,
            MetadataSource = value.MetadataSource,
            FetchedAtUtc = value.FetchedAtUtc,
            ProviderChannelId = value.ProviderChannelId,
            ProviderChannelUrl = value.ProviderChannelUrl,
        };
    }

    private static IReadOnlyCollection<LocalizedTextValue> ToApplicationLocalizedTextValues(this IEnumerable<LocalizedTextDto>? values)
    {
        if (values is null)
        {
            return Array.Empty<LocalizedTextValue>();
        }

        return values
            .Where(static value => value is not null)
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode) && !string.IsNullOrWhiteSpace(value.Value))
            .Select(static value => new LocalizedTextValue(value.LanguageCode.Trim().ToLowerInvariant(), value.Value!.Trim()))
            .ToList();
    }
}
