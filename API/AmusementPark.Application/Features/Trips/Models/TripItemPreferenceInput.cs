using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripItemPreferenceInput(
    string ParkItemId,
    long? ExpectedPreferenceVersion,
    TripItemPreferenceLevel Level,
    TripItemPreferenceReason? Reason);
