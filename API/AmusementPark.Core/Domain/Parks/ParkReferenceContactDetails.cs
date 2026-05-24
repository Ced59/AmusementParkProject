namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Coordonnées facultatives d'une référence liée aux parcs.
/// </summary>
public sealed class ParkReferenceContactDetails
{
    public string? WebsiteUrl { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public string? CountryCode { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
