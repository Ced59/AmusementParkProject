using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record SetTripItemPreferenceCommand(
    string UserId,
    string TripPlanId,
    long ExpectedPlanVersion,
    TripItemPreferenceInput Preference)
    : ICommand<ApplicationResult<TripPreferenceBoardResult>>;
