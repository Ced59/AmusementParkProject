namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportItemExperienceResult(
    string VisitId,
    VisitDateResult Date);
