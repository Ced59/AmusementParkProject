using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Geo;

/// <summary>
/// Entité de domaine localisable géographiquement.
/// </summary>
public abstract class GeolocatedEntityBase : AuditableEntity
{
    /// <summary>
    /// Position géographique associée à l'entité.
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
        Touch();
    }

    /// <summary>
    /// Définit la position de l'entité.
    /// </summary>
    /// <param name="position">Position à appliquer.</param>
    public void SetPosition(GeoPoint? position)
    {
        Position = position;
        Touch();
    }

    /// <summary>
    /// Supprime la position de l'entité.
    /// </summary>
    public void ClearPosition()
    {
        Position = null;
        Touch();
    }
}
