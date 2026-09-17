using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class NotificationEmailDeliveryJobHandler : IDurableBackgroundJobHandler
{
    private readonly IWatchlistAccountDeletionFence deletionFence;
    private readonly INotificationDigestRepository digestRepository;
    private readonly INotificationEmailPreferenceRepository preferenceRepository;
    private readonly INotificationDeliveryAttemptRepository attemptRepository;
    private readonly IUserRepository userRepository;
    private readonly NotificationDigestEmailEntryResolver entryResolver;
    private readonly INotificationEmailUnsubscribeTokenProtector tokenProtector;
    private readonly INotificationDigestEmailSender emailSender;
    private readonly TimeProvider timeProvider;

    public NotificationEmailDeliveryJobHandler(
        IWatchlistAccountDeletionFence deletionFence,
        INotificationDigestRepository digestRepository,
        INotificationEmailPreferenceRepository preferenceRepository,
        INotificationDeliveryAttemptRepository attemptRepository,
        IUserRepository userRepository,
        NotificationDigestEmailEntryResolver entryResolver,
        INotificationEmailUnsubscribeTokenProtector tokenProtector,
        INotificationDigestEmailSender emailSender)
        : this(
            deletionFence,
            digestRepository,
            preferenceRepository,
            attemptRepository,
            userRepository,
            entryResolver,
            tokenProtector,
            emailSender,
            TimeProvider.System)
    {
    }

    internal NotificationEmailDeliveryJobHandler(
        IWatchlistAccountDeletionFence deletionFence,
        INotificationDigestRepository digestRepository,
        INotificationEmailPreferenceRepository preferenceRepository,
        INotificationDeliveryAttemptRepository attemptRepository,
        IUserRepository userRepository,
        NotificationDigestEmailEntryResolver entryResolver,
        INotificationEmailUnsubscribeTokenProtector tokenProtector,
        INotificationDigestEmailSender emailSender,
        TimeProvider timeProvider)
    {
        this.deletionFence = deletionFence ?? throw new ArgumentNullException(nameof(deletionFence));
        this.digestRepository = digestRepository ?? throw new ArgumentNullException(nameof(digestRepository));
        this.preferenceRepository = preferenceRepository
            ?? throw new ArgumentNullException(nameof(preferenceRepository));
        this.attemptRepository = attemptRepository ?? throw new ArgumentNullException(nameof(attemptRepository));
        this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        this.entryResolver = entryResolver ?? throw new ArgumentNullException(nameof(entryResolver));
        this.tokenProtector = tokenProtector ?? throw new ArgumentNullException(nameof(tokenProtector));
        this.emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            NotificationEmailDeliveryJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { NotificationEmailDeliveryJob.PayloadVersion },
            TimeSpan.FromMinutes(2),
            maximumAttempts: 6,
            initialRetryDelay: TimeSpan.FromMinutes(2),
            maximumRetryDelay: TimeSpan.FromHours(2),
            maximumConcurrency: 2);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        NotificationEmailDeliveryJobPayload? payload = Parse(context);
        if (payload is null || !TryParseDigestId(payload.DigestId, out NotificationDigestId digestId))
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                NotificationEmailDeliveryErrorCodes.InvalidPayload);
        }

        NotificationDigest? digest = await this.digestRepository.GetAsync(digestId, cancellationToken);
        if (digest is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                NotificationEmailDeliveryErrorCodes.DigestMissing);
        }

        if (await this.deletionFence.IsBlockedAsync(digest.UserId, cancellationToken))
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (nowUtc < digest.PeriodEndUtc)
        {
            return DurableBackgroundJobHandlerResult.Retry(
                NotificationEmailDeliveryErrorCodes.DigestNotClosed);
        }

        string attemptId = $"email:{digest.Id.Value}";
        NotificationDeliveryAttempt? attempt = await this.GetOrCreateAttemptAsync(
            attemptId,
            digest,
            nowUtc,
            cancellationToken);
        if (attempt is null)
        {
            return DurableBackgroundJobHandlerResult.Retry(
                NotificationEmailDeliveryErrorCodes.PersistenceConflict);
        }

        if (await this.deletionFence.IsBlockedAsync(digest.UserId, cancellationToken))
        {
            await this.attemptRepository.DeleteAsync(attempt.Id, cancellationToken);
            return DurableBackgroundJobHandlerResult.Success();
        }

        if (attempt.Status is NotificationDeliveryAttemptStatus.Succeeded
            or NotificationDeliveryAttemptStatus.Cancelled)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        if (attempt.Status == NotificationDeliveryAttemptStatus.Pending
            && attempt.AttemptCount > 0)
        {
            return await this.CancelAsync(
                attempt,
                NotificationEmailDeliveryErrorCodes.AmbiguousProviderAcceptance,
                nowUtc,
                cancellationToken);
        }

        NotificationEmailPreference? preference = await this.preferenceRepository.GetAsync(
            digest.UserId,
            cancellationToken);
        if (preference?.IsEnabled != true)
        {
            return await this.CancelAsync(
                attempt,
                NotificationEmailDeliveryErrorCodes.PreferenceDisabled,
                nowUtc,
                cancellationToken);
        }

        User? user = await this.userRepository.GetByIdAsync(digest.UserId, cancellationToken);
        if (user is null
            || !NotificationEmailPreferenceService.CanReceiveEmail(user)
            || string.IsNullOrWhiteSpace(user.Email))
        {
            return await this.CancelAsync(
                attempt,
                NotificationEmailDeliveryErrorCodes.EmailUnavailable,
                nowUtc,
                cancellationToken);
        }

        NotificationDigestEmailEntry[] entries = await this.entryResolver.ResolveEligibleAsync(
            digest,
            cancellationToken);
        if (entries.Length == 0)
        {
            return await this.CancelAsync(
                attempt,
                NotificationEmailDeliveryErrorCodes.NoEligibleEntries,
                nowUtc,
                cancellationToken);
        }

        if (await this.deletionFence.IsBlockedAsync(digest.UserId, cancellationToken))
        {
            await this.attemptRepository.DeleteAsync(attempt.Id, cancellationToken);
            return DurableBackgroundJobHandlerResult.Success();
        }

        long expectedVersion = attempt.Version;
        attempt.BeginAttempt(nowUtc);
        if (await this.attemptRepository.ReplaceAsync(
                attempt,
                expectedVersion,
                cancellationToken) != NotificationDeliveryAttemptWriteOutcome.Success)
        {
            return DurableBackgroundJobHandlerResult.Retry(
                NotificationEmailDeliveryErrorCodes.PersistenceConflict);
        }

        if (await this.deletionFence.IsBlockedAsync(digest.UserId, cancellationToken))
        {
            await this.attemptRepository.DeleteAsync(attempt.Id, cancellationToken);
            return DurableBackgroundJobHandlerResult.Success();
        }

        NotificationDigestEmailMessage message = new NotificationDigestEmailMessage(
            user.Email.Trim(),
            preference.ConsentLocale ?? "en",
            digest.Frequency,
            digest.PeriodStartUtc,
            digest.PeriodEndUtc,
            entries,
            digest.ObservedNotificationCount,
            this.tokenProtector.CreateToken(digest.UserId));
        try
        {
            await this.emailSender.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return await this.RecordFailureAsync(attempt, cancellationToken);
        }

        expectedVersion = attempt.Version;
        attempt.MarkSucceeded(this.timeProvider.GetUtcNow().UtcDateTime);
        return await this.attemptRepository.ReplaceAsync(
                attempt,
                expectedVersion,
                cancellationToken) == NotificationDeliveryAttemptWriteOutcome.Success
            ? DurableBackgroundJobHandlerResult.Success()
            : DurableBackgroundJobHandlerResult.Retry(
                NotificationEmailDeliveryErrorCodes.PersistenceConflict);
    }

    private async Task<NotificationDeliveryAttempt?> GetOrCreateAttemptAsync(
        string attemptId,
        NotificationDigest digest,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        NotificationDeliveryAttempt? attempt = await this.attemptRepository.GetAsync(
            attemptId,
            cancellationToken);
        if (attempt is not null)
        {
            return attempt;
        }

        attempt = NotificationDeliveryAttempt.Create(
            attemptId,
            digest.UserId,
            digest.Id,
            nowUtc);
        NotificationDeliveryAttemptWriteOutcome outcome = await this.attemptRepository.CreateAsync(
            attempt,
            cancellationToken);
        return outcome == NotificationDeliveryAttemptWriteOutcome.Success
            ? attempt
            : await this.attemptRepository.GetAsync(attemptId, cancellationToken);
    }

    private async Task<DurableBackgroundJobHandlerResult> CancelAsync(
        NotificationDeliveryAttempt attempt,
        string reasonCode,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        long expectedVersion = attempt.Version;
        attempt.Cancel(reasonCode, nowUtc);
        return await this.attemptRepository.ReplaceAsync(
                attempt,
                expectedVersion,
                cancellationToken) == NotificationDeliveryAttemptWriteOutcome.Success
            ? DurableBackgroundJobHandlerResult.Success()
            : DurableBackgroundJobHandlerResult.Retry(
                NotificationEmailDeliveryErrorCodes.PersistenceConflict);
    }

    private async Task<DurableBackgroundJobHandlerResult> RecordFailureAsync(
        NotificationDeliveryAttempt attempt,
        CancellationToken cancellationToken)
    {
        long expectedVersion = attempt.Version;
        attempt.RecordFailure(
            NotificationEmailDeliveryErrorCodes.ProviderUnavailable,
            this.timeProvider.GetUtcNow().UtcDateTime);
        NotificationDeliveryAttemptWriteOutcome outcome = await this.attemptRepository.ReplaceAsync(
            attempt,
            expectedVersion,
            cancellationToken);
        return outcome == NotificationDeliveryAttemptWriteOutcome.Success
            ? DurableBackgroundJobHandlerResult.Retry(
                NotificationEmailDeliveryErrorCodes.ProviderUnavailable)
            : DurableBackgroundJobHandlerResult.Retry(
                NotificationEmailDeliveryErrorCodes.PersistenceConflict);
    }

    private static NotificationEmailDeliveryJobPayload? Parse(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != NotificationEmailDeliveryJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            return context.Payload.Deserialize<NotificationEmailDeliveryJobPayload>();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryParseDigestId(string? value, out NotificationDigestId digestId)
    {
        digestId = default;
        try
        {
            digestId = NotificationDigestId.Parse(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
