namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportParkAssessmentPointResult(
    string VisitId,
    VisitDateResult Date,
    double Rating);
