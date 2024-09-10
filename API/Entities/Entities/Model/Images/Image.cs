using Common.General;

namespace Entities.Model.Images;

public class Image : GeolocatedEntity
{
    public ImageType Type { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
}