namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveHistoryRetentionPolicy
{
    public static readonly TimeSpan MaximumRawRetention = TimeSpan.FromDays(30);
    public static readonly TimeSpan MaximumAggregateRetention = TimeSpan.FromDays(730);
    public static readonly TimeSpan MaximumBucketDuration = TimeSpan.FromDays(1);

    public LiveHistoryRetentionPolicy(
        TimeSpan rawRetention,
        TimeSpan aggregateRetention,
        TimeSpan bucketDuration)
    {
        if (rawRetention <= TimeSpan.Zero || rawRetention > MaximumRawRetention)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidDuration,
                "The raw live history retention must be positive and cannot exceed 30 days.",
                nameof(rawRetention));
        }

        if (aggregateRetention < rawRetention
            || aggregateRetention > MaximumAggregateRetention)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidDuration,
                "The aggregate live history retention must cover the raw retention and cannot exceed 730 days.",
                nameof(aggregateRetention));
        }

        if (bucketDuration <= TimeSpan.Zero
            || bucketDuration > MaximumBucketDuration
            || TimeSpan.TicksPerDay % bucketDuration.Ticks != 0)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidDuration,
                "The live history bucket duration must divide a UTC day and cannot exceed one day.",
                nameof(bucketDuration));
        }

        this.RawRetention = rawRetention;
        this.AggregateRetention = aggregateRetention;
        this.BucketDuration = bucketDuration;
        this.StorageKey = FormattableString.Invariant(
            $"{rawRetention.Ticks}:{aggregateRetention.Ticks}:{bucketDuration.Ticks}");
    }

    public TimeSpan RawRetention { get; }

    public TimeSpan AggregateRetention { get; }

    public TimeSpan BucketDuration { get; }

    public string StorageKey { get; }

    public DateTime GetBucketStartUtc(DateTime observedAtUtc)
    {
        EnsureUtc(observedAtUtc, nameof(observedAtUtc));
        long bucketTicks = observedAtUtc.Ticks
            - (observedAtUtc.Ticks % this.BucketDuration.Ticks);
        return new DateTime(bucketTicks, DateTimeKind.Utc);
    }

    public DateTime GetRawExpirationUtc(DateTime normalizedAtUtc)
    {
        EnsureUtc(normalizedAtUtc, nameof(normalizedAtUtc));
        return normalizedAtUtc.Add(this.RawRetention);
    }

    public DateTime GetAggregateExpirationUtc(DateTime bucketStartUtc)
    {
        EnsureUtc(bucketStartUtc, nameof(bucketStartUtc));
        return bucketStartUtc.Add(this.BucketDuration).Add(this.AggregateRetention);
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "Live history timestamps must be expressed in UTC.",
                parameterName);
        }
    }

    private static LiveDataValidationException Invalid(
        string code,
        string message,
        string parameterName)
    {
        return new LiveDataValidationException(
            code,
            message,
            parameterName);
    }
}
