using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

internal sealed record ParsedRideAssessmentScope(
    string UserId,
    RideOccurrenceId OccurrenceId);
