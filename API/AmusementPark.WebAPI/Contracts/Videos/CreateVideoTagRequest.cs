using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Videos;

public sealed class CreateVideoTagRequest
{
    public string Slug { get; set; } = string.Empty;

    public List<LocalizedTextDto> Labels { get; set; } = new();

    public List<LocalizedTextDto> Descriptions { get; set; } = new();
}
