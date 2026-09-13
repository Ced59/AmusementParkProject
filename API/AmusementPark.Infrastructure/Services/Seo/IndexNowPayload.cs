using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;

namespace AmusementPark.Infrastructure.Services.Seo;

internal sealed class IndexNowPayload
{
    [JsonPropertyName("host")]
    public string Host { get; init; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("keyLocation")]
    public string KeyLocation { get; init; } = string.Empty;

    [JsonPropertyName("urlList")]
    public IReadOnlyCollection<string> UrlList { get; init; } = Array.Empty<string>();
}
