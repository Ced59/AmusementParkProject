namespace AmusementPark.Core.Domain.Trips;

public enum TripPermission
{
    Read = 1,
    EditPlan = 2,
    EditProgram = 3,
    Invite = 4,
    ChangeRoles = 5,
    Vote = 6,
    ManageOwnConstraints = 7,
    Export = 8,
    Delete = 9,
    Leave = 10,
    AddCandidates = 11,
}
