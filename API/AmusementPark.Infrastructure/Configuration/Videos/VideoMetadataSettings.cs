using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.Videos;

public sealed class VideoMetadataSettings
{
    public const string SectionName = "VideoMetadata";

    public string YouTubeApiKey { get; set; } = string.Empty;

    public string YouTubeApiBaseUrl { get; set; } = "https://www.googleapis.com/youtube/v3/videos";

    public string YouTubeOEmbedBaseUrl { get; set; } = "https://www.youtube.com/oembed";

    public string DailymotionOEmbedBaseUrl { get; set; } = "https://www.dailymotion.com/services/oembed";

    public int RequestTimeoutSeconds { get; set; } = 5;

    public int ThumbnailMaxBytes { get; set; } = 5 * 1024 * 1024;

    public static VideoMetadataSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        VideoMetadataSettings settings = configuration.GetSection(SectionName).Get<VideoMetadataSettings>() ?? new VideoMetadataSettings();
        if (settings.RequestTimeoutSeconds <= 0)
        {
            settings.RequestTimeoutSeconds = 5;
        }

        if (settings.ThumbnailMaxBytes <= 0)
        {
            settings.ThumbnailMaxBytes = 5 * 1024 * 1024;
        }

        if (string.IsNullOrWhiteSpace(settings.YouTubeApiBaseUrl))
        {
            settings.YouTubeApiBaseUrl = "https://www.googleapis.com/youtube/v3/videos";
        }

        if (string.IsNullOrWhiteSpace(settings.YouTubeOEmbedBaseUrl))
        {
            settings.YouTubeOEmbedBaseUrl = "https://www.youtube.com/oembed";
        }

        if (string.IsNullOrWhiteSpace(settings.DailymotionOEmbedBaseUrl))
        {
            settings.DailymotionOEmbedBaseUrl = "https://www.dailymotion.com/services/oembed";
        }

        return settings;
    }
}
