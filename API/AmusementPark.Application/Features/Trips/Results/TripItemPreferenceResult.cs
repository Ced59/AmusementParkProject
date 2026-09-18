using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripItemPreferenceResult(
    string ParkId,
    string ParkName,
    string ParkItemId,
    string ParkItemName,
    string? MainImageId,
    TripItemPreferenceLevel Level,
    TripItemPreferenceReason? Reason,
    long? Version);
