using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class LiveAlertEvaluationService
{
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
        LivePollingTarget? publicTarget = this.sourceCatalog.PublicPollingTarget;
        if (!this.sourceCatalog.IsPublicReadEnabled || publicTarget is null)
        {
            return;
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
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
                LiveAlertNotification notification = LiveAlertNotification.Create(
                    LiveAlertNotificationId.New(),
                    subscription,
                    trigger);
                _ = await this.notificationRepository.CreateAsync(notification, cancellationToken);
            }
        }
    }
}
