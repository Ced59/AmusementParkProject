using System.Text.Json.Serialization;

namespace AmusementPark.Application.Ports;

public sealed class SsrPageCacheInvalidationRequest
{
    public const string RatingRankingPageGroup = "rating-rankings";

    [JsonPropertyName("all")]
    public bool All { get; init; }

    [JsonPropertyName("paths")]
    public IReadOnlyCollection<string> Paths { get; init; } = Array.Empty<string>();

    [JsonPropertyName("prefixes")]
    public IReadOnlyCollection<string> Prefixes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("pageGroups")]
    public IReadOnlyCollection<string> PageGroups { get; init; } = Array.Empty<string>();

    [JsonPropertyName("includeSeoDocuments")]
    public bool IncludeSeoDocuments { get; init; }

    [JsonPropertyName("allowStale")]
    public bool AllowStale { get; init; } = true;

    [JsonPropertyName("refresh")]
    public bool Refresh { get; init; } = true;

    public static SsrPageCacheInvalidationRequest AllCaches()
    {
        return new SsrPageCacheInvalidationRequest
        {
            All = true,
            IncludeSeoDocuments = true,
            AllowStale = false,
            Refresh = false,
        };
    }

    public static SsrPageCacheInvalidationRequest RatingRankingPages()
    {
        return new SsrPageCacheInvalidationRequest
        {
            All = false,
            PageGroups = new string[] { RatingRankingPageGroup },
            IncludeSeoDocuments = false,
            AllowStale = false,
            Refresh = false,
        };
    }
}
