using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record DeleteRideOccurrenceCommand(
    string UserId,
    string VisitId,
    string OccurrenceId,
    long ExpectedVersion)
    : ICommand<ApplicationResult<RideOccurrenceResult>>;
