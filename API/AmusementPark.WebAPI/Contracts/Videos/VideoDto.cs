using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Videos;

public sealed class VideoDto
{
    public string Id { get; set; } = string.Empty;

    public VideoHostingProviderDto HostingProvider { get; set; }

    public VideoOwnerTypeDto OwnerType { get; set; }

    public string? OwnerId { get; set; }

    public VideoTypeDto Type { get; set; }

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

    public long? DurationSeconds { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public List<string> TagIds { get; set; } = new();

    public VideoExternalMetadataDto ExternalMetadata { get; set; } = new();

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class VideoExternalMetadataDto
{
    public string? Source { get; set; }

    public DateTime? FetchedAtUtc { get; set; }

    public string? ProviderTitle { get; set; }

    public string? ProviderDescription { get; set; }

    public string? ProviderChannelId { get; set; }

    public string? ProviderChannelUrl { get; set; }
}
