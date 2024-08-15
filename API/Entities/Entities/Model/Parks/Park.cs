using Common.General;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Entities.Model.Parks;

public class Park : ModelBase
{
    private double _latitude;
    private double _longitude;

    public string? Name { get; set; }
    public string? CountryCode { get; set; }

    public double Latitude
    {
        get => _latitude;
        set
        {
            _latitude = value;
            UpdateLocation();
        }
    }

    public double Longitude
    {
        get => _longitude;
        set
        {
            _longitude = value;
            UpdateLocation();
        }
    }

    public GeoJsonPoint<GeoJson2DGeographicCoordinates>? Location { get; private set; }

    private void UpdateLocation()
    {
        // Assurez-vous que les valeurs de latitude et de longitude sont valides avant de mettre à jour Location
        if (_latitude != 0 && _longitude != 0)
        {
            Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(_longitude, _latitude));
        }
    }
}