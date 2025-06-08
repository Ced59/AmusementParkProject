using Common.General;

namespace Entities.Model.Images;

public class Image : GeolocatedEntity
{
    public ImageCategory Category { get; set; }
    public string? Path { get; set; }
    public string? Description { get; set; }
}