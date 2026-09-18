namespace AmusementPark.Core.Domain.Trips;

public enum TripInvitationStatus
{
    Prepared = 1,
    Active = 2,
    Accepting = 3,
    RevocationPending = 4,
    Accepted = 5,
    Declined = 6,
    Revoked = 7,
    Expired = 8,
}
