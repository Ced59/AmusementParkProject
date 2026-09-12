using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceVersionedChange(
    RideOccurrence Occurrence,
    long ExpectedVersion,
    long PreviousSortPosition);
