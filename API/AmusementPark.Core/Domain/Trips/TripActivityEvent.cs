using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripActivityEvent
{
    public const int MaximumOperationKeyLength = 200;
    public const int MaximumAffectedCount = 2000;

    public TripActivityEvent(
        string id,
        TripPlanId tripPlanId,
        TripMemberId? actorMemberId,
        TripEffectiveRole? actorRole,
        TripActivityKind kind,
        string operationKey,
        long sequence,
        int affectedCount,
        DateTime occurredAtUtc)
    {
        this.Id = IdentifierRules.NormalizeRequired(id, nameof(id));
        _ = tripPlanId.Value;
        if (actorMemberId.HasValue)
        {
            _ = actorMemberId.Value.Value;
        }
        if (actorMemberId.HasValue != actorRole.HasValue
            || actorRole.HasValue && !Enum.IsDefined(actorRole.Value)
            || !Enum.IsDefined(kind)
            || sequence < 1
            || affectedCount is < 1 or > MaximumAffectedCount
            || occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip activity event is invalid.");
        }

        string normalizedOperationKey = operationKey?.Trim() ?? string.Empty;
        if (normalizedOperationKey.Length is < 1 or > MaximumOperationKeyLength)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip activity operation key is invalid.");
        }

        this.TripPlanId = tripPlanId;
        this.ActorMemberId = actorMemberId;
        this.ActorRole = actorRole;
        this.Kind = kind;
        this.OperationKey = normalizedOperationKey;
        this.Sequence = sequence;
        this.AffectedCount = affectedCount;
        this.OccurredAtUtc = occurredAtUtc;
    }

    public string Id { get; }

    public TripPlanId TripPlanId { get; }

    public TripMemberId? ActorMemberId { get; }

    public TripEffectiveRole? ActorRole { get; }

    public TripActivityKind Kind { get; }

    public string OperationKey { get; }

    public long Sequence { get; }

    public int AffectedCount { get; }

    public DateTime OccurredAtUtc { get; }
}
