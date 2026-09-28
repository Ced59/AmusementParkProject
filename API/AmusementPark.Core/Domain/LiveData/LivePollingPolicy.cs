namespace AmusementPark.Core.Domain.LiveData;

public sealed class LivePollingPolicy
{
    public static readonly TimeSpan MinimumPollingInterval = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MaximumDelay = TimeSpan.FromDays(30);

    public LivePollingPolicy(
        TimeSpan pollingInterval,
        TimeSpan initialFailureBackoff,
        TimeSpan maximumFailureBackoff,
        int circuitBreakerFailureThreshold,
        TimeSpan circuitBreakDuration)
    {
        ValidateDuration(
            pollingInterval,
            MinimumPollingInterval,
            MaximumDelay,
            nameof(pollingInterval));
        ValidateDuration(
            initialFailureBackoff,
            MinimumPollingInterval,
            MaximumDelay,
            nameof(initialFailureBackoff));
        ValidateDuration(
            maximumFailureBackoff,
            initialFailureBackoff,
            MaximumDelay,
            nameof(maximumFailureBackoff));
        if (circuitBreakerFailureThreshold is < 1 or > 100)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidPollingPolicy,
                "The circuit breaker threshold must be between 1 and 100.",
                nameof(circuitBreakerFailureThreshold));
        }

        ValidateDuration(
            circuitBreakDuration,
            MinimumPollingInterval,
            MaximumDelay,
            nameof(circuitBreakDuration));

        this.PollingInterval = pollingInterval;
        this.InitialFailureBackoff = initialFailureBackoff;
        this.MaximumFailureBackoff = maximumFailureBackoff;
        this.CircuitBreakerFailureThreshold = circuitBreakerFailureThreshold;
        this.CircuitBreakDuration = circuitBreakDuration;
    }

    public TimeSpan PollingInterval { get; }

    public TimeSpan InitialFailureBackoff { get; }

    public TimeSpan MaximumFailureBackoff { get; }

    public int CircuitBreakerFailureThreshold { get; }

    public TimeSpan CircuitBreakDuration { get; }

    public LivePollingSchedule PlanNext(
        DateTime attemptedAtUtc,
        LivePollingAttemptOutcome outcome,
        int currentConsecutiveFailures,
        TimeSpan retryAfter,
        TimeSpan jitter)
    {
        ValidateUtc(attemptedAtUtc, nameof(attemptedAtUtc));
        ValidateEnum(outcome, nameof(outcome));
        if (currentConsecutiveFailures < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentConsecutiveFailures));
        }

        if (retryAfter < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAfter));
        }

        if (jitter < TimeSpan.Zero || jitter > this.PollingInterval)
        {
            throw new ArgumentOutOfRangeException(nameof(jitter));
        }

        if (outcome is LivePollingAttemptOutcome.Success or LivePollingAttemptOutcome.NotModified)
        {
            return new LivePollingSchedule(
                AddSafely(attemptedAtUtc, this.PollingInterval, jitter),
                0,
                null,
                false);
        }

        int failures = currentConsecutiveFailures == int.MaxValue
            ? int.MaxValue
            : currentConsecutiveFailures + 1;
        TimeSpan failureBackoff = this.CalculateFailureBackoff(failures);
        TimeSpan delay = Max(this.PollingInterval, failureBackoff, retryAfter);
        bool circuitOpened = failures >= this.CircuitBreakerFailureThreshold;
        DateTime? circuitOpenUntilUtc = circuitOpened
            ? AddSafely(attemptedAtUtc, this.CircuitBreakDuration, TimeSpan.Zero)
            : null;
        if (circuitOpened)
        {
            delay = Max(delay, this.CircuitBreakDuration);
        }

        return new LivePollingSchedule(
            AddSafely(attemptedAtUtc, delay, jitter),
            failures,
            circuitOpenUntilUtc,
            circuitOpened);
    }

    private TimeSpan CalculateFailureBackoff(int failures)
    {
        int exponent = Math.Min(Math.Max(failures - 1, 0), 30);
        double multiplier = Math.Pow(2, exponent);
        double ticks = Math.Min(
            this.InitialFailureBackoff.Ticks * multiplier,
            this.MaximumFailureBackoff.Ticks);
        return TimeSpan.FromTicks((long)ticks);
    }

    private static TimeSpan Max(params TimeSpan[] values)
    {
        return values.Max();
    }

    private static DateTime AddSafely(DateTime value, TimeSpan delay, TimeSpan jitter)
    {
        long remainingTicks = DateTime.MaxValue.Ticks - value.Ticks;
        if (delay.Ticks > remainingTicks || jitter.Ticks > remainingTicks - delay.Ticks)
        {
            return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        }

        return new DateTime(value.Ticks + delay.Ticks + jitter.Ticks, DateTimeKind.Utc);
    }

    private static void ValidateDuration(
        TimeSpan value,
        TimeSpan minimum,
        TimeSpan maximum,
        string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidPollingPolicy,
                $"A live polling duration must be between {minimum} and {maximum}.",
                parameterName);
        }
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "A live polling timestamp must be UTC.",
                parameterName);
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidEnum,
                "A live polling outcome is invalid.",
                parameterName);
        }
    }

    private static LiveDataValidationException Invalid(
        string code,
        string message,
        string? parameterName = null)
    {
        return new LiveDataValidationException(code, message, parameterName);
    }
}
