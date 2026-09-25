using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripDepartureCleanup(TripPlanId TripPlanId, string UserId);
