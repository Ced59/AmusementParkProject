using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

internal sealed record ParsedOccurrenceScope(
    string UserId,
    VisitId VisitId,
    RideOccurrenceId OccurrenceId);
