using Dtos.Images.Creating;

namespace Services.Models.Images
{
    public class ImageSaveRequest
    {
        public required Stream FileStream { get; set; }

        public required ImageCategoryDto Category { get; set; }

        public string? OriginalFileName { get; set; }

        public string? ContentType { get; set; }

        public string? Description { get; set; }

        public bool WithWatermark { get; set; } = true;
    }
}
