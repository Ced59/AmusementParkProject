using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record UpsertRideAssessmentCommand(
    string UserId,
    string OccurrenceId,
    double Value,
    string? PrivateComment,
    long ExpectedVersion)
    : ICommand<ApplicationResult<RideOccurrenceResult>>;
