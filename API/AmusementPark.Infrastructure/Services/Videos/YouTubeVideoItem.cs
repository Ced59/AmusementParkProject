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

internal sealed class YouTubeVideoItem
{
    [JsonPropertyName("snippet")]
    public YouTubeSnippet? Snippet { get; init; }

    [JsonPropertyName("contentDetails")]
    public YouTubeContentDetails? ContentDetails { get; init; }

    [JsonPropertyName("statistics")]
    public YouTubeStatistics? Statistics { get; init; }
}
