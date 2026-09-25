using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripPassportTransitionDayResult(
    DateOnly LocalDate,
    string ParkId,
    string? ParkName,
    bool IsParkAvailable,
    bool CanConfirm,
    string? ExistingVisitId,
    VisitStatus? ExistingVisitStatus,
    IReadOnlyCollection<TripPassportTransitionItemResult> Attractions);
