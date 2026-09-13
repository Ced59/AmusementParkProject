using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Videos;

public sealed class VideoExternalMetadataDto
{
    public string? Source { get; set; }

    public DateTime? FetchedAtUtc { get; set; }

    public string? ProviderTitle { get; set; }

    public string? ProviderDescription { get; set; }

    public string? ProviderChannelId { get; set; }

    public string? ProviderChannelUrl { get; set; }

    public long? ProviderViewCount { get; set; }
}
