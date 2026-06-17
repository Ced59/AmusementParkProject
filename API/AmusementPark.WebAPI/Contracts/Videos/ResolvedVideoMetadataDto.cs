namespace AmusementPark.WebAPI.Contracts.Videos;

public sealed class ResolvedVideoMetadataDto
{
    public VideoHostingProviderDto HostingProvider { get; set; }

    public string OriginalUrl { get; set; } = string.Empty;

    public string CanonicalUrl { get; set; } = string.Empty;

    public string? EmbedUrl { get; set; }

    public string? ExternalId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? CreatorName { get; set; }

    public string? CreatorUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public long? DurationSeconds { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public string? MetadataSource { get; set; }

    public DateTime? FetchedAtUtc { get; set; }

    public string? ProviderChannelId { get; set; }

    public string? ProviderChannelUrl { get; set; }
}
