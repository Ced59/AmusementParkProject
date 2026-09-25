using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripPassportTransitionItemResult(
    string ParkItemId,
    string Name,
    string? MainImageId,
    TripItemPreferenceLevel OwnPreference,
    HistoricalConsistency HistoricalConsistency);
