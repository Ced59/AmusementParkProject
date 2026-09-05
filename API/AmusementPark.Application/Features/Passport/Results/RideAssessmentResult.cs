namespace AmusementPark.Application.Features.Passport.Results;

public sealed record RideAssessmentResult(
    double Value,
    string? PrivateComment,
    int Revision,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
