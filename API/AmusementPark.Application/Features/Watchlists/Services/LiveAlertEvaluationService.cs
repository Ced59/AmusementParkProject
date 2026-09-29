using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class LiveAlertEvaluationService
{
    private const int MaximumPendingDeliveryBatch = 500;
    private readonly ILiveAlertSubscriptionRepository subscriptionRepository;
    private readonly ILiveAlertNotificationRepository notificationRepository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly ILiveOperationalGate operationalGate;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<LiveAlertEvaluationService>? logger;

    public LiveAlertEvaluationService(
        ILiveAlertSubscriptionRepository subscriptionRepository,
        ILiveAlertNotificationRepository notificationRepository,
        ILiveDataSourceCatalog sourceCatalog,
        ILiveOperationalGate operationalGate,
        TimeProvider? timeProvider = null,
        ILogger<LiveAlertEvaluationService>? logger = null)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.notificationRepository = notificationRepository;
        this.sourceCatalog = sourceCatalog;
        this.operationalGate = operationalGate;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.logger = logger;
    }

    public async Task EvaluateAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observations);
        await this.RetryPendingAsync(cancellationToken);

        try
        {
            await this.EvaluateCurrentObservationsAsync(observations, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger?.LogError(
                exception,
                "Live alert evaluation failed after live observations were committed; polling remains healthy and the next reconciliation will retry.");
        }
    }

    public async Task RetryPendingAsync(CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            IReadOnlyCollection<LiveAlertSubscription> pendingSubscriptions =
                await this.subscriptionRepository.ListPendingAsync(
                nowUtc,
                MaximumPendingDeliveryBatch,
                cancellationToken);
            foreach (LiveAlertSubscription pendingSubscription in pendingSubscriptions)
            {
                await this.DeliverPendingTriggerSafelyAsync(
                    pendingSubscription,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger?.LogError(
                exception,
                "Pending live alert reconciliation failed and will be retried independently of live polling.");
        }
    }

    private async Task EvaluateCurrentObservationsAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
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
            try
            {
                await this.EvaluateSubscriptionAsync(
                    subscription,
                    eligible,
                    nowUtc,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger?.LogError(
                    exception,
                    "Live alert subscription {SubscriptionId} could not be evaluated and will be retried without failing live polling.",
                    subscription.Id.Value);
            }
        }
    }

    private async Task EvaluateSubscriptionAsync(
        LiveAlertSubscription subscription,
        IReadOnlyDictionary<string, LiveLatestObservation> eligible,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (subscription.PendingTrigger is not null
            && !await this.DeliverPendingTriggerAsync(subscription, cancellationToken))
        {
            return;
        }

        if (!eligible.TryGetValue(subscription.TargetId, out LiveLatestObservation? observation))
        {
            return;
        }

        long expectedVersion = subscription.Version;
        LiveAlertTrigger? trigger = subscription.Evaluate(observation, nowUtc);
        if (subscription.Version == expectedVersion)
        {
            return;
        }

        WatchSubscriptionWriteOutcome outcome = await this.subscriptionRepository.ReplaceAsync(
            subscription,
            expectedVersion,
            cancellationToken);
        if (outcome == WatchSubscriptionWriteOutcome.Success && trigger is not null)
        {
            await this.DeliverPendingTriggerSafelyAsync(subscription, cancellationToken);
        }
    }

    private async Task<bool> DeliverPendingTriggerSafelyAsync(
        LiveAlertSubscription subscription,
        CancellationToken cancellationToken)
    {
        try
        {
            return await this.DeliverPendingTriggerAsync(subscription, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger?.LogError(
                exception,
                "Pending live alert for subscription {SubscriptionId} could not be delivered and remains durable for retry.",
                subscription.Id.Value);
            return false;
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
