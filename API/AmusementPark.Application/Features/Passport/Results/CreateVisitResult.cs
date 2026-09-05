namespace AmusementPark.Application.Features.Passport.Results;

public sealed record CreateVisitResult(
    VisitResult Visit,
    bool WasReplayed);
