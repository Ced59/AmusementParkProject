namespace AmusementPark.Core.Domain.Trips;

public sealed class TripChildMutationLease
{
    public TripChildMutationLease(
        string operationId,
        TripMemberId actorMemberId,
        long childMutationEpoch,
        long generation,
        DateTime expiresAtUtc)
    {
        string normalizedOperationId = operationId?.Trim() ?? string.Empty;
        if (normalizedOperationId.Length is 0 or > 100
            || childMutationEpoch < 1
            || generation < 1
            || expiresAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidChildMutationLease,
                "The child mutation lease is invalid.");
        }

        _ = actorMemberId.Value;
        this.OperationId = normalizedOperationId;
        this.ActorMemberId = actorMemberId;
        this.ChildMutationEpoch = childMutationEpoch;
        this.Generation = generation;
        this.ExpiresAtUtc = expiresAtUtc;
    }

    public string OperationId { get; }

    public TripMemberId ActorMemberId { get; }

    public long ChildMutationEpoch { get; }

    public long Generation { get; }

    public DateTime ExpiresAtUtc { get; }
}
