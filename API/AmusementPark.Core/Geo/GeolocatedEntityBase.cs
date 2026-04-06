using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Geo;

/// <summary>
/// Entité de domaine localisable géographiquement.
/// </summary>
public abstract class GeolocatedEntityBase : EntityBase
{
    /// <summary>
    /// Point géographique associé à l'entité.
    /// </summary>
    public GeoPoint? Position { get; private set; }

    /// <summary>
    /// Définit la position de l'entité.
    /// </summary>
    /// <param name="latitude">Latitude en degrés.</param>
    /// <param name="longitude">Longitude en degrés.</param>
    public void SetPosition(double latitude, double longitude)
    {
        Position = new GeoPoint(latitude, longitude);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Supprime la position de l'entité.
    /// </summary>
    public void ClearPosition()
    {
        Position = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
