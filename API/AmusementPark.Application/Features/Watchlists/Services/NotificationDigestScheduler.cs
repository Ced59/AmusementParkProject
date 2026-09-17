using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class NotificationDigestScheduler : INotificationDigestScheduler
{
    private readonly IWatchlistAccountDeletionFence deletionFence;
    private readonly IDurableBackgroundJobRepository jobRepository;
    private readonly IUserNotificationRepository notificationRepository;
    private readonly IWatchSubscriptionRepository subscriptionRepository;

    public NotificationDigestScheduler(
        IWatchlistAccountDeletionFence deletionFence,
        IDurableBackgroundJobRepository jobRepository,
        IUserNotificationRepository notificationRepository,
        IWatchSubscriptionRepository subscriptionRepository)
    {
        this.deletionFence = deletionFence ?? throw new ArgumentNullException(nameof(deletionFence));
        this.jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        this.notificationRepository = notificationRepository
            ?? throw new ArgumentNullException(nameof(notificationRepository));
        this.subscriptionRepository = subscriptionRepository
            ?? throw new ArgumentNullException(nameof(subscriptionRepository));
    }

    public async Task ScheduleAsync(
        FactualChangeEventId eventId,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        string[] distinctUserIds = userIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<UserNotification> notifications =
            await this.notificationRepository.ListByFactualEventAndUsersAsync(
                eventId,
                distinctUserIds,
                cancellationToken);
        foreach (IGrouping<string, UserNotification> ownerCandidates in notifications
            .GroupBy(static candidate => candidate.UserId, StringComparer.Ordinal))
        {
            if (await this.deletionFence.IsBlockedAsync(ownerCandidates.Key, cancellationToken))
            {
                continue;
            }

            WatchSubscriptionId[] subscriptionIds = ownerCandidates
                .Select(static notification => notification.SubscriptionId)
                .Distinct()
                .ToArray();
            IReadOnlyCollection<WatchSubscription> subscriptions =
                await this.subscriptionRepository.ListOwnedByIdsAsync(
                    ownerCandidates.Key,
                    subscriptionIds,
                    cancellationToken);
            Dictionary<WatchSubscriptionId, WatchSubscription> subscriptionsById = subscriptions
                .ToDictionary(static subscription => subscription.Id);
            NotificationDigestJobPayload[] groups = ownerCandidates
                .Where(notification => subscriptionsById.TryGetValue(
                    notification.SubscriptionId,
                    out WatchSubscription? subscription)
                    && IsDigestEligible(subscription, notification))
                .Select(notification =>
                {
                    WatchSubscription subscription = subscriptionsById[notification.SubscriptionId];
                    return new NotificationDigestJobPayload(
                        notification.UserId,
                        NotificationChannel.Email,
                        subscription.Frequency,
                        NotificationDigestPeriodResolver.ResolveStart(
                            subscription.Frequency,
                            notification.DeliveredAtUtc));
                })
                .Distinct()
                .ToArray();
            foreach (NotificationDigestJobPayload group in groups)
            {
                NotificationDigestId digestId = NotificationDigestId.ForGroup(
                    group.UserId,
                    group.Channel,
                    group.Frequency,
                    group.PeriodStartUtc);
                DurableBackgroundJob job = await this.jobRepository.CoalesceAsync(
                    new CoalesceBackgroundJobRequest(
                        NotificationDigestJob.Kind,
                        $"watch-digest:{digestId.Value}",
                        RequestedRevision: 0,
                        NotificationDigestJob.PayloadVersion,
                        JsonSerializer.SerializeToElement(group),
                        CorrelationId: digestId.Value,
                        AdvanceRevision: true),
                    cancellationToken);
                if (job is not null
                    && await this.deletionFence.IsBlockedAsync(group.UserId, cancellationToken))
                {
                    await this.jobRepository.CancelAsync(job.Id, cancellationToken);
                }
            }
        }
    }

    private static bool IsDigestEligible(
        WatchSubscription subscription,
        UserNotification notification)
    {
        return !subscription.IsPaused
            && subscription.Frequency is NotificationFrequency.DailyDigest
                or NotificationFrequency.WeeklyDigest
            && subscription.Channels.Contains(NotificationChannel.Email)
            && subscription.EventTypes.Contains(notification.EventType);
    }
}
