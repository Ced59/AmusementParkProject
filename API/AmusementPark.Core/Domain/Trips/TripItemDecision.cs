using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripItemDecision
{
    public const int MaximumDecisionsPerTrip = 2000;
    public const int MaximumReasonLength = 500;

    private TripItemDecision(
        TripItemDecisionId id,
        TripPlanId tripPlanId,
        string parkItemId,
        TripItemDecisionStatus status,
        string reason,
        string decidedByUserId,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        _ = id.Value;
        _ = tripPlanId.Value;
        this.Id = id;
        this.TripPlanId = tripPlanId;
        this.ParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        this.Status = ValidateStatus(status);
        this.Reason = NormalizeReason(reason);
        this.DecidedByUserId = IdentifierRules.NormalizeRequired(decidedByUserId, nameof(decidedByUserId));
        if (version < 1)
        {
            throw Invalid("The decision version must be positive.");
        }

        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (updatedAtUtc < createdAtUtc)
        {
            throw Invalid("Decision timestamps must be chronological.");
        }

        this.Version = version;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public TripItemDecisionId Id { get; }

    public TripPlanId TripPlanId { get; }

    public string ParkItemId { get; }

    public TripItemDecisionStatus Status { get; private set; }

    public string Reason { get; private set; }

    public string DecidedByUserId { get; private set; }

    public long Version { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TripItemDecision Create(
        TripItemDecisionId id,
        TripPlanId tripPlanId,
        string parkItemId,
        TripItemDecisionStatus status,
        string reason,
        string decidedByUserId,
        DateTime nowUtc)
    {
        return new TripItemDecision(
            id,
            tripPlanId,
            parkItemId,
            status,
            reason,
            decidedByUserId,
            1,
            nowUtc,
            nowUtc);
    }

    public static TripItemDecision Restore(
        TripItemDecisionId id,
        TripPlanId tripPlanId,
        string parkItemId,
        TripItemDecisionStatus status,
        string reason,
        string decidedByUserId,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        return new TripItemDecision(
            id,
            tripPlanId,
            parkItemId,
            status,
            reason,
            decidedByUserId,
            version,
            createdAtUtc,
            updatedAtUtc);
    }

    public void Set(
        TripItemDecisionStatus status,
        string reason,
        string decidedByUserId,
        DateTime nowUtc)
    {
        TripItemDecisionStatus normalizedStatus = ValidateStatus(status);
        string normalizedReason = NormalizeReason(reason);
        string normalizedActor = IdentifierRules.NormalizeRequired(decidedByUserId, nameof(decidedByUserId));
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw Invalid("A decision mutation cannot predate its current state.");
        }

        if (this.Status == normalizedStatus
            && string.Equals(this.Reason, normalizedReason, StringComparison.Ordinal)
            && string.Equals(this.DecidedByUserId, normalizedActor, StringComparison.Ordinal))
        {
            return;
        }

        if (this.Version == long.MaxValue)
        {
            throw Invalid("The decision version cannot be incremented further.");
        }

        this.Status = normalizedStatus;
        this.Reason = normalizedReason;
        this.DecidedByUserId = normalizedActor;
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static TripItemDecisionStatus ValidateStatus(TripItemDecisionStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return status;
    }

    private static string NormalizeReason(string reason)
    {
        string normalized = reason?.Trim() ?? string.Empty;
        if (normalized.Length is < 3 or > MaximumReasonLength)
        {
            throw Invalid($"A decision reason must contain between 3 and {MaximumReasonLength} characters.");
        }

        return normalized;
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid("Decision timestamps must be expressed in UTC.");
        }
    }

    private static TripPlanValidationException Invalid(string message)
    {
        return new TripPlanValidationException(TripPlanErrorCodes.InvalidDecision, message);
    }
}
