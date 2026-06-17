using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Contracts;

public sealed class ResolvedVideoMetadata
{
    public VideoHostingProvider HostingProvider { get; init; } = VideoHostingProvider.Other;

    public string OriginalUrl { get; init; } = string.Empty;

    public string CanonicalUrl { get; init; } = string.Empty;

    public string? EmbedUrl { get; init; }

    public string? ExternalId { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? CreatorName { get; init; }

    public string? CreatorUrl { get; init; }

    public string? ThumbnailUrl { get; init; }

    public TimeSpan? Duration { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public string? MetadataSource { get; init; }

    public DateTime? FetchedAtUtc { get; init; }

    public string? ProviderChannelId { get; init; }

    public string? ProviderChannelUrl { get; init; }
}
