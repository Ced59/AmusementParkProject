namespace AmusementPark.Core.Domain.FactualEvents;

public sealed record SourceReference
{
    public const int MaximumPublisherNameLength = 200;
    public const int MaximumTitleLength = 300;
    public const int MaximumUrlLength = 2048;

    public SourceReference(
        SourceReferenceType type,
        string publisherName,
        string title,
        string url,
        DateTime publishedAtUtc)
    {
        if (!Enum.IsDefined(type))
        {
            throw InvalidSource("The factual source type is invalid.");
        }

        string normalizedPublisherName = NormalizeBounded(
            publisherName,
            nameof(publisherName),
            MaximumPublisherNameLength);
        string normalizedTitle = NormalizeBounded(title, nameof(title), MaximumTitleLength);
        string normalizedUrl = NormalizeBounded(url, nameof(url), MaximumUrlLength);
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? parsedUrl)
            || (parsedUrl.Scheme != Uri.UriSchemeHttps && parsedUrl.Scheme != Uri.UriSchemeHttp))
        {
            throw InvalidSource("The factual source URL must be an absolute HTTP or HTTPS URL.");
        }

        string canonicalUrl = parsedUrl.AbsoluteUri;
        if (canonicalUrl.Length > MaximumUrlLength)
        {
            throw InvalidSource(
                $"The canonical factual source URL cannot exceed {MaximumUrlLength} characters.");
        }

        EnsureUtc(publishedAtUtc);
        this.Type = type;
        this.PublisherName = normalizedPublisherName;
        this.Title = normalizedTitle;
        this.Url = canonicalUrl;
        this.PublishedAtUtc = publishedAtUtc;
    }

    public SourceReferenceType Type { get; }

    public string PublisherName { get; }

    public string Title { get; }

    public string Url { get; }

    public DateTime PublishedAtUtc { get; }

    private static string NormalizeBounded(string value, string parameterName, int maximumLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0
            || normalized.Length > maximumLength
            || (value is not null && value.Any(char.IsControl)))
        {
            throw InvalidSource(
                $"The factual source {parameterName} must contain between 1 and {maximumLength} valid characters.");
        }

        return normalized;
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw InvalidSource("The factual source publication timestamp must be UTC.");
        }
    }

    private static FactualEventValidationException InvalidSource(string message)
    {
        return new FactualEventValidationException(
            FactualEventErrorCodes.InvalidSource,
            message);
    }
}
