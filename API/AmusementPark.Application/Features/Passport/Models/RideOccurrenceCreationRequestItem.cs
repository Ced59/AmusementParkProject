using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceCreationRequestItem(
    string ParkItemId,
    OccurrenceMoment Moment,
    RideOccurrenceStatus Status,
    RideLogSource Source,
    string? PrivateNote,
    bool ConfirmHistoricalConflict);
