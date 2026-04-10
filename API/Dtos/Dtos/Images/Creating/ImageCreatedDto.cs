namespace Dtos.Images.Creating
{
    public class ImageCreatedDto
    {
        public string? Id { get; set; }
        public IEnumerable<string>? SavedListFile { get; set; }
        public ImageCategoryDto Category { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long SizeInBytes { get; set; }
    }
}
