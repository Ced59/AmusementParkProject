using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Videos;

public sealed class UpdateVideoTagRequest
{
    public string Slug { get; set; } = string.Empty;

    public List<LocalizedTextDto> Labels { get; set; } = new();

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public bool IsActive { get; set; } = true;
}
