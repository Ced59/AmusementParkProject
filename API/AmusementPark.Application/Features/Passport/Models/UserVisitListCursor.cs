using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record UserVisitListCursor(
    VisitDate Date,
    DateTime UpdatedAtUtc,
    VisitId VisitId);
