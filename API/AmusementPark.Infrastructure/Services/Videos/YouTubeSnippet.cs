using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Configuration.Videos;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Videos;

internal sealed class YouTubeSnippet
{
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; init; }

    [JsonPropertyName("channelId")]
    public string? ChannelId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("thumbnails")]
    public Dictionary<string, YouTubeThumbnail>? Thumbnails { get; init; }

    [JsonPropertyName("channelTitle")]
    public string? ChannelTitle { get; init; }

    [JsonPropertyName("defaultLanguage")]
    public string? DefaultLanguage { get; init; }

    [JsonPropertyName("defaultAudioLanguage")]
    public string? DefaultAudioLanguage { get; init; }
}
