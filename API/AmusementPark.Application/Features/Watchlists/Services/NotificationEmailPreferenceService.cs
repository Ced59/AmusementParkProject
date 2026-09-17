using System.Net.Mail;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class NotificationEmailPreferenceService
{
    private static readonly HashSet<string> SupportedLocales = new HashSet<string>(
        new[] { "de", "en", "es", "fr", "it", "nl", "pl", "pt" },
        StringComparer.Ordinal);
    private readonly INotificationEmailPreferenceRepository repository;
    private readonly IUserRepository userRepository;
    private readonly TimeProvider timeProvider;

    public NotificationEmailPreferenceService(
        INotificationEmailPreferenceRepository repository,
        IUserRepository userRepository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<NotificationEmailPreferenceResult>> GetAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeUserId(userId, out string normalizedUserId))
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.Invalid());
        }

        User? user = await this.userRepository.GetByIdAsync(normalizedUserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.Invalid());
        }

        NotificationEmailPreference? preference = await this.repository.GetAsync(
            normalizedUserId,
            cancellationToken);
        return ApplicationResult<NotificationEmailPreferenceResult>.Success(Map(user, preference));
    }

    public async Task<ApplicationResult<NotificationEmailPreferenceResult>> UpdateAsync(
        string userId,
        NotificationEmailPreferenceInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!TryNormalizeUserId(userId, out string normalizedUserId)
            || !TryNormalizeLocale(input.ConsentLocale, out string consentLocale)
            || input.ExpectedVersion.HasValue && input.ExpectedVersion.Value < 1)
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.Invalid());
        }

        User? user = await this.userRepository.GetByIdAsync(normalizedUserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.Invalid());
        }

        if (input.EmailDigestEnabled && !CanReceiveEmail(user))
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.EmailUnavailable());
        }

        if (input.EmailDigestEnabled && !input.ConsentAccepted)
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.ConsentRequired());
        }

        NotificationEmailPreference? preference = await this.repository.GetAsync(
            normalizedUserId,
            cancellationToken);
        if (preference is null)
        {
            return await this.CreateAsync(user, input, consentLocale, cancellationToken);
        }

        if (input.ExpectedVersion != preference.Version)
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.ChangedConcurrently());
        }

        long expectedVersion = preference.Version;
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            if (input.EmailDigestEnabled)
            {
                preference.GrantConsent(
                    NotificationEmailPreference.CurrentConsentTextVersion,
                    consentLocale,
                    nowUtc);
            }
            else
            {
                preference.Revoke(nowUtc);
            }
        }
        catch (NotificationEmailPreferenceValidationException)
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.Invalid());
        }

        if (preference.Version != expectedVersion)
        {
            NotificationEmailPreferenceWriteOutcome outcome = await this.repository.ReplaceAsync(
                preference,
                expectedVersion,
                cancellationToken);
            if (outcome != NotificationEmailPreferenceWriteOutcome.Success)
            {
                return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                    NotificationEmailPreferenceApplicationErrors.ChangedConcurrently());
            }
        }

        return ApplicationResult<NotificationEmailPreferenceResult>.Success(Map(user, preference));
    }

    public async Task<ApplicationResult> RevokeByTokenAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeUserId(userId, out string normalizedUserId))
        {
            return ApplicationResult.Failure(NotificationEmailPreferenceApplicationErrors.Invalid());
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            NotificationEmailPreference? preference = await this.repository.GetAsync(
                normalizedUserId,
                cancellationToken);
            if (preference is null || !preference.IsEnabled)
            {
                return ApplicationResult.Success();
            }

            long expectedVersion = preference.Version;
            preference.Revoke(this.timeProvider.GetUtcNow().UtcDateTime);
            NotificationEmailPreferenceWriteOutcome outcome = await this.repository.ReplaceAsync(
                preference,
                expectedVersion,
                cancellationToken);
            if (outcome == NotificationEmailPreferenceWriteOutcome.Success)
            {
                return ApplicationResult.Success();
            }
        }

        return ApplicationResult.Failure(
            NotificationEmailPreferenceApplicationErrors.ChangedConcurrently());
    }

    internal static bool CanReceiveEmail(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.IsActivated
            && !user.IsBlocked
            && MailAddress.TryCreate(user.Email?.Trim(), out MailAddress? address)
            && string.Equals(address.Address, user.Email?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ApplicationResult<NotificationEmailPreferenceResult>> CreateAsync(
        User user,
        NotificationEmailPreferenceInput input,
        string consentLocale,
        CancellationToken cancellationToken)
    {
        if (input.ExpectedVersion.HasValue)
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.ChangedConcurrently());
        }

        if (!input.EmailDigestEnabled)
        {
            return ApplicationResult<NotificationEmailPreferenceResult>.Success(Map(user, null));
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        NotificationEmailPreference preference = NotificationEmailPreference.CreateConsented(
            user.Id,
            NotificationEmailPreference.CurrentConsentTextVersion,
            consentLocale,
            nowUtc);
        NotificationEmailPreferenceWriteOutcome outcome = await this.repository.CreateAsync(
            preference,
            cancellationToken);
        return outcome == NotificationEmailPreferenceWriteOutcome.Success
            ? ApplicationResult<NotificationEmailPreferenceResult>.Success(Map(user, preference))
            : ApplicationResult<NotificationEmailPreferenceResult>.Failure(
                NotificationEmailPreferenceApplicationErrors.ChangedConcurrently());
    }

    private static NotificationEmailPreferenceResult Map(
        User user,
        NotificationEmailPreference? preference)
    {
        return new NotificationEmailPreferenceResult(
            preference?.IsEnabled == true,
            CanReceiveEmail(user),
            MaskEmail(user.Email),
            NotificationEmailPreference.CurrentConsentTextVersion,
            preference?.ConsentGrantedAtUtc,
            preference?.RevokedAtUtc,
            preference?.Version);
    }

    private static string? MaskEmail(string? email)
    {
        if (!MailAddress.TryCreate(email?.Trim(), out MailAddress? address))
        {
            return null;
        }

        string[] parts = address.Address.Split('@', 2);
        string local = parts[0];
        string maskedLocal = local.Length <= 1
            ? "*"
            : $"{local[0]}{new string('*', Math.Min(4, local.Length - 1))}";
        return $"{maskedLocal}@{parts[1]}";
    }

    private static bool TryNormalizeUserId(string userId, out string normalizedUserId)
    {
        normalizedUserId = string.Empty;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryNormalizeLocale(string locale, out string normalizedLocale)
    {
        normalizedLocale = locale?.Trim().ToLowerInvariant() ?? string.Empty;
        return SupportedLocales.Contains(normalizedLocale);
    }
}
