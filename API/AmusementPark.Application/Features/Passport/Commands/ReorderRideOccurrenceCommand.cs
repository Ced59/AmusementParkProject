using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record ReorderRideOccurrenceCommand(
    string UserId,
    string VisitId,
    string ClientOperationId,
    string OccurrenceId,
    long ExpectedVersion,
    string? AnchorOccurrenceId,
    RideOccurrencePlacement Placement)
    : ICommand<ApplicationResult<ReorderRideOccurrenceResult>>;
