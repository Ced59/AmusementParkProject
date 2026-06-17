using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Videos;

public sealed class VideoTagDto
{
    public string Id { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public List<LocalizedTextDto> Labels { get; set; } = new();

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
