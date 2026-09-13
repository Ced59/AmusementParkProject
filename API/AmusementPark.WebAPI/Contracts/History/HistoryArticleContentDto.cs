using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.Parks;

namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoryArticleContentDto
{
    public string? Slug { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Subtitles { get; set; } = new();

    public List<LocalizedTextDto> Summaries { get; set; } = new();

    public string? MainImageId { get; set; }

    public List<HistoryArticleBlockDto> Blocks { get; set; } = new();

    public List<HistorySourceReferenceDto> Sources { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}
