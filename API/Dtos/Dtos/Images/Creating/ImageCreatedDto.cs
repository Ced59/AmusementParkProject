namespace Dtos.Images.Creating
{
    public class ImageCreatedDto
    {
        public string? Id { get; set; }
        public IEnumerable<string>? SavedListFile { get; set; }
    }
}