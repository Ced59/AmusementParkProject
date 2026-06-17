namespace AmusementPark.Core.Domain.Videos;

public sealed class VideoExternalMetadata
{
    public string? Source { get; set; }

    public DateTime? FetchedAtUtc { get; set; }

    public string? ProviderTitle { get; set; }

    public string? ProviderDescription { get; set; }

    public string? ProviderChannelId { get; set; }

    public string? ProviderChannelUrl { get; set; }
}
