using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripProgramDayEvidenceResult(
    DateOnly LocalDate,
    string ParkId,
    string? ParkName,
    bool IsParkAvailable,
    string? ParkStatus,
    TripProgramOpeningState OpeningState,
    string? OpeningHoursSourceUrl,
    DateTime? OpeningHoursVerifiedAtUtc,
    DateTime DayPlanUpdatedAtUtc);
