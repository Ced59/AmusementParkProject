using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

internal sealed record ParsedVisitAssessmentScope(
    string UserId,
    VisitId VisitId);
