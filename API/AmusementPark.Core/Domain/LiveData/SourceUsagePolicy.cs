namespace AmusementPark.Core.Domain.LiveData;

public sealed class SourceUsagePolicy
{
    private const int MaximumVersionLength = 100;
    private const int MaximumAttributionKeyLength = 200;

    public SourceUsagePolicy(
        string version,
        string termsUrl,
        bool commercialUseAllowed,
        bool historicalStorageAllowed,
        bool redistributionAllowed,
        bool attributionRequired,
        string? attributionTemplateKey,
        DateTime reviewedAtUtc)
    {
        string normalizedVersion = NormalizeRequired(
            version,
            MaximumVersionLength,
            nameof(version));
        string normalizedTermsUrl = NormalizeHttpsUri(termsUrl, nameof(termsUrl));
        string? normalizedAttributionTemplateKey = NormalizeOptional(
            attributionTemplateKey,
            MaximumAttributionKeyLength,
            nameof(attributionTemplateKey));
        EnsureUtc(reviewedAtUtc, nameof(reviewedAtUtc));
        if (attributionRequired && normalizedAttributionTemplateKey is null)
        {
            throw Invalid(
                LiveDataErrorCodes.MissingAttribution,
                "A source that requires attribution must define an attribution template key.",
                nameof(attributionTemplateKey));
        }

        this.Version = normalizedVersion;
        this.TermsUrl = normalizedTermsUrl;
        this.CommercialUseAllowed = commercialUseAllowed;
        this.HistoricalStorageAllowed = historicalStorageAllowed;
        this.RedistributionAllowed = redistributionAllowed;
        this.AttributionRequired = attributionRequired;
        this.AttributionTemplateKey = normalizedAttributionTemplateKey;
        this.ReviewedAtUtc = reviewedAtUtc;
    }

    public string Version { get; }

    public string TermsUrl { get; }

    public bool CommercialUseAllowed { get; }

    public bool HistoricalStorageAllowed { get; }

    public bool RedistributionAllowed { get; }

    public bool AttributionRequired { get; }

    public string? AttributionTemplateKey { get; }

    public DateTime ReviewedAtUtc { get; }

    private static string NormalizeHttpsUri(string value, string parameterName)
    {
        string normalizedValue = NormalizeRequired(value, 2000, parameterName);
        if (!Uri.TryCreate(normalizedValue, UriKind.Absolute, out Uri? uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidUri,
                "A live data source policy requires an absolute HTTPS terms URL.",
                parameterName);
        }

        return uri.AbsoluteUri;
    }

    private static string NormalizeRequired(
        string? value,
        int maximumLength,
        string parameterName)
    {
        return NormalizeOptional(value, maximumLength, parameterName)
            ?? throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "A live data source policy field is required.",
                parameterName);
    }

    private static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string parameterName)
    {
        string? normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalizedValue is not null
            && (normalizedValue.Length > maximumLength || normalizedValue.Any(char.IsControl)))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "A live data source policy field is invalid.",
                parameterName);
        }

        return normalizedValue;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "Live data source policy timestamps must be expressed in UTC.",
                parameterName);
        }
    }

    private static LiveDataValidationException Invalid(
        string code,
        string message,
        string? parameterName = null)
    {
        return new LiveDataValidationException(code, message, parameterName);
    }
}
