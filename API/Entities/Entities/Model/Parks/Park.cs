using Common.General;
using Entities.Model.Attractions;

namespace Entities.Model.Parks;

public class Park : ModelBase
{
    public string? Name { get; set; }
    public string? CountryCode { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}