using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class LiveAlertLifecycleService
{
    private const int MaximumNotifications = 50;
    private readonly ILiveAlertSubscriptionRepository subscriptionRepository;
    private readonly ILiveAlertNotificationRepository notificationRepository;
    private readonly ILiveLatestObservationRepository observationRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly PublicLiveLatestReader publicLiveReader;
    private readonly UserCollectionTargetReader targetReader;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly IWatchlistAccountDeletionFence deletionFence;
    private readonly TimeProvider timeProvider;

    public LiveAlertLifecycleService(
        ILiveAlertSubscriptionRepository subscriptionRepository,
        ILiveAlertNotificationRepository notificationRepository,
        ILiveLatestObservationRepository observationRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        PublicLiveLatestReader publicLiveReader,
        UserCollectionTargetReader targetReader,
        ILiveDataSourceCatalog sourceCatalog,
        IWatchlistAccountDeletionFence deletionFence,
        TimeProvider? timeProvider = null)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.notificationRepository = notificationRepository;
        this.observationRepository = observationRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.publicLiveReader = publicLiveReader;
        this.targetReader = targetReader;
        this.sourceCatalog = sourceCatalog;
        this.deletionFence = deletionFence;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<LiveAlertDashboardResult>> GetDashboardAsync(
        string userId,
        string? targetId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        }
        catch (ArgumentException)
        {
            return ApplicationResult<LiveAlertDashboardResult>.Failure(LiveAlertApplicationErrors.Invalid());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyCollection<LiveAlertSubscription> subscriptions =
            await this.subscriptionRepository.ListOwnedAsync(
                normalizedUserId,
                string.IsNullOrWhiteSpace(targetId) ? null : targetId.Trim(),
                nowUtc,
                cancellationToken);
        IReadOnlyCollection<LiveAlertNotification> notifications = string.IsNullOrWhiteSpace(targetId)
            ? await this.notificationRepository.ListOwnedAsync(
                normalizedUserId,
                nowUtc,
                MaximumNotifications,
                cancellationToken)
            : Array.Empty<LiveAlertNotification>();
        string[] targetIds = subscriptions.Select(static item => item.TargetId)
            .Concat(notifications.Select(static item => item.TargetId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, UserCollectionTargetSnapshot> targets =
            await this.targetReader.ResolveAsync(CollectionTargetType.ParkItem, targetIds, cancellationToken);
        int unreadCount = await this.notificationRepository.CountUnreadAsync(
            normalizedUserId,
            nowUtc,
            cancellationToken);
        return ApplicationResult<LiveAlertDashboardResult>.Success(
            new LiveAlertDashboardResult(
                subscriptions.Select(item => Map(item, targets.GetValueOrDefault(item.TargetId))).ToArray(),
                notifications.Select(item => this.Map(item, targets.GetValueOrDefault(item.TargetId))).ToArray(),
                unreadCount,
                LiveAlertNotification.RetentionDays));
    }

    public async Task<ApplicationResult<LiveAlertSubscriptionResult>> CreateAsync(
        string userId,
        LiveAlertPreferenceInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            bool thresholdRequired = input.Type is LiveAlertType.WaitBelow or LiveAlertType.WaitAbove;
            if (!Enum.IsDefined(input.Type)
                || input.DurationMinutes is < LiveAlertSubscription.MinimumDurationMinutes
                    or > LiveAlertSubscription.MaximumDurationMinutes
                || thresholdRequired != input.ThresholdMinutes.HasValue
                || input.ThresholdMinutes is < 5 or > 300
                || await this.deletionFence.IsBlockedAsync(normalizedUserId, cancellationToken))
            {
                return ApplicationResult<LiveAlertSubscriptionResult>.Failure(LiveAlertApplicationErrors.Invalid());
            }

            UserCollectionTargetSnapshot target = await this.targetReader.ResolveAsync(
                CollectionTargetType.ParkItem,
                input.TargetId,
                cancellationToken);
            if (!target.IsAvailableForCreation || string.IsNullOrWhiteSpace(target.ParentParkId))
            {
                return ApplicationResult<LiveAlertSubscriptionResult>.Failure(LiveAlertApplicationErrors.TargetUnavailable());
            }

            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            IReadOnlyCollection<LiveAlertSubscription> existing = await this.subscriptionRepository.ListOwnedAsync(
                normalizedUserId,
                target.TargetId,
                nowUtc,
                cancellationToken);
            LiveAlertSubscription? duplicate = existing.FirstOrDefault(item =>
                item.Type == input.Type && item.ThresholdMinutes == input.ThresholdMinutes);
            if (duplicate is not null)
            {
                return ApplicationResult<LiveAlertSubscriptionResult>.Success(Map(duplicate, target));
            }

            ApplicationResult<PublicLiveTargetResult> publicResult = await this.publicLiveReader.ReadParkItemAsync(
                target.TargetId,
                cancellationToken);
            PublicLiveTargetResult? live = publicResult.Value;
            if (!publicResult.IsSuccess || live is null
                || live.Freshness != LiveFreshnessState.Fresh
                || live.Source is null
                || !live.ObservedAtUtc.HasValue)
            {
                return ApplicationResult<LiveAlertSubscriptionResult>.Failure(LiveAlertApplicationErrors.TargetUnavailable());
            }

            IReadOnlyCollection<LiveLatestObservation> observations =
                await this.observationRepository.GetByTargetAsync(
                    LiveTargetType.ParkItem,
                    target.TargetId,
                    target.ParentParkId,
                    cancellationToken);
            LiveLatestObservation? seed = observations.FirstOrDefault(observation =>
                string.Equals(observation.Provenance.SourceId.Value, live.Source.Id, StringComparison.Ordinal)
                && observation.Provenance.ObservedAtUtc == live.ObservedAtUtc.Value);
            if (seed is null)
            {
                return ApplicationResult<LiveAlertSubscriptionResult>.Failure(LiveAlertApplicationErrors.TargetUnavailable());
            }

            ParkOpeningHoursSchedule? schedule = await this.openingHoursRepository.GetByParkIdAsync(
                target.ParentParkId,
                cancellationToken);
            DateTime expiresAtUtc = ResolveExpiration(
                nowUtc,
                input.DurationMinutes,
                schedule?.TimeZoneId);
            LiveAlertSubscription subscription = LiveAlertSubscription.Create(
                LiveAlertSubscriptionId.New(),
                normalizedUserId,
                target.TargetId,
                target.ParentParkId,
                input.Type,
                input.ThresholdMinutes,
                nowUtc,
                expiresAtUtc,
                seed);
            WatchSubscriptionWriteOutcome outcome = await this.subscriptionRepository.CreateAsync(
                subscription,
                nowUtc,
                cancellationToken);
            return outcome switch
            {
                WatchSubscriptionWriteOutcome.Success =>
                    ApplicationResult<LiveAlertSubscriptionResult>.Success(Map(subscription, target)),
                WatchSubscriptionWriteOutcome.LimitReached =>
                    ApplicationResult<LiveAlertSubscriptionResult>.Failure(LiveAlertApplicationErrors.LimitReached()),
                _ => ApplicationResult<LiveAlertSubscriptionResult>.Failure(LiveAlertApplicationErrors.Conflict()),
            };
        }
        catch (ArgumentException)
        {
            return ApplicationResult<LiveAlertSubscriptionResult>.Failure(LiveAlertApplicationErrors.Invalid());
        }
    }

    public async Task<ApplicationResult> DeleteAsync(
        string userId,
        string subscriptionId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (!LiveAlertSubscriptionId.TryParse(subscriptionId, out LiveAlertSubscriptionId parsedId)
            || expectedVersion < 1)
        {
            return ApplicationResult.Failure(LiveAlertApplicationErrors.Invalid());
        }

        WatchSubscriptionWriteOutcome outcome = await this.subscriptionRepository.DeleteAsync(
            userId,
            parsedId,
            expectedVersion,
            cancellationToken);
        return outcome switch
        {
            WatchSubscriptionWriteOutcome.Success => ApplicationResult.Success(),
            WatchSubscriptionWriteOutcome.NotFound => ApplicationResult.Failure(LiveAlertApplicationErrors.NotFound()),
            _ => ApplicationResult.Failure(LiveAlertApplicationErrors.Conflict()),
        };
    }

    public async Task<ApplicationResult> MutateNotificationAsync(
        string userId,
        string notificationId,
        long expectedVersion,
        LiveAlertNotificationMutation mutation,
        CancellationToken cancellationToken)
    {
        if (!LiveAlertNotificationId.TryParse(notificationId, out LiveAlertNotificationId parsedId)
            || expectedVersion < 1 || !Enum.IsDefined(mutation))
        {
            return ApplicationResult.Failure(LiveAlertApplicationErrors.Invalid());
        }

        LiveAlertNotification? notification = await this.notificationRepository.GetOwnedAsync(
            userId,
            parsedId,
            cancellationToken);
        if (notification is null)
        {
            return ApplicationResult.Failure(LiveAlertApplicationErrors.NotFound());
        }

        if (notification.Version != expectedVersion)
        {
            return ApplicationResult.Failure(LiveAlertApplicationErrors.Conflict());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (mutation == LiveAlertNotificationMutation.Read)
        {
            notification.MarkRead(nowUtc);
        }
        else
        {
            notification.Dismiss(nowUtc);
        }

        if (notification.Version == expectedVersion)
        {
            return ApplicationResult.Success();
        }

        UserNotificationWriteOutcome outcome = await this.notificationRepository.ReplaceAsync(
            notification,
            expectedVersion,
            cancellationToken);
        return outcome == UserNotificationWriteOutcome.Success
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(LiveAlertApplicationErrors.Conflict());
    }

    private static LiveAlertSubscriptionResult Map(
        LiveAlertSubscription subscription,
        UserCollectionTargetSnapshot? target)
    {
        return new LiveAlertSubscriptionResult(
            subscription.Id.Value,
            subscription.TargetId,
            subscription.ParkId,
            target?.Name,
            target?.ParentParkName,
            target?.MainImageId,
            subscription.Type,
            subscription.ThresholdMinutes,
            subscription.CreatedAtUtc,
            subscription.ExpiresAtUtc,
            subscription.LastObservedAtUtc,
            subscription.LastStatus,
            subscription.LastWaitMinutes,
            LiveAlertSubscription.CooldownMinutes,
            LiveAlertSubscription.WaitHysteresisMinutes,
            subscription.Version);
    }

    private LiveAlertNotificationResult Map(
        LiveAlertNotification notification,
        UserCollectionTargetSnapshot? target)
    {
        LiveDataSourcePresentation? source = this.sourceCatalog.Find(notification.SourceId);
        return new LiveAlertNotificationResult(
            notification.Id.Value,
            notification.SubscriptionId.Value,
            notification.TargetId,
            notification.ParkId,
            target?.Name,
            target?.ParentParkName,
            target?.MainImageId,
            notification.Type,
            notification.ThresholdMinutes,
            notification.PreviousStatus,
            notification.CurrentStatus,
            notification.PreviousWaitMinutes,
            notification.CurrentWaitMinutes,
            notification.SourceId.Value,
            source?.Source.DisplayName,
            source?.AttributionText,
            source?.AttributionUrl,
            notification.ObservedAtUtc,
            notification.DeliveredAtUtc,
            notification.AgeSeconds,
            notification.Status,
            notification.ReadAtUtc,
            notification.ExpiresAtUtc,
            notification.Version);
    }

    private static DateTime ResolveExpiration(DateTime nowUtc, int durationMinutes, string? timeZoneId)
    {
        DateTime durationExpiration = nowUtc.AddMinutes(durationMinutes);
        TimeZoneInfo timeZone = ResolveTimeZone(timeZoneId);
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        DateTime nextLocalMidnight = DateTime.SpecifyKind(localNow.Date.AddDays(1), DateTimeKind.Unspecified);
        DateTime dayExpiration = TimeZoneInfo.ConvertTimeToUtc(nextLocalMidnight, timeZone);
        return durationExpiration <= dayExpiration ? durationExpiration : dayExpiration;
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        string? normalized = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim();
        if (normalized is not null
            && TimeZoneInfo.TryFindSystemTimeZoneById(normalized, out TimeZoneInfo? direct))
        {
            return direct;
        }

        if (normalized is not null
            && TimeZoneInfo.TryConvertIanaIdToWindowsId(normalized, out string? windowsId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out TimeZoneInfo? windows))
        {
            return windows;
        }

        if (normalized is not null
            && TimeZoneInfo.TryConvertWindowsIdToIanaId(normalized, out string? ianaId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out TimeZoneInfo? iana))
        {
            return iana;
        }

        return TimeZoneInfo.Utc;
    }
}
