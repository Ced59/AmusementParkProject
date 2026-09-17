using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record RenameTripPlanCommand(
    string UserId,
    string TripPlanId,
    long ExpectedVersion,
    string Title)
    : ICommand<ApplicationResult<TripPlanResult>>;
