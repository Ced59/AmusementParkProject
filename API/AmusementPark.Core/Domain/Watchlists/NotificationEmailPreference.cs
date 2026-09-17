using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

/// <summary>
/// Consentement global requis en plus des canaux choisis sur chaque surveillance.
/// </summary>
public sealed class NotificationEmailPreference
{
    public const string CurrentConsentTextVersion = "watch-email-consent-v1";

    private NotificationEmailPreference(
        string userId,
        bool isEnabled,
        string? consentTextVersion,
        string? consentLocale,
        DateTime? consentGrantedAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        ValidateTimestamps(
            isEnabled,
            consentGrantedAtUtc,
            revokedAtUtc,
            createdAtUtc,
            updatedAtUtc);
        if (version < 1)
        {
            throw CreateValidationException(
                NotificationEmailPreferenceErrorCodes.InvalidVersion,
                "The notification email preference version must be positive.");
        }

        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.IsEnabled = isEnabled;
        this.ConsentTextVersion = NormalizeConsentValue(
            consentTextVersion,
            isEnabled,
            nameof(consentTextVersion));
        this.ConsentLocale = NormalizeConsentValue(
            consentLocale,
            isEnabled,
            nameof(consentLocale));
        this.ConsentGrantedAtUtc = consentGrantedAtUtc;
        this.RevokedAtUtc = revokedAtUtc;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public string UserId { get; }

    public bool IsEnabled { get; private set; }

    public string? ConsentTextVersion { get; private set; }

    public string? ConsentLocale { get; private set; }

    public DateTime? ConsentGrantedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static NotificationEmailPreference CreateConsented(
        string userId,
        string consentTextVersion,
        string consentLocale,
        DateTime nowUtc)
    {
        return new NotificationEmailPreference(
            userId,
            true,
            consentTextVersion,
            consentLocale,
            nowUtc,
            null,
            nowUtc,
            nowUtc,
            1);
    }

    public static NotificationEmailPreference Restore(
        string userId,
        bool isEnabled,
        string? consentTextVersion,
        string? consentLocale,
        DateTime? consentGrantedAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        return new NotificationEmailPreference(
            userId,
            isEnabled,
            consentTextVersion,
            consentLocale,
            consentGrantedAtUtc,
            revokedAtUtc,
            createdAtUtc,
            updatedAtUtc,
            version);
    }

    public void GrantConsent(
        string consentTextVersion,
        string consentLocale,
        DateTime nowUtc)
    {
        EnsureMutationTimestamp(nowUtc);
        string normalizedTextVersion = NormalizeConsentValue(
            consentTextVersion,
            true,
            nameof(consentTextVersion))!;
        string normalizedLocale = NormalizeConsentValue(
            consentLocale,
            true,
            nameof(consentLocale))!;
        if (this.IsEnabled
            && string.Equals(this.ConsentTextVersion, normalizedTextVersion, StringComparison.Ordinal)
            && string.Equals(this.ConsentLocale, normalizedLocale, StringComparison.Ordinal))
        {
            return;
        }

        this.PrepareMutation();
        this.IsEnabled = true;
        this.ConsentTextVersion = normalizedTextVersion;
        this.ConsentLocale = normalizedLocale;
        this.ConsentGrantedAtUtc = nowUtc;
        this.RevokedAtUtc = null;
        this.CommitMutation(nowUtc);
    }

    public void Revoke(DateTime nowUtc)
    {
        EnsureMutationTimestamp(nowUtc);
        if (!this.IsEnabled)
        {
            return;
        }

        this.PrepareMutation();
        this.IsEnabled = false;
        this.RevokedAtUtc = nowUtc;
        this.CommitMutation(nowUtc);
    }

    private static string? NormalizeConsentValue(
        string? value,
        bool required,
        string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if ((required && normalized.Length == 0) || normalized.Length > 64)
        {
            throw CreateValidationException(
                NotificationEmailPreferenceErrorCodes.InvalidConsent,
                $"{parameterName} is invalid.");
        }

        return normalized.Length == 0 ? null : normalized;
    }

    private static void ValidateTimestamps(
        bool isEnabled,
        DateTime? consentGrantedAtUtc,
        DateTime? revokedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (consentGrantedAtUtc.HasValue)
        {
            EnsureUtc(consentGrantedAtUtc.Value);
        }

        if (revokedAtUtc.HasValue)
        {
            EnsureUtc(revokedAtUtc.Value);
        }

        if (updatedAtUtc < createdAtUtc
            || isEnabled && (!consentGrantedAtUtc.HasValue || revokedAtUtc.HasValue)
            || consentGrantedAtUtc > updatedAtUtc
            || revokedAtUtc > updatedAtUtc)
        {
            throw CreateValidationException(
                NotificationEmailPreferenceErrorCodes.InvalidTimestamp,
                "The notification email preference timestamps are invalid.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw CreateValidationException(
                NotificationEmailPreferenceErrorCodes.InvalidTimestamp,
                "Notification email preference timestamps must use UTC.");
        }
    }

    private void EnsureMutationTimestamp(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw CreateValidationException(
                NotificationEmailPreferenceErrorCodes.InvalidTimestamp,
                "A notification email preference mutation cannot predate its current state.");
        }
    }

    private void PrepareMutation()
    {
        if (this.Version == long.MaxValue)
        {
            throw CreateValidationException(
                NotificationEmailPreferenceErrorCodes.InvalidVersion,
                "The notification email preference version cannot be incremented further.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static NotificationEmailPreferenceValidationException CreateValidationException(
        string code,
        string message)
    {
        return new NotificationEmailPreferenceValidationException(code, message);
    }
}
