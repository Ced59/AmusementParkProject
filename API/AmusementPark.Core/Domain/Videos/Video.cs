using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Videos;

public sealed class Video : AuditableEntity
{
    public VideoHostingProvider HostingProvider { get; set; } = VideoHostingProvider.Other;

    public VideoOwnerType OwnerType { get; set; } = VideoOwnerType.None;

    public string? OwnerId { get; set; }

    public VideoType Type { get; set; } = VideoType.Other;

    public string OriginalUrl { get; set; } = string.Empty;

    public string CanonicalUrl { get; set; } = string.Empty;

    public string? EmbedUrl { get; set; }

    public string? ExternalId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? CreatorName { get; set; }

    public string? CreatorUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? ThumbnailImageId { get; set; }

    public TimeSpan? Duration { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public List<string> LanguageCodes { get; set; } = new();

    public List<LocalizedText> Titles { get; set; } = new();

    public List<LocalizedText> Descriptions { get; set; } = new();

    public List<string> TagIds { get; set; } = new();

    public VideoExternalMetadata ExternalMetadata { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}
