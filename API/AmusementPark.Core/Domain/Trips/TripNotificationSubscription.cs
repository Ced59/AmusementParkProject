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

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static TripNotificationSubscription CreateEnabled(
        TripPlanId tripPlanId,
        TripMemberId memberId,
        string userId,
        long currentSequence,
        DateTime nowUtc)
    {
        return new TripNotificationSubscription(
            Guid.NewGuid().ToString("N"),
            tripPlanId,
            memberId,
            userId,
            true,
            currentSequence,
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
            createdAtUtc,
            updatedAtUtc,
            version);
    }

    public void SetEnabled(bool enabled, long currentSequence, DateTime nowUtc)
    {
        this.ValidateMutation(currentSequence, nowUtc);
        if (this.IsEnabled == enabled)
        {
            return;
        }

        this.IsEnabled = enabled;
        if (enabled)
        {
            this.SeenThroughSequence = currentSequence;
        }

        this.CommitMutation(nowUtc);
    }

    public void MarkSeenThrough(long currentSequence, DateTime nowUtc)
    {
        this.ValidateMutation(currentSequence, nowUtc);
        if (!this.IsEnabled || currentSequence <= this.SeenThroughSequence)
        {
            return;
        }

        this.SeenThroughSequence = currentSequence;
        this.CommitMutation(nowUtc);
    }

    private void ValidateMutation(long currentSequence, DateTime nowUtc)
    {
        if (currentSequence < 0 || nowUtc.Kind != DateTimeKind.Utc || nowUtc < this.UpdatedAtUtc)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip notification mutation is invalid.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.UpdatedAtUtc = nowUtc;
        this.Version++;
    }
}
