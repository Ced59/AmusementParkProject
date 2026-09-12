using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrencePage(
    IReadOnlyCollection<RideOccurrence> Items,
    RideOccurrenceListCursor? NextCursor);
