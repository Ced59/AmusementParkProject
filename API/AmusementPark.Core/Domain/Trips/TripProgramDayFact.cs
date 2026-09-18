using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Trips;

public sealed record TripProgramDayFact(
    string DayPlanId,
    DateOnly LocalDate,
    string ParkId,
    TripParkCandidateState? CandidateState,
    bool IsParkAvailable,
    ParkStatus? ParkStatus,
    bool? IsOpenOnDate,
    DateTime? OpeningHoursVerifiedAtUtc,
    DateTime DayPlanUpdatedAtUtc);
