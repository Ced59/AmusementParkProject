using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripPreferenceCount(
    string ParkItemId,
    TripItemPreferenceLevel Level,
    int Count,
    DateTime? LatestUpdatedAtUtc = null);
