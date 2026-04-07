namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Coordonnées géographiques d'une image.
/// </summary>
public sealed class ImageGeoLocationDto
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
