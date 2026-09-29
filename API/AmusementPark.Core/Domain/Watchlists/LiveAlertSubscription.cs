using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Core.Domain.Watchlists;

public sealed class LiveAlertSubscription
{
    public const int MaximumSubscriptionsPerUser = 50;
    public const int MinimumDurationMinutes = 30;
    public const int MaximumDurationMinutes = 720;
    public const int CooldownMinutes = 30;
    public const int WaitHysteresisMinutes = 5;

    private LiveAlertSubscription(
        LiveAlertSubscriptionId id,
        string userId,
        string targetId,
        string parkId,
        LiveAlertType type,
        int? thresholdMinutes,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        DateTime? lastObservedAtUtc,
        LiveOperationalStatus? lastStatus,
        int? lastWaitMinutes,
        bool isArmed,
        DateTime? lastTriggeredAtUtc,
        LiveAlertTrigger? pendingTrigger,
        long version)
    {
        _ = id.Value;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.TargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        ValidateThreshold(type, thresholdMinutes);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (lastObservedAtUtc.HasValue)
        {
            EnsureUtc(lastObservedAtUtc.Value, nameof(lastObservedAtUtc));
        }

        if (lastTriggeredAtUtc.HasValue)
        {
            EnsureUtc(lastTriggeredAtUtc.Value, nameof(lastTriggeredAtUtc));
        }

        if (pendingTrigger is not null)
        {
            ValidatePendingTrigger(
                pendingTrigger,
                type,
                thresholdMinutes,
                lastObservedAtUtc,
                lastTriggeredAtUtc);
        }

        if (expiresAtUtc <= createdAtUtc || version < 1)
        {
            throw new ArgumentException("The live alert lifecycle is invalid.");
        }

        this.Id = id;
        this.Type = type;
        this.ThresholdMinutes = thresholdMinutes;
        this.CreatedAtUtc = createdAtUtc;
        this.ExpiresAtUtc = expiresAtUtc;
        this.LastObservedAtUtc = lastObservedAtUtc;
        this.LastStatus = lastStatus;
        this.LastWaitMinutes = lastWaitMinutes;
        this.IsArmed = isArmed;
        this.LastTriggeredAtUtc = lastTriggeredAtUtc;
        this.PendingTrigger = pendingTrigger;
        this.Version = version;
    }

    public LiveAlertSubscriptionId Id { get; }

    public string UserId { get; }

    public string TargetId { get; }

    public string ParkId { get; }

    public LiveAlertType Type { get; }

    public int? ThresholdMinutes { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime ExpiresAtUtc { get; }

    public DateTime? LastObservedAtUtc { get; private set; }

    public LiveOperationalStatus? LastStatus { get; private set; }

    public int? LastWaitMinutes { get; private set; }

    public bool IsArmed { get; private set; }

    public DateTime? LastTriggeredAtUtc { get; private set; }

    public LiveAlertTrigger? PendingTrigger { get; private set; }

    public long Version { get; private set; }

    public bool IsActive(DateTime nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        return nowUtc < this.ExpiresAtUtc;
    }

    public static LiveAlertSubscription Create(
        LiveAlertSubscriptionId id,
        string userId,
        string targetId,
        string parkId,
        LiveAlertType type,
        int? thresholdMinutes,
        DateTime nowUtc,
        DateTime expiresAtUtc,
        LiveLatestObservation seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (!string.Equals(seed.Target.Id, targetId, StringComparison.Ordinal)
            || !string.Equals(seed.Target.ParkId, parkId, StringComparison.Ordinal))
        {
            throw new ArgumentException("The live alert seed must match its target.", nameof(seed));
        }

        int? waitMinutes = ResolveWaitMinutes(seed);
        return new LiveAlertSubscription(
            id,
            userId,
            targetId,
            parkId,
            type,
            thresholdMinutes,
            nowUtc,
            expiresAtUtc,
            seed.Provenance.ObservedAtUtc,
            seed.Status,
            waitMinutes,
            ResolveArmed(type, thresholdMinutes, seed.Status, waitMinutes),
            null,
            null,
            1);
    }

    public static LiveAlertSubscription Restore(
        LiveAlertSubscriptionId id,
        string userId,
        string targetId,
        string parkId,
        LiveAlertType type,
        int? thresholdMinutes,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        DateTime? lastObservedAtUtc,
        LiveOperationalStatus? lastStatus,
        int? lastWaitMinutes,
        bool isArmed,
        DateTime? lastTriggeredAtUtc,
        LiveAlertTrigger? pendingTrigger,
        long version)
    {
        return new LiveAlertSubscription(
            id,
            userId,
            targetId,
            parkId,
            type,
            thresholdMinutes,
            createdAtUtc,
            expiresAtUtc,
            lastObservedAtUtc,
            lastStatus,
            lastWaitMinutes,
            isArmed,
            lastTriggeredAtUtc,
            pendingTrigger,
            version);
    }

    public LiveAlertTrigger? Evaluate(LiveLatestObservation observation, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(observation);
        EnsureUtc(nowUtc, nameof(nowUtc));
        if (!this.IsActive(nowUtc)
            || !string.Equals(observation.Target.Id, this.TargetId, StringComparison.Ordinal)
            || !string.Equals(observation.Target.ParkId, this.ParkId, StringComparison.Ordinal)
            || this.PendingTrigger is not null
            || this.LastObservedAtUtc >= observation.Provenance.ObservedAtUtc)
        {
            return null;
        }

        LiveFreshnessAssessment freshness = observation.FreshnessPolicy.Assess(
            observation.Provenance.ObservedAtUtc,
            nowUtc);
        if (freshness.State != LiveFreshnessState.Fresh || !freshness.Age.HasValue)
        {
            return null;
        }

        int? waitMinutes = ResolveWaitMinutes(observation);
        bool canTrigger = !this.LastTriggeredAtUtc.HasValue
            || nowUtc >= this.LastTriggeredAtUtc.Value.AddMinutes(CooldownMinutes);
        bool triggered = this.Type switch
        {
            LiveAlertType.Reopened => this.IsArmed && canTrigger
                && IsClosedForReopening(this.LastStatus)
                && observation.Status == LiveOperationalStatus.Open,
            LiveAlertType.Degraded => this.IsArmed && canTrigger
                && IsDegraded(observation.Status),
            LiveAlertType.WaitBelow => this.IsArmed && canTrigger
                && IsWaitOperational(observation.Status)
                && waitMinutes.HasValue && waitMinutes.Value <= this.ThresholdMinutes,
            LiveAlertType.WaitAbove => this.IsArmed && canTrigger
                && IsWaitOperational(observation.Status)
                && waitMinutes.HasValue && waitMinutes.Value >= this.ThresholdMinutes,
            _ => false,
        };
        LiveAlertTrigger? trigger = triggered
            ? new LiveAlertTrigger(
                this.Type,
                this.LastStatus,
                observation.Status,
                this.LastWaitMinutes,
                waitMinutes,
                this.ThresholdMinutes,
                observation.Provenance.SourceId,
                observation.Provenance.ObservedAtUtc,
                nowUtc,
                checked((long)Math.Floor(freshness.Age.Value.TotalSeconds)))
            : null;

        this.LastObservedAtUtc = observation.Provenance.ObservedAtUtc;
        this.LastStatus = observation.Status;
        this.LastWaitMinutes = waitMinutes;
        this.IsArmed = ResolveNextArmed(triggered, observation.Status, waitMinutes);
        if (triggered)
        {
            this.LastTriggeredAtUtc = nowUtc;
            this.PendingTrigger = trigger;
        }

        this.Version++;
        return trigger;
    }

    public void MarkPendingTriggerDelivered()
    {
        if (this.PendingTrigger is null)
        {
            return;
        }

        this.PendingTrigger = null;
        this.Version++;
    }

    private bool ResolveNextArmed(
        bool triggered,
        LiveOperationalStatus status,
        int? waitMinutes)
    {
        if (triggered)
        {
            return false;
        }

        return this.Type switch
        {
            LiveAlertType.Reopened => IsClosedForReopening(status),
            LiveAlertType.Degraded => !IsDegraded(status),
            LiveAlertType.WaitBelow when waitMinutes.HasValue =>
                this.IsArmed || waitMinutes.Value >= this.ThresholdMinutes + WaitHysteresisMinutes,
            LiveAlertType.WaitAbove when waitMinutes.HasValue =>
                this.IsArmed || waitMinutes.Value <= this.ThresholdMinutes - WaitHysteresisMinutes,
            _ => this.IsArmed,
        };
    }

    private static bool ResolveArmed(
        LiveAlertType type,
        int? thresholdMinutes,
        LiveOperationalStatus status,
        int? waitMinutes)
    {
        return type switch
        {
            LiveAlertType.Reopened => IsClosedForReopening(status),
            LiveAlertType.Degraded => !IsDegraded(status),
            LiveAlertType.WaitBelow => waitMinutes >= thresholdMinutes + WaitHysteresisMinutes,
            LiveAlertType.WaitAbove => waitMinutes <= thresholdMinutes - WaitHysteresisMinutes,
            _ => false,
        };
    }

    private static bool IsDegraded(LiveOperationalStatus status)
    {
        return status is LiveOperationalStatus.Delayed
            or LiveOperationalStatus.Down
            or LiveOperationalStatus.OperatingWithLimitations;
    }

    private static bool IsClosedForReopening(LiveOperationalStatus? status)
    {
        return status is LiveOperationalStatus.Closed
            or LiveOperationalStatus.TemporarilyClosed
            or LiveOperationalStatus.Down
            or LiveOperationalStatus.WeatherClosed
            or LiveOperationalStatus.Maintenance
            or LiveOperationalStatus.NotOperatingToday;
    }

    private static bool IsWaitOperational(LiveOperationalStatus status)
    {
        return status is LiveOperationalStatus.Open
            or LiveOperationalStatus.Delayed
            or LiveOperationalStatus.OperatingWithLimitations;
    }

    private static int? ResolveWaitMinutes(LiveLatestObservation observation)
    {
        if (!IsWaitOperational(observation.Status))
        {
            return null;
        }

        return observation.Queues
            .Where(static queue => queue.Kind == LiveQueueKind.Standby)
            .Select(static queue => queue.WaitTimeMinutes)
            .FirstOrDefault(static wait => wait.HasValue);
    }

    private static void ValidateThreshold(LiveAlertType type, int? thresholdMinutes)
    {
        bool thresholdRequired = type is LiveAlertType.WaitBelow or LiveAlertType.WaitAbove;
        if (thresholdRequired != thresholdMinutes.HasValue
            || thresholdMinutes is < 5 or > 300)
        {
            throw new ArgumentException("A wait alert requires a threshold between 5 and 300 minutes.", nameof(thresholdMinutes));
        }
    }

    private static void ValidatePendingTrigger(
        LiveAlertTrigger trigger,
        LiveAlertType type,
        int? thresholdMinutes,
        DateTime? lastObservedAtUtc,
        DateTime? lastTriggeredAtUtc)
    {
        if (trigger.Type != type
            || trigger.ThresholdMinutes != thresholdMinutes
            || !Enum.IsDefined(trigger.CurrentStatus)
            || trigger.AgeSeconds < 0
            || trigger.ObservedAtUtc != lastObservedAtUtc
            || trigger.TriggeredAtUtc != lastTriggeredAtUtc)
        {
            throw new ArgumentException("The pending live alert trigger is inconsistent.", nameof(trigger));
        }

        EnsureUtc(trigger.ObservedAtUtc, nameof(trigger));
        EnsureUtc(trigger.TriggeredAtUtc, nameof(trigger));
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Live alert timestamps must use UTC.", parameterName);
        }
    }
}
