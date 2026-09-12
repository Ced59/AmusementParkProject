using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record IdempotentRideOccurrenceReorderResult(
    IdempotentRideOccurrenceReorderStatus Status,
    RideOccurrence? Occurrence,
    bool WasNormalized);
