using MongoDB.Driver.GeoJsonObjectModel;

namespace Common.General;

public class GeolocatedEntity : ModelBase
{
    private double _latitude;
    private double _longitude;

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