using Entities.Model.Images;

namespace Services.Models.Images
{
    public sealed class ExtractedImageMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public long SizeInBytes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public ImageExifMetadata? ExifMetadata { get; set; }
    }
}
