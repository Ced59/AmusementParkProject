using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Watchlists;

public sealed class NotificationDigest
{
    public const int MaximumEntries = 2000;

    private NotificationDigest(
        NotificationDigestId id,
        string userId,
        NotificationChannel channel,
        NotificationFrequency frequency,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        IReadOnlyCollection<NotificationDigestEntry> entries,
        int observedNotificationCount,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        NotificationDigestPeriodResolver.ValidateGroup(channel, frequency, periodStartUtc);
        DateTime expectedEnd = NotificationDigestPeriodResolver.ResolveEnd(frequency, periodStartUtc);
        if (periodEndUtc != expectedEnd
            || createdAtUtc.Kind != DateTimeKind.Utc
            || updatedAtUtc.Kind != DateTimeKind.Utc
            || updatedAtUtc < createdAtUtc)
        {
            throw new ArgumentException("The notification digest timestamps are invalid.");
        }

        ArgumentNullException.ThrowIfNull(entries);
        NotificationDigestEntry[] normalizedEntries = entries
            .GroupBy(static entry => entry.DeduplicationKey, StringComparer.Ordinal)
            .Select(static group => group
                .OrderByDescending(static entry => entry.SourceRevision)
                .ThenByDescending(static entry => entry.OccurredAtUtc)
                .ThenBy(static entry => entry.FactualEventId.Value, StringComparer.Ordinal)
                .First())
            .OrderBy(static entry => entry.DeduplicationKey, StringComparer.Ordinal)
            .ToArray();
        if (normalizedEntries.Length > MaximumEntries
            || observedNotificationCount < normalizedEntries.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(entries), "The notification digest size is invalid.");
        }

        this.Id = id;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.Channel = channel;
        this.Frequency = frequency;
        this.PeriodStartUtc = periodStartUtc;
        this.PeriodEndUtc = periodEndUtc;
        this.Entries = normalizedEntries;
        this.ObservedNotificationCount = observedNotificationCount;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public NotificationDigestId Id { get; }

    public string UserId { get; }

    public NotificationChannel Channel { get; }

    public NotificationFrequency Frequency { get; }

    public DateTime PeriodStartUtc { get; }

    public DateTime PeriodEndUtc { get; }

    public IReadOnlyCollection<NotificationDigestEntry> Entries { get; }

    public int ObservedNotificationCount { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; }

    public static NotificationDigest CreateSnapshot(
        string userId,
        NotificationChannel channel,
        NotificationFrequency frequency,
        DateTime periodStartUtc,
        IReadOnlyCollection<NotificationDigestEntry> entries,
        int observedNotificationCount,
        DateTime nowUtc)
    {
        NotificationDigestId id = NotificationDigestId.ForGroup(
            userId,
            channel,
            frequency,
            periodStartUtc);
        return new NotificationDigest(
            id,
            userId,
            channel,
            frequency,
            periodStartUtc,
            NotificationDigestPeriodResolver.ResolveEnd(frequency, periodStartUtc),
            entries,
            observedNotificationCount,
            nowUtc,
            nowUtc);
    }

    public static NotificationDigest Restore(
        NotificationDigestId id,
        string userId,
        NotificationChannel channel,
        NotificationFrequency frequency,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        IReadOnlyCollection<NotificationDigestEntry> entries,
        int observedNotificationCount,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new NotificationDigest(
            id,
            userId,
            channel,
            frequency,
            periodStartUtc,
            periodEndUtc,
            entries,
            observedNotificationCount,
            createdAtUtc,
            updatedAtUtc);
    }
}
