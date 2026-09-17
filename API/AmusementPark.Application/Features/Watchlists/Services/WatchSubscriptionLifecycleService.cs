using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class WatchSubscriptionLifecycleService
{
    private readonly IWatchSubscriptionRepository repository;
    private readonly UserCollectionTargetReader targetReader;
    private readonly TimeProvider timeProvider;
    private readonly WatchPilotMetricsRecorder? pilotMetricsRecorder;

    public WatchSubscriptionLifecycleService(
        IWatchSubscriptionRepository repository,
        UserCollectionTargetReader targetReader,
        TimeProvider? timeProvider = null,
        WatchPilotMetricsRecorder? pilotMetricsRecorder = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.targetReader = targetReader ?? throw new ArgumentNullException(nameof(targetReader));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.pilotMetricsRecorder = pilotMetricsRecorder;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>>> ListAsync(
        string userId,
        CollectionTargetType? targetType,
        string? targetId,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            IReadOnlyCollection<WatchSubscription> subscriptions = await this.repository.ListOwnedAsync(
                normalizedUserId,
                targetType,
                targetId,
                cancellationToken);
            IReadOnlyDictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>>
                snapshots = await this.ResolveTargetsAsync(subscriptions, cancellationToken);
            WatchSubscriptionResult[] results = subscriptions
                .Select(subscription => ToResult(subscription, FindSnapshot(snapshots, subscription)))
                .ToArray();
            return ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>>.Success(results);
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>>.Failure(
                WatchSubscriptionApplicationErrors.Invalid(
                    WatchSubscriptionErrorCodes.InvalidState,
                    exception.Message));
        }
    }

    public async Task<ApplicationResult<WatchSubscriptionResult>> CreateAsync(
        string userId,
        WatchSubscriptionPreferenceInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            UserCollectionTargetSnapshot target = await this.targetReader.ResolveAsync(
                input.TargetType,
                input.TargetId,
                cancellationToken);
            if (!target.IsAvailableForCreation)
            {
                return ApplicationResult<WatchSubscriptionResult>.Failure(
                    WatchSubscriptionApplicationErrors.TargetNotFound());
            }

            WatchSubscription? existing = await this.repository.GetOwnedByTargetAsync(
                normalizedUserId,
                input.TargetType,
                input.TargetId,
                cancellationToken);
            if (existing is not null)
            {
                return ApplicationResult<WatchSubscriptionResult>.Success(ToResult(existing, target));
            }

            WatchSubscription subscription = WatchSubscription.Create(
                WatchSubscriptionId.New(),
                normalizedUserId,
                input.TargetType,
                input.TargetId,
                input.EventTypes,
                input.Frequency,
                input.Channels,
                this.timeProvider.GetUtcNow().UtcDateTime);
            WatchSubscriptionWriteOutcome outcome = await this.repository.CreateAsync(
                subscription,
                cancellationToken);
            if (outcome == WatchSubscriptionWriteOutcome.LimitReached)
            {
                return ApplicationResult<WatchSubscriptionResult>.Failure(
                    WatchSubscriptionApplicationErrors.LimitReached());
            }

            if (outcome == WatchSubscriptionWriteOutcome.AlreadyExists)
            {
                WatchSubscription? concurrent = await this.repository.GetOwnedByTargetAsync(
                    normalizedUserId,
                    input.TargetType,
                    input.TargetId,
                    cancellationToken);
                return concurrent is null
                    ? ApplicationResult<WatchSubscriptionResult>.Failure(
                        WatchSubscriptionApplicationErrors.ChangedConcurrently())
                    : ApplicationResult<WatchSubscriptionResult>.Success(ToResult(concurrent, target));
            }

            return outcome == WatchSubscriptionWriteOutcome.Success
                ? ApplicationResult<WatchSubscriptionResult>.Success(ToResult(subscription, target))
                : ApplicationResult<WatchSubscriptionResult>.Failure(
                    WatchSubscriptionApplicationErrors.ChangedConcurrently());
        }
        catch (WatchSubscriptionValidationException exception)
        {
            return Invalid<WatchSubscriptionResult>(exception);
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<WatchSubscriptionResult>.Failure(
                WatchSubscriptionApplicationErrors.Invalid(
                    WatchSubscriptionErrorCodes.InvalidState,
                    exception.Message));
        }
    }

    public Task<ApplicationResult<WatchSubscriptionResult>> UpdateAsync(
        string userId,
        string subscriptionId,
        long expectedVersion,
        WatchSubscriptionSettingsInput input,
        CancellationToken cancellationToken)
    {
        return this.MutateAsync(
            userId,
            subscriptionId,
            expectedVersion,
            (subscription, nowUtc) => subscription.UpdatePreferences(
                input.EventTypes,
                input.Frequency,
                input.Channels,
                nowUtc),
            cancellationToken);
    }

    public Task<ApplicationResult<WatchSubscriptionResult>> SetPausedAsync(
        string userId,
        string subscriptionId,
        long expectedVersion,
        bool paused,
        CancellationToken cancellationToken)
    {
        return this.MutateAsync(
            userId,
            subscriptionId,
            expectedVersion,
            paused
                ? static (subscription, nowUtc) => subscription.Pause(nowUtc)
                : static (subscription, nowUtc) => subscription.Resume(nowUtc),
            cancellationToken);
    }

    public async Task<ApplicationResult> DeleteAsync(
        string userId,
        string subscriptionId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMutation(userId, subscriptionId, expectedVersion, out string normalizedUserId, out WatchSubscriptionId parsedId))
        {
            return ApplicationResult.Failure(WatchSubscriptionApplicationErrors.Invalid(
                WatchSubscriptionErrorCodes.InvalidVersion,
                "A valid subscription and version are required."));
        }

        WatchSubscriptionWriteOutcome outcome = await this.repository.DeleteAsync(
            normalizedUserId,
            parsedId,
            expectedVersion,
            cancellationToken);
        if (outcome == WatchSubscriptionWriteOutcome.Success && this.pilotMetricsRecorder is not null)
        {
            await this.pilotMetricsRecorder.RecordBestEffortAsync(
                WatchPilotInteractionKind.SubscriptionRemoved,
                cancellationToken);
        }

        return outcome switch
        {
            WatchSubscriptionWriteOutcome.Success => ApplicationResult.Success(),
            WatchSubscriptionWriteOutcome.NotFound => ApplicationResult.Failure(
                WatchSubscriptionApplicationErrors.NotFound()),
            _ => ApplicationResult.Failure(WatchSubscriptionApplicationErrors.ChangedConcurrently()),
        };
    }

    private async Task<ApplicationResult<WatchSubscriptionResult>> MutateAsync(
        string userId,
        string subscriptionId,
        long expectedVersion,
        Action<WatchSubscription, DateTime> mutation,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMutation(userId, subscriptionId, expectedVersion, out string normalizedUserId, out WatchSubscriptionId parsedId))
        {
            return ApplicationResult<WatchSubscriptionResult>.Failure(
                WatchSubscriptionApplicationErrors.Invalid(
                    WatchSubscriptionErrorCodes.InvalidVersion,
                    "A valid subscription and version are required."));
        }

        WatchSubscription? subscription = await this.repository.GetOwnedAsync(
            normalizedUserId,
            parsedId,
            cancellationToken);
        if (subscription is null)
        {
            return ApplicationResult<WatchSubscriptionResult>.Failure(
                WatchSubscriptionApplicationErrors.NotFound());
        }

        if (subscription.Version != expectedVersion)
        {
            return ApplicationResult<WatchSubscriptionResult>.Failure(
                WatchSubscriptionApplicationErrors.ChangedConcurrently());
        }

        try
        {
            mutation(subscription, this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (WatchSubscriptionValidationException exception)
        {
            return Invalid<WatchSubscriptionResult>(exception);
        }

        if (subscription.Version != expectedVersion)
        {
            WatchSubscriptionWriteOutcome write = await this.repository.ReplaceAsync(
                subscription,
                expectedVersion,
                cancellationToken);
            if (write != WatchSubscriptionWriteOutcome.Success)
            {
                return ApplicationResult<WatchSubscriptionResult>.Failure(
                    WatchSubscriptionApplicationErrors.ChangedConcurrently());
            }
        }

        UserCollectionTargetSnapshot target = await this.targetReader.ResolveAsync(
            subscription.TargetType,
            subscription.TargetId,
            cancellationToken);
        return ApplicationResult<WatchSubscriptionResult>.Success(ToResult(subscription, target));
    }

    private async Task<IReadOnlyDictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>>>
        ResolveTargetsAsync(
            IReadOnlyCollection<WatchSubscription> subscriptions,
            CancellationToken cancellationToken)
    {
        Dictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> result = new();
        foreach (IGrouping<CollectionTargetType, WatchSubscription> group in subscriptions.GroupBy(
            static subscription => subscription.TargetType))
        {
            result[group.Key] = await this.targetReader.ResolveAsync(
                group.Key,
                group.Select(static subscription => subscription.TargetId).ToArray(),
                cancellationToken);
        }

        return result;
    }

    private static UserCollectionTargetSnapshot FindSnapshot(
        IReadOnlyDictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> snapshots,
        WatchSubscription subscription)
    {
        return snapshots.TryGetValue(subscription.TargetType, out IReadOnlyDictionary<string, UserCollectionTargetSnapshot>? byId)
            && byId.TryGetValue(subscription.TargetId, out UserCollectionTargetSnapshot? snapshot)
                ? snapshot
                : new UserCollectionTargetSnapshot(
                    subscription.TargetType,
                    subscription.TargetId,
                    CollectionTargetStatus.Unknown,
                    null,
                    null,
                    null,
                    null);
    }

    private static WatchSubscriptionResult ToResult(
        WatchSubscription subscription,
        UserCollectionTargetSnapshot target)
    {
        return new WatchSubscriptionResult(
            subscription.Id.Value,
            subscription.TargetType,
            subscription.TargetId,
            target.Name,
            target.ParentParkId,
            target.ParentParkName,
            target.MainImageId,
            subscription.EventTypes.OrderBy(static type => type).ToArray(),
            subscription.Frequency,
            subscription.Channels.OrderBy(static channel => channel).ToArray(),
            subscription.IsPaused,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc,
            subscription.Version);
    }

    private static bool TryNormalizeMutation(
        string userId,
        string subscriptionId,
        long expectedVersion,
        out string normalizedUserId,
        out WatchSubscriptionId parsedId)
    {
        normalizedUserId = string.Empty;
        parsedId = default;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        }
        catch (ArgumentException)
        {
            return false;
        }

        return WatchSubscriptionId.TryParse(subscriptionId, out parsedId) && expectedVersion > 0;
    }

    private static ApplicationResult<TResult> Invalid<TResult>(
        WatchSubscriptionValidationException exception)
    {
        return ApplicationResult<TResult>.Failure(
            WatchSubscriptionApplicationErrors.Invalid(exception.Code, exception.Message));
    }
}
