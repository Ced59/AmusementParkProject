using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.NearbyDestinations.Results;

public sealed record NearbyDestinationResult(
    Park? Park,
    StandaloneAttraction? StandaloneAttraction,
    double DistanceKilometers,
    int EstimatedTravelDurationMinutes,
    int ProximityRank,
    string? PhotoImageId = null);
