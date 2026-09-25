using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

internal sealed record TripNotificationAccess(
    TripPlan Trip,
    TripMember Member,
    string UserId);
