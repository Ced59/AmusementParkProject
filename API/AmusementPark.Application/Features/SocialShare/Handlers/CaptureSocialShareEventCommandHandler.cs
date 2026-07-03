using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialShare.Commands;
using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.Application.Features.SocialShare.Ports;
using AmusementPark.Core.Domain.SocialShare;

namespace AmusementPark.Application.Features.SocialShare.Handlers;

public sealed class CaptureSocialShareEventCommandHandler
    : ICommandHandler<CaptureSocialShareEventCommand, ApplicationResult<SocialShareEventCaptureResult>>
{
    private const int MaximumUrlLength = 2048;
    private const int MaximumTargetIdLength = 128;
    private const int MaximumTargetTitleLength = 180;
    private const int MaximumUserIdLength = 128;

    private readonly ISocialShareEventRepository repository;
    private readonly IPublicSeoContextProvider publicSeoContextProvider;

    public CaptureSocialShareEventCommandHandler(
        ISocialShareEventRepository repository,
        IPublicSeoContextProvider publicSeoContextProvider)
    {
        this.repository = repository;
        this.publicSeoContextProvider = publicSeoContextProvider;
    }

    public async Task<ApplicationResult<SocialShareEventCaptureResult>> HandleAsync(
        CaptureSocialShareEventCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        SocialShareEventCapture capture = command.Capture;
        Dictionary<string, IReadOnlyCollection<string>> validationErrors = new Dictionary<string, IReadOnlyCollection<string>>();

        if (!TryParseEnumName(capture.TargetType, out SocialShareTargetType targetType))
        {
            validationErrors["targetType"] = new[] { "invalid" };
        }

        if (!TryParseEnumName(capture.Channel, out SocialShareChannel channel))
        {
            validationErrors["channel"] = new[] { "invalid" };
        }

        PublicSeoContext publicSeoContext = await this.publicSeoContextProvider.GetAsync(cancellationToken);
        string normalizedUrl = NormalizeUrl(capture.Url, publicSeoContext.PublicBaseUrl, validationErrors);

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SocialShareEventCaptureResult>.Failure(SocialShareApplicationErrors.InvalidEvent(validationErrors));
        }

        DateTime nowUtc = DateTime.UtcNow;
        string? userId = NormalizeOptionalText(capture.UserId, MaximumUserIdLength);
        SocialShareEvent shareEvent = new SocialShareEvent
        {
            Id = Guid.NewGuid().ToString("N"),
            OccurredAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            TargetType = targetType,
            TargetId = NormalizeOptionalText(capture.TargetId, MaximumTargetIdLength),
            TargetTitle = NormalizeOptionalText(capture.TargetTitle, MaximumTargetTitleLength),
            Url = normalizedUrl,
            LanguageCode = NormalizeLanguageCode(capture.LanguageCode),
            Channel = channel,
            VisitorKind = string.IsNullOrWhiteSpace(userId) ? SocialShareVisitorKind.Anonymous : SocialShareVisitorKind.Authenticated,
            UserId = userId,
        };

        SocialShareEvent created = await this.repository.CreateAsync(shareEvent, cancellationToken);
        return ApplicationResult<SocialShareEventCaptureResult>.Success(new SocialShareEventCaptureResult(true, created.OccurredAtUtc));
    }

    private static bool TryParseEnumName<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalizedValue = value.Trim();
        if (!Enum.GetNames<TEnum>().Any(name => string.Equals(name, normalizedValue, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return Enum.TryParse(normalizedValue, true, out parsed);
    }

    private static string NormalizeUrl(
        string? value,
        string publicBaseUrl,
        IDictionary<string, IReadOnlyCollection<string>> validationErrors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            validationErrors["url"] = new[] { "required" };
            return string.Empty;
        }

        string normalizedValue = value.Trim();
        if (normalizedValue.Length > MaximumUrlLength || ContainsControlCharacter(normalizedValue))
        {
            validationErrors["url"] = new[] { "invalid" };
            return string.Empty;
        }

        if (!Uri.TryCreate(normalizedValue, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            validationErrors["url"] = new[] { "invalid" };
            return string.Empty;
        }

        if (!IsPublicSiteUrl(uri, publicBaseUrl))
        {
            validationErrors["url"] = new[] { "invalid" };
            return string.Empty;
        }

        return uri.AbsoluteUri;
    }

    private static bool IsPublicSiteUrl(Uri uri, string publicBaseUrl)
    {
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out Uri? publicUri)
            || (publicUri.Scheme != Uri.UriSchemeHttps && publicUri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        return string.Equals(uri.Scheme, publicUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(uri.IdnHost, publicUri.IdnHost, StringComparison.OrdinalIgnoreCase)
            && uri.Port == publicUri.Port;
    }

    private static bool ContainsControlCharacter(string value)
    {
        return value.Any(static character => char.IsControl(character));
    }

    private static string? NormalizeOptionalText(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim();
        return normalizedValue.Length <= maximumLength ? normalizedValue : normalizedValue[..maximumLength];
    }

    private static string? NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        string normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        return normalizedLanguageCode.Length >= 2 ? normalizedLanguageCode[..2] : null;
    }
}
