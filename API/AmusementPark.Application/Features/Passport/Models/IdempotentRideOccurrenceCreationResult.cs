using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record IdempotentRideOccurrenceCreationResult(
    IdempotentRideOccurrenceCreationStatus Status,
    IReadOnlyCollection<RideOccurrence> Occurrences,
    bool WasNormalized = false);
