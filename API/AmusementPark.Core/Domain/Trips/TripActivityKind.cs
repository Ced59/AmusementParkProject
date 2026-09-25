namespace AmusementPark.Core.Domain.Trips;

public enum TripActivityKind
{
    TripCreated = 0,
    TripRenamed = 1,
    DatesChanged = 2,
    CandidateAdded = 3,
    CandidateUpdated = 4,
    CandidateMoved = 5,
    CandidateRemoved = 6,
    DayUpdated = 7,
    DayRemoved = 8,
    InvitationCreated = 9,
    InvitationRevoked = 10,
    InvitationAccepted = 11,
    InvitationDeclined = 12,
    ParticipantRoleChanged = 13,
    OwnershipTransferred = 14,
    ParticipantLeft = 15,
    PreferencesUpdated = 16,
    CollectiveDecisionUpdated = 17,
    PlanExported = 18,
}
