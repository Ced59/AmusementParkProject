using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Common.General;

public class GeolocatedEntity : ModelBase
{
    private double _latitude;
    private double _longitude;

    /// <summary>
    /// Latitude du point (entre -90 et 90). Lorsqu’on l’assigne, UpdateLocation() reconstruit le champ GeoJSON “location”.
    /// </summary>
    [BsonElement("latitude")]
    [BsonRepresentation(BsonType.Double)]
    public double Latitude
    {
        get => _latitude;
        set
        {
            if (IsValidLatitude(value))
            {
                _latitude = value;
                UpdateLocation();
            }
        }
    }

    /// <summary>
    /// Longitude du point (entre -180 et 180). Lorsqu’on l’assigne, UpdateLocation() reconstruit le champ GeoJSON “location”.
    /// </summary>
    [BsonElement("longitude")]
    [BsonRepresentation(BsonType.Double)]
    public double Longitude
    {
        get => _longitude;
        set
        {
            if (IsValidLongitude(value))
            {
                _longitude = value;
                UpdateLocation();
            }
        }
    }

    /// <summary>
    /// Champ GeoJSON de type Point, que MongoDB pourra indexer en 2dsphere.
    /// Contient automatiquement [longitude, latitude].
    /// </summary>
    [BsonElement("location")]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates>? Location { get; private set; }

    protected void UpdateLocation()
    {
        if (IsValidLatitude(_latitude) && IsValidLongitude(_longitude))
        {
            Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                new GeoJson2DGeographicCoordinates(_longitude, _latitude));
        }
    }

    private static bool IsValidLatitude(double latitude)
    {
        return latitude is >= -90 and <= 90;
    }

    private static bool IsValidLongitude(double longitude)
    {
        return longitude is >= -180 and <= 180;
    }
}