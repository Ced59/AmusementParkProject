using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class LiveAlertEvaluationService
{
    private const int MaximumPendingDeliveryBatch = 500;
    private readonly ILiveAlertSubscriptionRepository subscriptionRepository;
    private readonly ILiveAlertNotificationRepository notificationRepository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly ILiveOperationalGate operationalGate;
    private readonly TimeProvider timeProvider;

    public LiveAlertEvaluationService(
        ILiveAlertSubscriptionRepository subscriptionRepository,
        ILiveAlertNotificationRepository notificationRepository,
        ILiveDataSourceCatalog sourceCatalog,
        ILiveOperationalGate operationalGate,
        TimeProvider? timeProvider = null)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.notificationRepository = notificationRepository;
        this.sourceCatalog = sourceCatalog;
        this.operationalGate = operationalGate;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task EvaluateAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyCollection<LiveAlertSubscription> pendingSubscriptions =
            await this.subscriptionRepository.ListPendingAsync(
                nowUtc,
                MaximumPendingDeliveryBatch,
                cancellationToken);
        foreach (LiveAlertSubscription pendingSubscription in pendingSubscriptions)
        {
            _ = await this.DeliverPendingTriggerAsync(pendingSubscription, cancellationToken);
        }

        LivePollingTarget? publicTarget = this.sourceCatalog.PublicPollingTarget;
        if (!this.sourceCatalog.IsPublicReadEnabled || publicTarget is null)
        {
            return;
        }

        LiveOperationalGateSnapshot gate = await this.operationalGate.LoadAsync(
            publicTarget.SourceId,
            publicTarget.ExternalEntityId,
            cancellationToken);
        Dictionary<string, LiveLatestObservation> eligible = observations
            .Where(observation => observation.Target.Type == LiveTargetType.ParkItem
                && observation.Provenance.SourceId == publicTarget.SourceId
                && gate.AllowsPublicRead(observation.Target.ParkId, observation.Target.Type, observation.Target.Id)
                && observation.FreshnessPolicy.Assess(observation.Provenance.ObservedAtUtc, nowUtc).State
                    == LiveFreshnessState.Fresh)
            .GroupBy(static observation => observation.Target.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.OrderByDescending(item => item.Provenance.ObservedAtUtc).First(),
                StringComparer.Ordinal);
        if (eligible.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<LiveAlertSubscription> subscriptions =
            await this.subscriptionRepository.ListActiveMatchingAsync(
                eligible.Keys.ToArray(),
                nowUtc,
                cancellationToken);
        foreach (LiveAlertSubscription subscription in subscriptions)
        {
            if (subscription.PendingTrigger is not null
                && !await this.DeliverPendingTriggerAsync(subscription, cancellationToken))
            {
                continue;
            }

            if (!eligible.TryGetValue(subscription.TargetId, out LiveLatestObservation? observation))
            {
                continue;
            }

            long expectedVersion = subscription.Version;
            LiveAlertTrigger? trigger = subscription.Evaluate(observation, nowUtc);
            if (subscription.Version == expectedVersion)
            {
                continue;
            }

            WatchSubscriptionWriteOutcome outcome = await this.subscriptionRepository.ReplaceAsync(
                subscription,
                expectedVersion,
                cancellationToken);
            if (outcome == WatchSubscriptionWriteOutcome.Success && trigger is not null)
            {
                _ = await this.DeliverPendingTriggerAsync(subscription, cancellationToken);
            }
        }
    }

    private async Task<bool> DeliverPendingTriggerAsync(
        LiveAlertSubscription subscription,
        CancellationToken cancellationToken)
    {
        LiveAlertTrigger? pendingTrigger = subscription.PendingTrigger;
        if (pendingTrigger is null)
        {
            return true;
        }

        LiveAlertNotification notification = LiveAlertNotification.Create(
            LiveAlertNotificationId.New(),
            subscription,
            pendingTrigger);
        UserNotificationWriteOutcome notificationOutcome =
            await this.notificationRepository.CreateAsync(notification, cancellationToken);
        if (notificationOutcome is not UserNotificationWriteOutcome.Success
            and not UserNotificationWriteOutcome.AlreadyExists)
        {
            return false;
        }

        long expectedVersion = subscription.Version;
        subscription.MarkPendingTriggerDelivered();
        WatchSubscriptionWriteOutcome subscriptionOutcome =
            await this.subscriptionRepository.ReplaceAsync(
                subscription,
                expectedVersion,
                cancellationToken);
        return subscriptionOutcome == WatchSubscriptionWriteOutcome.Success;
    }
}
