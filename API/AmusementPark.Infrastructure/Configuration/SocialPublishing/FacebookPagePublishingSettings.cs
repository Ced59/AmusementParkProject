using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.SocialPublishing;

public sealed class FacebookPagePublishingSettings
{
    public const string SectionName = "SocialPublishing:Facebook";

    public bool Enabled { get; set; }

    public string ApiVersion { get; set; } = "v24.0";

    public string PageId { get; set; } = string.Empty;

    public string PageAccessToken { get; set; } = string.Empty;

    public string PageUrl { get; set; } = string.Empty;

    public int RequestTimeoutSeconds { get; set; } = 30;

    public bool WebhookEnabled { get; set; }

    public string AppSecret { get; set; } = string.Empty;

    public string WebhookVerifyToken { get; set; } = string.Empty;

    public bool IsConfigured()
    {
        return this.Enabled
            && !string.IsNullOrWhiteSpace(this.PageId)
            && !string.IsNullOrWhiteSpace(this.PageAccessToken)
            && Uri.TryCreate(this.PageUrl, UriKind.Absolute, out Uri? pageUri)
            && string.Equals(pageUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsWebhookConfigured()
    {
        return this.IsConfigured()
            && this.WebhookEnabled
            && !string.IsNullOrWhiteSpace(this.AppSecret)
            && !string.IsNullOrWhiteSpace(this.WebhookVerifyToken);
    }

    public static FacebookPagePublishingSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        FacebookPagePublishingSettings settings = configuration
            .GetSection(SectionName)
            .Get<FacebookPagePublishingSettings>() ?? new FacebookPagePublishingSettings();

        settings.ApiVersion = NormalizeApiVersion(settings.ApiVersion);
        settings.PageId = settings.PageId?.Trim() ?? string.Empty;
        settings.PageAccessToken = settings.PageAccessToken?.Trim() ?? string.Empty;
        settings.PageUrl = settings.PageUrl?.Trim() ?? string.Empty;
        settings.AppSecret = settings.AppSecret?.Trim() ?? string.Empty;
        settings.WebhookVerifyToken = settings.WebhookVerifyToken?.Trim() ?? string.Empty;
        settings.RequestTimeoutSeconds = Math.Clamp(settings.RequestTimeoutSeconds, 3, 60);
        return settings;
    }

    private static string NormalizeApiVersion(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        string[] versionParts = normalized.Length > 1
            ? normalized[1..].Split('.', StringSplitOptions.None)
            : Array.Empty<string>();
        if (versionParts.Length != 2
            || versionParts.Any(static part => part.Length == 0 || part.Any(static character => !char.IsDigit(character))))
        {
            return "v24.0";
        }

        return normalized;
    }
}
