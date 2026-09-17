using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripPlanDetailsInput(
    string Title,
    TripDateProposal DateProposal,
    string? DestinationTimeZoneId);
