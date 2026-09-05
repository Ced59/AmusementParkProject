using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record UpsertVisitParkAssessmentCommand(
    string UserId,
    string VisitId,
    double Value,
    string? PrivateComment,
    long ExpectedVersion)
    : ICommand<ApplicationResult<VisitResult>>;
