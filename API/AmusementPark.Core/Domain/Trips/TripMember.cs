using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripMember
{
    private TripMember(
        TripMemberId id,
        string userId,
        TripDelegatedRole? delegatedRole,
        TripMembershipState state,
        DateTime joinedAtUtc,
        long memberDataEpoch,
        string? admissionOperationId)
    {
        _ = id.Value;
        if (delegatedRole.HasValue && !Enum.IsDefined(delegatedRole.Value))
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The delegated trip role is invalid.");
        }

        string? normalizedAdmissionOperationId = string.IsNullOrWhiteSpace(admissionOperationId)
            ? null
            : admissionOperationId.Trim();
        if (!Enum.IsDefined(state)
            || joinedAtUtc.Kind != DateTimeKind.Utc
            || memberDataEpoch < 1
            || (state == TripMembershipState.Provisional && normalizedAdmissionOperationId is null))
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip membership state is invalid.");
        }

        this.Id = id;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.DelegatedRole = delegatedRole;
        this.State = state;
        this.JoinedAtUtc = joinedAtUtc;
        this.MemberDataEpoch = memberDataEpoch;
        this.AdmissionOperationId = normalizedAdmissionOperationId;
    }

    public TripMemberId Id { get; }

    public string UserId { get; }

    public TripDelegatedRole? DelegatedRole { get; private set; }

    public TripMembershipState State { get; private set; }

    public DateTime JoinedAtUtc { get; }

    public long MemberDataEpoch { get; private set; }

    public string? AdmissionOperationId { get; private set; }

    public static TripMember CreateOwner(string userId, DateTime nowUtc)
    {
        return new TripMember(TripMemberId.New(), userId, null, TripMembershipState.Active, nowUtc, 1, null);
    }

    public static TripMember CreateProvisional(
        string userId,
        TripDelegatedRole delegatedRole,
        string admissionOperationId,
        DateTime nowUtc)
    {
        return new TripMember(
            TripMemberId.New(),
            userId,
            delegatedRole,
            TripMembershipState.Provisional,
            nowUtc,
            1,
            admissionOperationId);
    }

    public static TripMember Restore(
        TripMemberId id,
        string userId,
        TripDelegatedRole? delegatedRole,
        TripMembershipState state,
        DateTime joinedAtUtc,
        long memberDataEpoch = 1,
        string? admissionOperationId = null)
    {
        return new TripMember(
            id,
            userId,
            delegatedRole,
            state,
            joinedAtUtc,
            memberDataEpoch,
            admissionOperationId);
    }

    public void Activate(string admissionOperationId)
    {
        if (this.State != TripMembershipState.Provisional
            || !string.Equals(this.AdmissionOperationId, admissionOperationId, StringComparison.Ordinal))
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The provisional trip member does not match the admission operation.");
        }

        this.State = TripMembershipState.Active;
    }

    public void ChangeDelegatedRole(TripDelegatedRole role)
    {
        if (this.State != TripMembershipState.Active || !Enum.IsDefined(role))
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "Only an active delegated member can change role.");
        }

        this.DelegatedRole = role;
    }

    public void BecomeOwner()
    {
        if (this.State != TripMembershipState.Active)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "Only an active trip member can become owner.");
        }

        this.DelegatedRole = null;
    }

    public void BeginLeaving()
    {
        if (this.State != TripMembershipState.Active || this.MemberDataEpoch == long.MaxValue)
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The trip member cannot leave in the current state.");
        }

        this.State = TripMembershipState.Leaving;
        this.MemberDataEpoch++;
    }
}
