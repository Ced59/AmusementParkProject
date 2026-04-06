namespace AmusementPark.Core.Geo;

/// <summary>
/// Représente un point géographique du domaine sans dépendance Mongo ou GeoJSON.
/// </summary>
public sealed class GeoPoint
{
    /// <summary>
    /// Initialise une nouvelle instance de <see cref="GeoPoint"/>.
    /// </summary>
    /// <param name="latitude">Latitude en degrés.</param>
    /// <param name="longitude">Longitude en degrés.</param>
    public GeoPoint(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Latitude en degrés.
    /// </summary>
    public double Latitude { get; }

    /// <summary>
    /// Longitude en degrés.
    /// </summary>
    public double Longitude { get; }
}
