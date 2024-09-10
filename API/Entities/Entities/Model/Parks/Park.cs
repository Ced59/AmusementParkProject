using Common.General;

namespace Entities.Model.Parks;

public class Park : GeolocatedEntity
{
    public string? Name { get; set; }
    public string? CountryCode { get; set; }
}