using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripItemPreference
{
    public const int MaximumPreferencesPerMember = 2000;
    public const int MaximumBatchSize = 250;

    private TripItemPreference(
        TripItemPreferenceId id,
        TripPlanId tripPlanId,
        TripMemberId memberId,
        string userId,
        string parkItemId,
        TripItemPreferenceLevel level,
        TripItemPreferenceReason? reason,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        _ = id.Value;
        _ = tripPlanId.Value;
        _ = memberId.Value;
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string normalizedParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        ValidatePreference(level, reason);
        ValidateVersion(version);
        ValidateTimestamps(createdAtUtc, updatedAtUtc);

        this.Id = id;
        this.TripPlanId = tripPlanId;
        this.MemberId = memberId;
        this.UserId = normalizedUserId;
        this.ParkItemId = normalizedParkItemId;
        this.Level = level;
        this.Reason = level == TripItemPreferenceLevel.Unknown ? null : reason;
        this.Version = version;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public TripItemPreferenceId Id { get; }

    public TripPlanId TripPlanId { get; }

    public TripMemberId MemberId { get; }

    public string UserId { get; }

    public string ParkItemId { get; }

    public TripItemPreferenceLevel Level { get; private set; }

    public TripItemPreferenceReason? Reason { get; private set; }

    public long Version { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TripItemPreference Create(
        TripItemPreferenceId id,
        TripPlanId tripPlanId,
        TripMemberId memberId,
        string userId,
        string parkItemId,
        TripItemPreferenceLevel level,
        TripItemPreferenceReason? reason,
        DateTime nowUtc)
    {
        return new TripItemPreference(
            id,
            tripPlanId,
            memberId,
            userId,
            parkItemId,
            level,
            reason,
            1,
            nowUtc,
            nowUtc);
    }

    public static TripItemPreference Restore(
        TripItemPreferenceId id,
        TripPlanId tripPlanId,
        TripMemberId memberId,
        string userId,
        string parkItemId,
        TripItemPreferenceLevel level,
        TripItemPreferenceReason? reason,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new TripItemPreference(
            id,
            tripPlanId,
            memberId,
            userId,
            parkItemId,
            level,
            reason,
            version,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Set(
        TripItemPreferenceLevel level,
        TripItemPreferenceReason? reason,
        DateTime nowUtc)
    {
        ValidatePreference(level, reason);
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw Invalid("A preference mutation cannot predate its current state.");
        }

        TripItemPreferenceReason? normalizedReason = level == TripItemPreferenceLevel.Unknown
            ? null
            : reason;
        if (this.Level == level && this.Reason == normalizedReason)
        {
            return;
        }

        if (this.Version == long.MaxValue)
        {
            throw Invalid("The preference version cannot be incremented further.");
        }

        this.Level = level;
        this.Reason = normalizedReason;
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static void ValidatePreference(
        TripItemPreferenceLevel level,
        TripItemPreferenceReason? reason)
    {
        if (!Enum.IsDefined(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (reason.HasValue && !Enum.IsDefined(reason.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        if (level == TripItemPreferenceLevel.Unknown && reason.HasValue)
        {
            throw Invalid("An unknown preference cannot have a reason.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 1)
        {
            throw Invalid("The preference version must be positive.");
        }
    }

    private static void ValidateTimestamps(DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
        {
            throw Invalid("Preference timestamps must be chronological.");
        }
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid("Preference timestamps must be expressed in UTC.");
        }
    }

    private static TripPlanValidationException Invalid(string message)
    {
        return new TripPlanValidationException(TripPlanErrorCodes.InvalidPreference, message);
    }
}
