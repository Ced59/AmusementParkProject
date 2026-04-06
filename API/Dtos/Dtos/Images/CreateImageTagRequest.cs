using Dtos.Shared;

namespace Dtos.Images
{
    public sealed class CreateImageTagRequest
    {
        public string Slug { get; set; } = string.Empty;
        public List<LocalizedItemDto<string>> Labels { get; set; } = new();
        public List<LocalizedItemDto<string>> Descriptions { get; set; } = new();
    }
}
