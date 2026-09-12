namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportVisitExperienceResult(
    string VisitId,
    string ParkId,
    VisitDateResult Date);
