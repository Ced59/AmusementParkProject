using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record RideOccurrenceCreationItem(
    string ParkItemId,
    TimeOnly? LocalTime,
    bool IsApproximate,
    RideOccurrenceStatus Status,
    string? PrivateNote,
    bool ConfirmHistoricalConflict,
    int Count = 1);
