using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record RideOccurrencePageResult(
    IReadOnlyCollection<RideOccurrenceResult> Items,
    RideOccurrenceListCursor? NextCursor);
