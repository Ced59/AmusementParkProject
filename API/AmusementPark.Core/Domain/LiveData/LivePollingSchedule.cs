namespace AmusementPark.Core.Domain.LiveData;

public sealed class LivePollingSchedule
{
    public LivePollingSchedule(
        DateTime nextAttemptAtUtc,
        int consecutiveFailures,
        DateTime? circuitOpenUntilUtc,
        bool circuitOpened)
    {
        if (nextAttemptAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The next polling timestamp must be UTC.", nameof(nextAttemptAtUtc));
        }

        if (consecutiveFailures < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(consecutiveFailures));
        }

        if (circuitOpenUntilUtc is not null && circuitOpenUntilUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The circuit timestamp must be UTC.", nameof(circuitOpenUntilUtc));
        }

        this.NextAttemptAtUtc = nextAttemptAtUtc;
        this.ConsecutiveFailures = consecutiveFailures;
        this.CircuitOpenUntilUtc = circuitOpenUntilUtc;
        this.CircuitOpened = circuitOpened;
    }

    public DateTime NextAttemptAtUtc { get; }

    public int ConsecutiveFailures { get; }

    public DateTime? CircuitOpenUntilUtc { get; }

    public bool CircuitOpened { get; }
}
