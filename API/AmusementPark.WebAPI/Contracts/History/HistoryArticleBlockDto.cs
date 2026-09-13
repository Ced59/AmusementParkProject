using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoryArticleBlockDto
{
    public string? Id { get; set; }

    public string Type { get; set; } = "Paragraph";

    public int SortOrder { get; set; }

    public int? HeadingLevel { get; set; }

    public List<LocalizedTextDto> Texts { get; set; } = new();

    public string? ImageId { get; set; }

    public List<string> ImageIds { get; set; } = new();

    public List<LocalizedTextDto> Captions { get; set; } = new();
}
