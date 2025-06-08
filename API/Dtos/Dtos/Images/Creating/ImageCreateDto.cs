using Microsoft.AspNetCore.Http;

namespace Dtos.Images.Creating
{
    public class ImageCreateDto
    {
        public ImageCategoryDto Category { get; set; }

        public IFormFile? File { get; set; }

        public string? Description { get; set; }
    }
}