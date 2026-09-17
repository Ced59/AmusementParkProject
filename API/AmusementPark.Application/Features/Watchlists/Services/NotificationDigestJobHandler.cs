using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class NotificationDigestJobHandler : IDurableBackgroundJobHandler
{
    private readonly IUserNotificationRepository notificationRepository;
    private readonly IWatchSubscriptionRepository subscriptionRepository;
    private readonly IFactualChangeEventRepository eventRepository;
    private readonly INotificationDigestRepository digestRepository;
    private readonly TimeProvider timeProvider;

    public NotificationDigestJobHandler(
        IUserNotificationRepository notificationRepository,
        IWatchSubscriptionRepository subscriptionRepository,
        IFactualChangeEventRepository eventRepository,
        INotificationDigestRepository digestRepository)
        : this(
            notificationRepository,
            subscriptionRepository,
            eventRepository,
            digestRepository,
            TimeProvider.System)
    {
    }

    internal NotificationDigestJobHandler(
        IUserNotificationRepository notificationRepository,
        IWatchSubscriptionRepository subscriptionRepository,
        IFactualChangeEventRepository eventRepository,
        INotificationDigestRepository digestRepository,
        TimeProvider timeProvider)
    {
        this.notificationRepository = notificationRepository
            ?? throw new ArgumentNullException(nameof(notificationRepository));
        this.subscriptionRepository = subscriptionRepository
            ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        this.digestRepository = digestRepository ?? throw new ArgumentNullException(nameof(digestRepository));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            NotificationDigestJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { NotificationDigestJob.PayloadVersion },
            TimeSpan.FromMinutes(2),
            maximumAttempts: 5,
            initialRetryDelay: TimeSpan.FromSeconds(30),
            maximumRetryDelay: TimeSpan.FromMinutes(10),
            maximumConcurrency: 2);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        NotificationDigestJobPayload? payload = Parse(context);
        if (!TryValidate(payload, out DateTime periodEndUtc) || payload is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(NotificationDigestErrorCodes.InvalidPayload);
        }

        IReadOnlyCollection<WatchSubscription> subscriptions =
            await this.subscriptionRepository.ListOwnedAsync(
                payload.UserId,
                null,
                null,
                cancellationToken);
        NotificationDigestSubscriptionFilter[] subscriptionFilters = subscriptions
            .Where(subscription => IsEligible(subscription, payload))
            .Select(subscription => new NotificationDigestSubscriptionFilter(
                subscription.Id,
                subscription.EventTypes))
            .ToArray();
        IReadOnlyCollection<UserNotification> notifications =
            await this.notificationRepository.ListOwnedForDigestAsync(
                payload.UserId,
                payload.PeriodStartUtc,
                periodEndUtc,
                subscriptionFilters,
                NotificationDigest.MaximumEntries + 1,
                cancellationToken);
        if (notifications.Count > NotificationDigest.MaximumEntries)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                NotificationDigestErrorCodes.TooManyNotifications);
        }

        IReadOnlyCollection<FactualChangeEvent> factualEvents = await this.eventRepository.GetManyAsync(
            notifications
                .Select(static notification => notification.FactualEventId)
                .Distinct()
                .ToArray(),
            cancellationToken);
        Dictionary<FactualChangeEventId, FactualChangeEvent> eventsById = factualEvents
            .ToDictionary(static factualEvent => factualEvent.Id);
        if (notifications.Any(notification =>
            !eventsById.ContainsKey(notification.FactualEventId)))
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                NotificationDigestErrorCodes.EventMissing);
        }

        NotificationDigestEntry[] entries = notifications
            .Select(notification => ToEntry(notification, eventsById[notification.FactualEventId]))
            .ToArray();
        try
        {
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            NotificationDigest snapshot = NotificationDigest.CreateSnapshot(
                payload.UserId,
                payload.Channel,
                payload.Frequency,
                payload.PeriodStartUtc,
                entries,
                notifications.Count,
                nowUtc);
            await this.digestRepository.ReplaceSnapshotAsync(snapshot, cancellationToken);
            return DurableBackgroundJobHandlerResult.Success();
        }
        catch (ArgumentException)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(NotificationDigestErrorCodes.InvalidSnapshot);
        }
    }

    private static NotificationDigestJobPayload? Parse(DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != NotificationDigestJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            return context.Payload.Deserialize<NotificationDigestJobPayload>();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryValidate(
        NotificationDigestJobPayload? payload,
        out DateTime periodEndUtc)
    {
        periodEndUtc = default;
        if (payload is null || string.IsNullOrWhiteSpace(payload.UserId))
        {
            return false;
        }

        try
        {
            NotificationDigestPeriodResolver.ValidateGroup(
                payload.Channel,
                payload.Frequency,
                payload.PeriodStartUtc);
            periodEndUtc = NotificationDigestPeriodResolver.ResolveEnd(
                payload.Frequency,
                payload.PeriodStartUtc);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsEligible(
        WatchSubscription subscription,
        NotificationDigestJobPayload payload)
    {
        return !subscription.IsPaused
            && subscription.Frequency == payload.Frequency
            && subscription.Channels.Contains(payload.Channel);
    }

    private static NotificationDigestEntry ToEntry(
        UserNotification notification,
        FactualChangeEvent factualEvent)
    {
        return new NotificationDigestEntry(
            notification.FactualEventId,
            notification.SubscriptionId,
            factualEvent.DeduplicationKey,
            notification.SourceRevision,
            notification.EventType,
            notification.TargetType,
            notification.TargetId,
            factualEvent.Status,
            notification.DeliveredAtUtc);
    }
}
