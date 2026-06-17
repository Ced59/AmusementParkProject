using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Videos.Handlers;

internal static class VideoWriteModelMapper
{
    public static Video ToDomain(VideoWriteModel writeModel, ResolvedVideoMetadata metadata)
    {
        string title = FirstNotBlank(writeModel.Title, metadata.Title, metadata.ExternalId, metadata.CanonicalUrl);
        string? description = FirstNullableNotBlank(writeModel.Description, metadata.Description);
        string? creatorName = FirstNullableNotBlank(writeModel.CreatorName, metadata.CreatorName);
        string? creatorUrl = FirstNullableNotBlank(writeModel.CreatorUrl, metadata.CreatorUrl);
        string? thumbnailUrl = FirstNullableNotBlank(writeModel.ThumbnailUrl, metadata.ThumbnailUrl);

        return new Video
        {
            HostingProvider = metadata.HostingProvider,
            OwnerType = writeModel.OwnerType,
            OwnerId = writeModel.OwnerId.Trim(),
            Type = writeModel.Type,
            OriginalUrl = metadata.OriginalUrl,
            CanonicalUrl = metadata.CanonicalUrl,
            EmbedUrl = metadata.EmbedUrl,
            ExternalId = metadata.ExternalId,
            Title = title,
            Description = description,
            CreatorName = creatorName,
            CreatorUrl = creatorUrl,
            ThumbnailUrl = thumbnailUrl,
            Duration = writeModel.Duration ?? metadata.Duration,
            PublishedAtUtc = writeModel.PublishedAtUtc ?? metadata.PublishedAtUtc,
            Titles = ToLocalizedTexts(writeModel.Titles),
            Descriptions = ToLocalizedTexts(writeModel.Descriptions),
            TagIds = NormalizeTagIds(writeModel.TagIds),
            IsPublished = writeModel.IsPublished,
            ExternalMetadata = new VideoExternalMetadata
            {
                Source = metadata.MetadataSource,
                FetchedAtUtc = metadata.FetchedAtUtc,
                ProviderTitle = metadata.Title,
                ProviderDescription = metadata.Description,
                ProviderChannelId = metadata.ProviderChannelId,
                ProviderChannelUrl = metadata.ProviderChannelUrl,
            },
        };
    }

    public static bool HasValidOwner(VideoWriteModel writeModel)
    {
        return (writeModel.OwnerType == VideoOwnerType.Park || writeModel.OwnerType == VideoOwnerType.Attraction) &&
               !string.IsNullOrWhiteSpace(writeModel.OwnerId);
    }

    private static List<LocalizedText> ToLocalizedTexts(IReadOnlyCollection<LocalizedTextValue> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode) && !string.IsNullOrWhiteSpace(value.Value))
            .Select(static value => new LocalizedText(value.LanguageCode.Trim().ToLowerInvariant(), value.Value.Trim()))
            .ToList();
    }

    private static List<string> NormalizeTagIds(IReadOnlyCollection<string> tagIds)
    {
        return tagIds
            .Where(static tagId => !string.IsNullOrWhiteSpace(tagId))
            .Select(static tagId => tagId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string FirstNotBlank(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "Video";
    }

    private static string? FirstNullableNotBlank(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
