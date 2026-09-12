using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceListCursor(
    long SortPosition,
    DateTime CreatedAtUtc,
    RideOccurrenceId OccurrenceId);
