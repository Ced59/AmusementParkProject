using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Contracts.StandaloneAttractions;

namespace AmusementPark.WebAPI.Contracts.NearbyDestinations;

public sealed record NearbyDestinationDto(
    string Kind,
    ParkDto? Park,
    StandaloneAttractionDto? StandaloneAttraction,
    double DistanceKilometers,
    int EstimatedTravelDurationMinutes,
    int ProximityRank,
    string? PhotoImageId = null);
