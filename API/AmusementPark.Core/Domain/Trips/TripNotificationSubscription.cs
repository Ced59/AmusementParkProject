using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripNotificationSubscription
{
    private TripNotificationSubscription(
        string id,
        TripPlanId tripPlanId,
        TripMemberId memberId,
        string userId,
        bool isEnabled,
        long seenThroughSequence,
        IEnumerable<string>? pendingOperationKeys,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        this.Id = IdentifierRules.NormalizeRequired(id, nameof(id));
        _ = tripPlanId.Value;
        _ = memberId.Value;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        if (seenThroughSequence < 0
            || version < 1
            || createdAtUtc.Kind != DateTimeKind.Utc
            || updatedAtUtc.Kind != DateTimeKind.Utc
            || updatedAtUtc < createdAtUtc)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip notification subscription is invalid.");
        }

        this.TripPlanId = tripPlanId;
        this.MemberId = memberId;
        this.IsEnabled = isEnabled;
        this.SeenThroughSequence = seenThroughSequence;
        this.PendingOperationKeys = new TripNotificationBoundary(
            seenThroughSequence,
            pendingOperationKeys).PendingOperationKeys;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.Version = version;
    }

    public string Id { get; }

    public TripPlanId TripPlanId { get; }

    public TripMemberId MemberId { get; }

    public string UserId { get; }

    public bool IsEnabled { get; private set; }

    public long SeenThroughSequence { get; private set; }

    public IReadOnlyCollection<string> PendingOperationKeys { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static TripNotificationSubscription CreateEnabled(
        TripPlanId tripPlanId,
        TripMemberId memberId,
        string userId,
        TripNotificationBoundary boundary,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        return new TripNotificationSubscription(
            Guid.NewGuid().ToString("N"),
            tripPlanId,
            memberId,
            userId,
            true,
            boundary.Sequence,
            boundary.PendingOperationKeys,
            nowUtc,
            nowUtc,
            1);
    }

    public static TripNotificationSubscription Restore(
        string id,
        TripPlanId tripPlanId,
        TripMemberId memberId,
        string userId,
        bool isEnabled,
        long seenThroughSequence,
        IEnumerable<string>? pendingOperationKeys,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        long version)
    {
        return new TripNotificationSubscription(
            id,
            tripPlanId,
            memberId,
            userId,
            isEnabled,
            seenThroughSequence,
            pendingOperationKeys,
            createdAtUtc,
            updatedAtUtc,
            version);
    }

    public void SetEnabled(
        bool enabled,
        TripNotificationBoundary boundary,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        this.ValidateMutation(boundary.Sequence, nowUtc);
        if (this.IsEnabled == enabled)
        {
            return;
        }

        this.IsEnabled = enabled;
        if (enabled)
        {
            this.ApplyBoundary(boundary);
        }
        else
        {
            this.PendingOperationKeys = Array.Empty<string>();
        }

        this.CommitMutation(nowUtc);
    }

    public void MarkSeenThrough(TripNotificationBoundary boundary, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        this.ValidateMutation(boundary.Sequence, nowUtc);
        if (!this.IsEnabled
            || boundary.Sequence < this.SeenThroughSequence
            || (boundary.Sequence == this.SeenThroughSequence
                && boundary.PendingOperationKeys.SequenceEqual(
                    this.PendingOperationKeys,
                    StringComparer.Ordinal)))
        {
            return;
        }

        this.ApplyBoundary(boundary);
        this.CommitMutation(nowUtc);
    }

    private void ApplyBoundary(TripNotificationBoundary boundary)
    {
        this.SeenThroughSequence = Math.Max(this.SeenThroughSequence, boundary.Sequence);
        this.PendingOperationKeys = boundary.PendingOperationKeys.ToArray();
    }

    private void ValidateMutation(long currentSequence, DateTime nowUtc)
    {
        if (currentSequence < 0 || nowUtc.Kind != DateTimeKind.Utc)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip notification mutation is invalid.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.UpdatedAtUtc = nowUtc < this.UpdatedAtUtc
            ? this.UpdatedAtUtc
            : nowUtc;
        this.Version++;
    }
}
