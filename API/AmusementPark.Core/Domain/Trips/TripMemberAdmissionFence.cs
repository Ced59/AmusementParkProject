namespace AmusementPark.Core.Domain.Trips;

public sealed class TripMemberAdmissionFence
{
    private TripMemberAdmissionFence(
        TripInvitationId invitationId,
        string operationId,
        string candidateUserId,
        long generation,
        DateTime leaseExpiresAtUtc,
        TripMemberAdmissionFenceState state)
    {
        _ = invitationId.Value;
        string normalizedOperationId = operationId?.Trim() ?? string.Empty;
        string normalizedUserId = candidateUserId?.Trim() ?? string.Empty;
        if (normalizedOperationId.Length == 0
            || normalizedUserId.Length == 0
            || generation < 1
            || leaseExpiresAtUtc.Kind != DateTimeKind.Utc
            || !Enum.IsDefined(state))
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip member admission fence is invalid.");
        }

        this.InvitationId = invitationId;
        this.OperationId = normalizedOperationId;
        this.CandidateUserId = normalizedUserId;
        this.Generation = generation;
        this.LeaseExpiresAtUtc = leaseExpiresAtUtc;
        this.State = state;
    }

    public TripInvitationId InvitationId { get; }
    public string OperationId { get; }
    public string CandidateUserId { get; }
    public long Generation { get; }
    public DateTime LeaseExpiresAtUtc { get; }
    public TripMemberAdmissionFenceState State { get; private set; }

    public static TripMemberAdmissionFence Prepare(
        TripInvitationId invitationId,
        string operationId,
        string candidateUserId,
        long generation,
        DateTime leaseExpiresAtUtc)
    {
        return new TripMemberAdmissionFence(
            invitationId,
            operationId,
            candidateUserId,
            generation,
            leaseExpiresAtUtc,
            TripMemberAdmissionFenceState.Prepared);
    }

    public static TripMemberAdmissionFence Restore(
        TripInvitationId invitationId,
        string operationId,
        string candidateUserId,
        long generation,
        DateTime leaseExpiresAtUtc,
        TripMemberAdmissionFenceState state)
    {
        return new TripMemberAdmissionFence(
            invitationId,
            operationId,
            candidateUserId,
            generation,
            leaseExpiresAtUtc,
            state);
    }

    public void Arm()
    {
        if (this.State != TripMemberAdmissionFenceState.Prepared)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "Only a prepared trip admission fence can be armed.");
        }

        this.State = TripMemberAdmissionFenceState.Active;
    }

    public void MarkApplied()
    {
        if (this.State != TripMemberAdmissionFenceState.Active)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "Only an active trip admission fence can be applied.");
        }

        this.State = TripMemberAdmissionFenceState.Applied;
    }

    public void Cancel()
    {
        this.State = TripMemberAdmissionFenceState.Cancelled;
    }
}
