namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceCreationKeyReservationResult(
    RideOccurrenceCreationKeyReservationStatus Status,
    RideOccurrenceCreationPreparation? Preparation = null);
