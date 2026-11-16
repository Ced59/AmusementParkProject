using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.NearbyDestinations;

/// <summary>Only the data consumed by public destination cards, in every supported language.</summary>
public sealed record NearbyDestinationCardDto(
    string Kind, string Id, string? Name, string? CountryCode, string? City,
    string? Street, string? PostalCode, double? Latitude, double? Longitude,
    string? WebsiteUrl, string? LogoImageId, string? PhotoImageId,
    string? ParkStatus, string? AttractionStatus, IReadOnlyCollection<LocalizedTextDto> ShortDescriptions,
    double DistanceKilometers, int EstimatedTravelDurationMinutes, int ProximityRank);
