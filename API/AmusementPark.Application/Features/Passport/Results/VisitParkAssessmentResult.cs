namespace AmusementPark.Application.Features.Passport.Results;

public sealed record VisitParkAssessmentResult(
    double Value,
    string? PrivateComment,
    int Revision,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
