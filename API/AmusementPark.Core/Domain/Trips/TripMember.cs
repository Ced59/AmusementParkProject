using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Trips;

public sealed class TripMember
{
    private TripMember(
        TripMemberId id,
        string userId,
        TripDelegatedRole? delegatedRole,
        TripMembershipState state,
        DateTime joinedAtUtc)
    {
        _ = id.Value;
        if (delegatedRole.HasValue && !Enum.IsDefined(delegatedRole.Value))
        {
            throw new TripPlanValidationException(
                TripPlanErrorCodes.InvalidState,
                "The delegated trip role is invalid.");
        }

        if (!Enum.IsDefined(state) || joinedAtUtc.Kind != DateTimeKind.Utc)
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
    }

    public TripMemberId Id { get; }

    public string UserId { get; }

    public TripDelegatedRole? DelegatedRole { get; }

    public TripMembershipState State { get; }

    public DateTime JoinedAtUtc { get; }

    public static TripMember CreateOwner(string userId, DateTime nowUtc)
    {
        return new TripMember(TripMemberId.New(), userId, null, TripMembershipState.Active, nowUtc);
    }

    public static TripMember Restore(
        TripMemberId id,
        string userId,
        TripDelegatedRole? delegatedRole,
        TripMembershipState state,
        DateTime joinedAtUtc)
    {
        return new TripMember(id, userId, delegatedRole, state, joinedAtUtc);
    }
}
