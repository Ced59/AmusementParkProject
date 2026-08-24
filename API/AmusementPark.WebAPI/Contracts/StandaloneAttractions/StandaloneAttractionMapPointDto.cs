using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.StandaloneAttractions;

public sealed class StandaloneAttractionMapPointDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public ParkItemTypeDto Type { get; set; } = ParkItemTypeDto.Attraction;

    public string? Subtype { get; set; }

    public string? Status { get; set; }

    public string? City { get; set; }

    public string? Street { get; set; }

    public string? PostalCode { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
