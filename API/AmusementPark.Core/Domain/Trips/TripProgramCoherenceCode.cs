namespace AmusementPark.Core.Domain.Trips;

public enum TripProgramCoherenceCode
{
    DateOutsideProposal = 1,
    MultipleParksSameDate = 2,
    CandidateMissing = 3,
    CandidateNotSelected = 4,
    ParkUnavailable = 5,
    ParkNotOperating = 6,
    OpeningHoursUnknown = 7,
    OpeningHoursClosed = 8,
    OpeningHoursStale = 9,
    OpeningHoursVerifiedAfterPlanning = 10,
    AttractionUnavailable = 11,
    AttractionClosed = 12,
    NewMemberConstraint = 13,
}
