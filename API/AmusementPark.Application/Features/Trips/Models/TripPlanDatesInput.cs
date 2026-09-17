using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripPlanDatesInput(
    TripDateProposal DateProposal,
    string? DestinationTimeZoneId);
