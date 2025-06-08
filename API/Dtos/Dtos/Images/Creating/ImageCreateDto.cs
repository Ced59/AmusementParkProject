using Microsoft.AspNetCore.Http;

namespace Dtos.Images.Creating
{
    public class ImageCreateDto
    {
        public string? Category { get; set; }

        public IFormFile? File { get; set; }
    }
}