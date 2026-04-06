using Dtos.Shared;

namespace Dtos.Images
{
    public sealed class ImageTagDto
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public List<LocalizedItemDto<string>> Labels { get; set; } = new();
        public List<LocalizedItemDto<string>> Descriptions { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
