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

public sealed class ExternalVideoMetadataProvider : IVideoMetadataProvider
{
    public const string HttpClientName = "video-metadata";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory httpClientFactory;
    private readonly VideoMetadataSettings settings;
    private readonly ILogger<ExternalVideoMetadataProvider> logger;

    public ExternalVideoMetadataProvider(
        IHttpClientFactory httpClientFactory,
        VideoMetadataSettings settings,
        ILogger<ExternalVideoMetadataProvider> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<ResolvedVideoMetadata?> ResolveAsync(string url, CancellationToken cancellationToken)
    {
        if (!TryBuildReference(url, out VideoUrlReference? reference) || reference is null)
        {
            return null;
        }

        ResolvedVideoMetadata baseMetadata = ToBaseMetadata(reference);

        if (reference.HostingProvider == VideoHostingProvider.YouTube)
        {
            ResolvedVideoMetadata? apiMetadata = await this.TryResolveYouTubeDataApiAsync(reference, cancellationToken);
            if (apiMetadata is not null)
            {
                return apiMetadata;
            }

            return await this.TryResolveOEmbedAsync(reference, this.settings.YouTubeOEmbedBaseUrl, "youtube-oembed", cancellationToken) ?? baseMetadata;
        }

        if (reference.HostingProvider == VideoHostingProvider.Dailymotion)
        {
            return await this.TryResolveOEmbedAsync(reference, this.settings.DailymotionOEmbedBaseUrl, "dailymotion-oembed", cancellationToken) ?? baseMetadata;
        }

        return baseMetadata;
    }

    private async Task<ResolvedVideoMetadata?> TryResolveYouTubeDataApiAsync(VideoUrlReference reference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(this.settings.YouTubeApiKey) || string.IsNullOrWhiteSpace(reference.ExternalId))
        {
            return null;
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
            string requestUrl = $"{this.settings.YouTubeApiBaseUrl}?part=snippet,contentDetails&id={Uri.EscapeDataString(reference.ExternalId)}&key={Uri.EscapeDataString(this.settings.YouTubeApiKey)}";
            YouTubeVideoListResponse? response = await client.GetFromJsonAsync<YouTubeVideoListResponse>(requestUrl, JsonOptions, cancellationToken);
            YouTubeVideoItem? item = response?.Items?.FirstOrDefault();
            if (item is null)
            {
                return null;
            }

            YouTubeSnippet? snippet = item.Snippet;
            string? thumbnailUrl = SelectBestThumbnailUrl(snippet?.Thumbnails);

            return new ResolvedVideoMetadata
            {
                HostingProvider = reference.HostingProvider,
                OriginalUrl = reference.OriginalUrl,
                CanonicalUrl = reference.CanonicalUrl,
                EmbedUrl = reference.EmbedUrl,
                ExternalId = reference.ExternalId,
                Title = snippet?.Title,
                Description = snippet?.Description,
                CreatorName = snippet?.ChannelTitle,
                CreatorUrl = string.IsNullOrWhiteSpace(snippet?.ChannelId) ? null : $"https://www.youtube.com/channel/{snippet.ChannelId}",
                ThumbnailUrl = thumbnailUrl,
                Duration = TryParseIsoDuration(item.ContentDetails?.Duration),
                PublishedAtUtc = snippet?.PublishedAt,
                MetadataSource = "youtube-data-api",
                FetchedAtUtc = DateTime.UtcNow,
                ProviderChannelId = snippet?.ChannelId,
                ProviderChannelUrl = string.IsNullOrWhiteSpace(snippet?.ChannelId) ? null : $"https://www.youtube.com/channel/{snippet.ChannelId}",
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "YouTube metadata resolution failed for video {VideoId}.", reference.ExternalId);
            return null;
        }
    }

    private async Task<ResolvedVideoMetadata?> TryResolveOEmbedAsync(
        VideoUrlReference reference,
        string endpoint,
        string source,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
            string requestUrl = $"{endpoint}?url={Uri.EscapeDataString(reference.CanonicalUrl)}&format=json";
            OEmbedResponse? response = await client.GetFromJsonAsync<OEmbedResponse>(requestUrl, JsonOptions, cancellationToken);
            if (response is null)
            {
                return null;
            }

            return new ResolvedVideoMetadata
            {
                HostingProvider = reference.HostingProvider,
                OriginalUrl = reference.OriginalUrl,
                CanonicalUrl = reference.CanonicalUrl,
                EmbedUrl = reference.EmbedUrl,
                ExternalId = reference.ExternalId,
                Title = response.Title,
                CreatorName = response.AuthorName,
                CreatorUrl = response.AuthorUrl,
                ThumbnailUrl = response.ThumbnailUrl,
                MetadataSource = source,
                FetchedAtUtc = DateTime.UtcNow,
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "OEmbed metadata resolution failed for {Provider} video {VideoId}.", reference.HostingProvider, reference.ExternalId);
            return null;
        }
    }

    private static bool TryBuildReference(string url, out VideoUrlReference? reference)
    {
        reference = null;
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        string host = uri.Host.ToLowerInvariant();
        if (TryBuildYouTubeReference(uri, host, out reference))
        {
            return true;
        }

        if (TryBuildDailymotionReference(uri, host, out reference))
        {
            return true;
        }

        if (TryBuildVimeoReference(uri, host, out reference))
        {
            return true;
        }

        reference = new VideoUrlReference(
            VideoHostingProvider.Other,
            uri.AbsoluteUri,
            uri.AbsoluteUri,
            null,
            null);
        return true;
    }

    private static bool TryBuildYouTubeReference(Uri uri, string host, out VideoUrlReference? reference)
    {
        reference = null;
        bool isYouTubeHost = host == "youtu.be" ||
                             host == "youtube.com" ||
                             host == "www.youtube.com" ||
                             host == "m.youtube.com" ||
                             host == "music.youtube.com";
        if (!isYouTubeHost)
        {
            return false;
        }

        string? videoId = null;
        if (host == "youtu.be")
        {
            videoId = FirstPathSegment(uri);
        }
        else if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
        {
            videoId = ReadQueryValue(uri.Query, "v");
        }
        else if (uri.AbsolutePath.StartsWith("/embed/", StringComparison.OrdinalIgnoreCase) ||
                 uri.AbsolutePath.StartsWith("/shorts/", StringComparison.OrdinalIgnoreCase) ||
                 uri.AbsolutePath.StartsWith("/live/", StringComparison.OrdinalIgnoreCase))
        {
            videoId = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(videoId))
        {
            return false;
        }

        videoId = videoId.Trim();
        string canonicalUrl = $"https://www.youtube.com/watch?v={Uri.EscapeDataString(videoId)}";
        string embedUrl = $"https://www.youtube.com/embed/{Uri.EscapeDataString(videoId)}";
        reference = new VideoUrlReference(VideoHostingProvider.YouTube, uri.AbsoluteUri, canonicalUrl, embedUrl, videoId);
        return true;
    }

    private static bool TryBuildDailymotionReference(Uri uri, string host, out VideoUrlReference? reference)
    {
        reference = null;
        bool isDailymotionHost = host == "dailymotion.com" || host == "www.dailymotion.com" || host == "dai.ly";
        if (!isDailymotionHost)
        {
            return false;
        }

        string? videoId = host == "dai.ly"
            ? FirstPathSegment(uri)
            : uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .SkipWhile(static segment => !string.Equals(segment, "video", StringComparison.OrdinalIgnoreCase))
                .Skip(1)
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(videoId))
        {
            return false;
        }

        int underscoreIndex = videoId.IndexOf('_', StringComparison.Ordinal);
        if (underscoreIndex > 0)
        {
            videoId = videoId[..underscoreIndex];
        }

        string canonicalUrl = $"https://www.dailymotion.com/video/{Uri.EscapeDataString(videoId)}";
        string embedUrl = $"https://www.dailymotion.com/embed/video/{Uri.EscapeDataString(videoId)}";
        reference = new VideoUrlReference(VideoHostingProvider.Dailymotion, uri.AbsoluteUri, canonicalUrl, embedUrl, videoId);
        return true;
    }

    private static bool TryBuildVimeoReference(Uri uri, string host, out VideoUrlReference? reference)
    {
        reference = null;
        bool isVimeoHost = host == "vimeo.com" || host == "www.vimeo.com" || host == "player.vimeo.com";
        if (!isVimeoHost)
        {
            return false;
        }

        string? videoId = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(videoId) || !videoId.All(char.IsDigit))
        {
            return false;
        }

        string canonicalUrl = $"https://vimeo.com/{Uri.EscapeDataString(videoId)}";
        string embedUrl = $"https://player.vimeo.com/video/{Uri.EscapeDataString(videoId)}";
        reference = new VideoUrlReference(VideoHostingProvider.Vimeo, uri.AbsoluteUri, canonicalUrl, embedUrl, videoId);
        return true;
    }

    private static ResolvedVideoMetadata ToBaseMetadata(VideoUrlReference reference)
    {
        return new ResolvedVideoMetadata
        {
            HostingProvider = reference.HostingProvider,
            OriginalUrl = reference.OriginalUrl,
            CanonicalUrl = reference.CanonicalUrl,
            EmbedUrl = reference.EmbedUrl,
            ExternalId = reference.ExternalId,
            MetadataSource = "url",
            FetchedAtUtc = DateTime.UtcNow,
        };
    }

    private static string? FirstPathSegment(Uri uri)
    {
        return uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    private static string? ReadQueryValue(string query, string key)
    {
        string normalizedQuery = query.StartsWith('?') ? query[1..] : query;
        foreach (string part in normalizedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            if (pair.Length != 2)
            {
                continue;
            }

            string pairKey = Uri.UnescapeDataString(pair[0]);
            if (string.Equals(pairKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }

    private static TimeSpan? TryParseIsoDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? SelectBestThumbnailUrl(IReadOnlyDictionary<string, YouTubeThumbnail>? thumbnails)
    {
        return thumbnails?
            .Values
            .Where(static thumbnail => !string.IsNullOrWhiteSpace(thumbnail.Url))
            .OrderByDescending(static thumbnail => (thumbnail.Width ?? 0) * (thumbnail.Height ?? 0))
            .Select(static thumbnail => thumbnail.Url)
            .FirstOrDefault();
    }

    private sealed record VideoUrlReference(
        VideoHostingProvider HostingProvider,
        string OriginalUrl,
        string CanonicalUrl,
        string? EmbedUrl,
        string? ExternalId);

    private sealed class OEmbedResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("author_name")]
        public string? AuthorName { get; init; }

        [JsonPropertyName("author_url")]
        public string? AuthorUrl { get; init; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; init; }
    }

    private sealed class YouTubeVideoListResponse
    {
        [JsonPropertyName("items")]
        public List<YouTubeVideoItem>? Items { get; init; }
    }

    private sealed class YouTubeVideoItem
    {
        [JsonPropertyName("snippet")]
        public YouTubeSnippet? Snippet { get; init; }

        [JsonPropertyName("contentDetails")]
        public YouTubeContentDetails? ContentDetails { get; init; }
    }

    private sealed class YouTubeSnippet
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
    }

    private sealed class YouTubeContentDetails
    {
        [JsonPropertyName("duration")]
        public string? Duration { get; init; }
    }

    private sealed class YouTubeThumbnail
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("width")]
        public int? Width { get; init; }

        [JsonPropertyName("height")]
        public int? Height { get; init; }
    }
}
