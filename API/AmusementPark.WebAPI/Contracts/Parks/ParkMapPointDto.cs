namespace AmusementPark.WebAPI.Contracts.Parks;

/// <summary>
/// Point cartographique public minimal pour un parc visible.
/// </summary>
public sealed class ParkMapPointDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? CountryCode { get; set; }

    public string? City { get; set; }

    public string? Street { get; set; }

    public string? PostalCode { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string? CurrentLogoImageId { get; set; }
}
