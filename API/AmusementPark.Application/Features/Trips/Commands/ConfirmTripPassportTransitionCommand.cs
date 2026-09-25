using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Commands;

public sealed record ConfirmTripPassportTransitionCommand(
    string UserId,
    string TripPlanId,
    DateOnly LocalDate,
    IReadOnlyCollection<string> ParkItemIds)
    : ICommand<ApplicationResult<ConfirmTripPassportTransitionResult>>;
