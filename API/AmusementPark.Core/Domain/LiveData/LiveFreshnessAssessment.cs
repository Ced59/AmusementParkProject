namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveFreshnessAssessment
{
    internal LiveFreshnessAssessment(
        LiveFreshnessState state,
        LiveFreshnessReason reason,
        TimeSpan? age,
        DateTime? expiresAtUtc)
    {
        this.State = state;
        this.Reason = reason;
        this.Age = age;
        this.ExpiresAtUtc = expiresAtUtc;
    }

    public LiveFreshnessState State { get; }

    public LiveFreshnessReason Reason { get; }

    public TimeSpan? Age { get; }

    public DateTime? ExpiresAtUtc { get; }

    public bool CanBePresentedAsCurrent => this.State is LiveFreshnessState.Fresh
        or LiveFreshnessState.Aging
        or LiveFreshnessState.Stale;
}
