namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveLatestObservation
{
    public LiveLatestObservation(
        LiveTargetReference target,
        LiveOperationalStatus status,
        IReadOnlyCollection<LiveQueueObservation> queues,
        LiveObservationProvenance provenance,
        LiveFreshnessPolicy freshnessPolicy,
        string? payloadSha256)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(queues);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(freshnessPolicy);
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        List<LiveQueueObservation> normalizedQueues = queues.ToList();
        if (normalizedQueues.Any(static queue => queue is null)
            || normalizedQueues.GroupBy(static queue => queue.Kind).Any(static group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A latest live observation cannot contain duplicate or empty queues.",
                nameof(queues));
        }

        this.Target = target;
        this.Status = status;
        this.Queues = normalizedQueues.AsReadOnly();
        this.Provenance = provenance;
        this.FreshnessPolicy = freshnessPolicy;
        this.PayloadSha256 = NormalizeHash(payloadSha256);
    }

    public LiveTargetReference Target { get; }

    public LiveOperationalStatus Status { get; }

    public IReadOnlyCollection<LiveQueueObservation> Queues { get; }

    public LiveObservationProvenance Provenance { get; }

    public LiveFreshnessPolicy FreshnessPolicy { get; }

    public string? PayloadSha256 { get; }

    public bool HasStatusQueueConflict =>
        this.Status == LiveOperationalStatus.Closed
        && this.Queues.Any(static queue => queue.WaitTimeMinutes.HasValue);

    public int CompareRecencyTo(LiveLatestObservation other)
    {
        ArgumentNullException.ThrowIfNull(other);
        int observedComparison = this.Provenance.ObservedAtUtc.CompareTo(
            other.Provenance.ObservedAtUtc);
        return observedComparison != 0
            ? observedComparison
            : this.Provenance.ReceivedAtUtc.CompareTo(other.Provenance.ReceivedAtUtc);
    }

    private static string? NormalizeHash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim().ToLowerInvariant();
        if (normalizedValue.Length != 64
            || normalizedValue.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "A latest live observation payload hash must be a SHA-256 hexadecimal value.",
                nameof(value));
        }

        return normalizedValue;
    }
}
