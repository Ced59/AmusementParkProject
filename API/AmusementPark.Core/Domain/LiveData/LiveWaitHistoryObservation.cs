namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitHistoryObservation
{
    public LiveWaitHistoryObservation(
        string externalTargetId,
        string mappingVersion,
        DateTime observedAtUtc,
        DateTime receivedAtUtc,
        LiveOperationalStatus status,
        IReadOnlyCollection<LiveQueueObservation> queues,
        bool isBucketTruncated)
    {
        if (string.IsNullOrWhiteSpace(externalTargetId))
        {
            throw new ArgumentException(
                "The external live target identifier is required.",
                nameof(externalTargetId));
        }

        if (string.IsNullOrWhiteSpace(mappingVersion))
        {
            throw new ArgumentException(
                "The live target mapping version is required.",
                nameof(mappingVersion));
        }

        EnsureUtc(observedAtUtc, nameof(observedAtUtc));
        EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        ArgumentNullException.ThrowIfNull(queues);
        this.ExternalTargetId = externalTargetId.Trim();
        this.MappingVersion = mappingVersion.Trim();
        this.ObservedAtUtc = observedAtUtc;
        this.ReceivedAtUtc = receivedAtUtc;
        this.Status = status;
        this.Queues = queues.ToArray();
        this.IsBucketTruncated = isBucketTruncated;
    }

    public string ExternalTargetId { get; }

    public string MappingVersion { get; }

    public DateTime ObservedAtUtc { get; }

    public DateTime ReceivedAtUtc { get; }

    public LiveOperationalStatus Status { get; }

    public IReadOnlyCollection<LiveQueueObservation> Queues { get; }

    public bool IsBucketTruncated { get; }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A live history timestamp must be UTC.", parameterName);
        }
    }
}
