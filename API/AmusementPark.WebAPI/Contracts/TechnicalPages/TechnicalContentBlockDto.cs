using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.TechnicalPages;

public sealed class TechnicalContentBlockDto
{
    public string BlockType { get; set; } = "richText";

    public string? Tone { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImageId { get; set; }

    public string? DiagramKey { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Bodies { get; set; } = new();

    public List<LocalizedTextDto> Captions { get; set; } = new();

    public List<LocalizedTextDto> AltTexts { get; set; } = new();

    public List<TechnicalContentListItemDto> Items { get; set; } = new();

    public TechnicalContentTableDto? Table { get; set; }

    public List<TechnicalContentMetricDto> Metrics { get; set; } = new();

    public List<TechnicalContentLinkDto> Links { get; set; } = new();

    public List<TechnicalContentBlockDto> Columns { get; set; } = new();
}
