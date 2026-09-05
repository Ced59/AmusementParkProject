using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record UpdateRideOccurrenceCommand(
    string UserId,
    string VisitId,
    string OccurrenceId,
    long ExpectedVersion,
    TimeOnly? LocalTime,
    bool IsApproximate,
    RideOccurrenceStatus Status,
    string? PrivateNote,
    bool ConfirmHistoricalConflict)
    : ICommand<ApplicationResult<RideOccurrenceResult>>;
