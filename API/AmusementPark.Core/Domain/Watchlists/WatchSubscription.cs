using System.Collections.Frozen;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

/// <summary>
/// Explicit private request to follow selected factual changes for one target.
/// A collection entry never creates this subscription implicitly.
/// </summary>
public sealed class WatchSubscription
{
    public const int MaximumSubscriptionsPerUser = 250;

    private WatchSubscription(
        WatchSubscriptionId id,
        string userId,
        CollectionTargetType targetType,
        string targetId,
        IReadOnlyCollection<FactualEventType> eventTypes,
        NotificationFrequency frequency,
        IReadOnlyCollection<NotificationChannel> channels,
        bool isPaused,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        _ = id.Value;
        ValidateTargetType(targetType);
        FrozenSet<FactualEventType> normalizedEventTypes = NormalizeEventTypes(
            targetType,
            eventTypes);
        ValidateFrequency(frequency);
        FrozenSet<NotificationChannel> normalizedChannels = NormalizeChannels(channels);
        ValidateDeliveryPreference(frequency, normalizedChannels);
        ValidateTimestamps(createdAtUtc, updatedAtUtc);
        if (version < 1)
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.InvalidVersion,
                "The watch subscription version must be positive.");
        }

        this.Id = id;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.TargetType = targetType;
        this.TargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        this.EventTypes = normalizedEventTypes;
        this.Frequency = frequency;
        this.Channels = normalizedChannels;
        this.IsPaused = isPaused;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public WatchSubscriptionId Id { get; }

    public string UserId { get; }

    public CollectionTargetType TargetType { get; }

    public string TargetId { get; }

    public IReadOnlySet<FactualEventType> EventTypes { get; private set; }

    public NotificationFrequency Frequency { get; private set; }

    public IReadOnlySet<NotificationChannel> Channels { get; private set; }

    public bool IsPaused { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static WatchSubscription Create(
        WatchSubscriptionId id,
        string userId,
        CollectionTargetType targetType,
        string targetId,
        IReadOnlyCollection<FactualEventType> eventTypes,
        NotificationFrequency frequency,
        IReadOnlyCollection<NotificationChannel> channels,
        DateTime nowUtc)
    {
        return new WatchSubscription(
            id,
            userId,
            targetType,
            targetId,
            eventTypes,
            frequency,
            channels,
            false,
            nowUtc,
            nowUtc,
            1);
    }

    public static WatchSubscription Restore(
        WatchSubscriptionId id,
        string userId,
        CollectionTargetType targetType,
        string targetId,
        IReadOnlyCollection<FactualEventType> eventTypes,
        NotificationFrequency frequency,
        IReadOnlyCollection<NotificationChannel> channels,
        bool isPaused,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        return new WatchSubscription(
            id,
            userId,
            targetType,
            targetId,
            eventTypes,
            frequency,
            channels,
            isPaused,
            createdAtUtc,
            updatedAtUtc,
            version);
    }

    public void UpdatePreferences(
        IReadOnlyCollection<FactualEventType> eventTypes,
        NotificationFrequency frequency,
        IReadOnlyCollection<NotificationChannel> channels,
        DateTime nowUtc)
    {
        FrozenSet<FactualEventType> normalizedEventTypes = NormalizeEventTypes(
            this.TargetType,
            eventTypes);
        ValidateFrequency(frequency);
        FrozenSet<NotificationChannel> normalizedChannels = NormalizeChannels(channels);
        ValidateDeliveryPreference(frequency, normalizedChannels);
        this.ValidateMutationTimestamp(nowUtc);
        if (this.EventTypes.SetEquals(normalizedEventTypes)
            && this.Frequency == frequency
            && this.Channels.SetEquals(normalizedChannels))
        {
            return;
        }

        this.PrepareMutation();
        this.EventTypes = normalizedEventTypes;
        this.Frequency = frequency;
        this.Channels = normalizedChannels;
        this.CommitMutation(nowUtc);
    }

    public void Pause(DateTime nowUtc)
    {
        this.SetPaused(true, nowUtc);
    }

    public void Resume(DateTime nowUtc)
    {
        this.SetPaused(false, nowUtc);
    }

    public bool Accepts(FactualEventType eventType)
    {
        ValidateEventType(eventType);
        return !this.IsPaused && this.EventTypes.Contains(eventType);
    }

    public bool Accepts(FactualChangeEvent factualEvent)
    {
        ArgumentNullException.ThrowIfNull(factualEvent);
        if (!factualEvent.CanBeDistributed || !this.Accepts(factualEvent.Type))
        {
            return false;
        }

        return this.TargetType switch
        {
            CollectionTargetType.Park => factualEvent.Target.Type == FactualTargetType.Park
                ? string.Equals(this.TargetId, factualEvent.Target.TargetId, StringComparison.Ordinal)
                : string.Equals(this.TargetId, factualEvent.Target.ParentParkId, StringComparison.Ordinal),
            CollectionTargetType.ParkItem => factualEvent.Target.Type == FactualTargetType.ParkItem
                && string.Equals(this.TargetId, factualEvent.Target.TargetId, StringComparison.Ordinal),
            _ => false,
        };
    }

    public bool HasSameLogicalIdentityAs(WatchSubscription other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return string.Equals(this.UserId, other.UserId, StringComparison.Ordinal)
            && this.TargetType == other.TargetType
            && string.Equals(this.TargetId, other.TargetId, StringComparison.Ordinal);
    }

    private static void ValidateTargetType(CollectionTargetType targetType)
    {
        if (!Enum.IsDefined(targetType))
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.InvalidTargetType,
                "The watch subscription target type is invalid.");
        }
    }

    private static FrozenSet<FactualEventType> NormalizeEventTypes(
        CollectionTargetType targetType,
        IReadOnlyCollection<FactualEventType> eventTypes)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);
        if (eventTypes.Count == 0)
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.EmptyEventTypes,
                "A watch subscription must select at least one factual event type.");
        }

        foreach (FactualEventType eventType in eventTypes)
        {
            ValidateEventType(eventType);
            FactualEventDefinition definition = FactualEventCatalog.Get(eventType);
            if (targetType == CollectionTargetType.ParkItem
                && !definition.Supports(FactualTargetType.ParkItem))
            {
                throw CreateValidationException(
                    WatchSubscriptionErrorCodes.IncompatibleEventType,
                    "A park item subscription cannot select a park-only factual event.");
            }
        }

        return eventTypes.ToFrozenSet();
    }

    private static void ValidateEventType(FactualEventType eventType)
    {
        if (!Enum.IsDefined(eventType))
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.InvalidEventType,
                "The selected factual event type is invalid.");
        }
    }

    private static void ValidateFrequency(NotificationFrequency frequency)
    {
        if (!Enum.IsDefined(frequency))
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.InvalidFrequency,
                "The notification frequency is invalid.");
        }
    }

    private static FrozenSet<NotificationChannel> NormalizeChannels(
        IReadOnlyCollection<NotificationChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(channels);
        foreach (NotificationChannel channel in channels)
        {
            if (!Enum.IsDefined(channel))
            {
                throw CreateValidationException(
                    WatchSubscriptionErrorCodes.InvalidChannel,
                    "The selected notification channel is invalid.");
            }
        }

        return channels.ToFrozenSet();
    }

    private static void ValidateDeliveryPreference(
        NotificationFrequency frequency,
        IReadOnlySet<NotificationChannel> channels)
    {
        if (frequency == NotificationFrequency.WebOnly && channels.Count > 0)
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.IncompatibleDeliveryPreference,
                "A Web-only subscription cannot request an external delivery channel.");
        }
    }

    private static void ValidateTimestamps(DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.InvalidTimestamp,
                "The watch subscription timestamps are not chronologically consistent.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.InvalidTimestamp,
                "Watch subscription timestamps must be expressed in UTC.");
        }
    }

    private void SetPaused(bool isPaused, DateTime nowUtc)
    {
        this.ValidateMutationTimestamp(nowUtc);
        if (this.IsPaused == isPaused)
        {
            return;
        }

        this.PrepareMutation();
        this.IsPaused = isPaused;
        this.CommitMutation(nowUtc);
    }

    private void ValidateMutationTimestamp(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.InvalidTimestamp,
                "A watch subscription mutation cannot predate its current state.");
        }
    }

    private void PrepareMutation()
    {
        if (this.Version == long.MaxValue)
        {
            throw CreateValidationException(
                WatchSubscriptionErrorCodes.InvalidVersion,
                "The watch subscription version cannot be incremented further.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static WatchSubscriptionValidationException CreateValidationException(
        string code,
        string message)
    {
        return new WatchSubscriptionValidationException(code, message);
    }
}
